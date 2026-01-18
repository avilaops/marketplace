using MarketplaceBuilder.Api.Models;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;
using MarketplaceBuilder.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.RegularExpressions;

namespace MarketplaceBuilder.Api.Endpoints;

public static class StoreProvisioningEndpoints
{
    private static readonly Regex SubdomainRegex = new(@"^[a-z0-9][a-z0-9-]{1,28}[a-z0-9]$", RegexOptions.Compiled);
    private static readonly HashSet<string> ReservedSubdomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "www", "app", "dashboard", "portal", "store", "shop", "mail", "ftp"
    };

    public static void MapStoreProvisioningEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin/stores")
            .WithTags("Store Provisioning");

        group.MapPost("/", CreateStore)
            .WithName("CreateStore")
            .Produces<StoreResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{tenantId:guid}/config", UpdateStoreConfig)
            .WithName("UpdateStoreConfig")
            .Produces<StoreResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{tenantId:guid}/publish", PublishStore)
            .WithName("PublishStore")
            .Produces<PublishStoreResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> CreateStore(
        [FromBody] CreateStoreRequest request,
        ApplicationDbContext context,
        IDistributedCache cache,
        IConfiguration configuration,
        HttpContext httpContext)
    {
        // Validações
        if (string.IsNullOrWhiteSpace(request.StoreName))
        {
            return Results.Problem("StoreName is required", statusCode: 400);
        }

        if (string.IsNullOrWhiteSpace(request.Subdomain))
        {
            return Results.Problem("Subdomain is required", statusCode: 400);
        }

        var subdomain = request.Subdomain.ToLowerInvariant().Trim();

        // Validar formato do subdomain
        if (!SubdomainRegex.IsMatch(subdomain))
        {
            return Results.Problem(
                "Subdomain must be 3-30 characters, lowercase, alphanumeric with hyphens (not starting or ending with hyphen)",
                statusCode: 400);
        }

        // Validar subdomains reservados
        if (ReservedSubdomains.Contains(subdomain))
        {
            return Results.Problem($"Subdomain '{subdomain}' is reserved", statusCode: 400);
        }

        var baseDomain = configuration["Platform:BaseDomain"] ?? "localtest.me";
        var hostname = $"{subdomain}.{baseDomain}";

        // Verificar se hostname já existe
        var existingDomain = await context.Domains
            .AnyAsync(d => d.Hostname == hostname);

        if (existingDomain)
        {
            return Results.Problem($"Hostname '{hostname}' is already in use", statusCode: 400);
        }

        // Criar Tenant, Domain e StorefrontConfig de forma transacional
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.StoreName,
                Slug = subdomain,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Tenants.Add(tenant);

            var domain = new Domain
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Hostname = hostname,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Domains.Add(domain);

            var storefrontConfig = new StorefrontConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                StoreName = request.StoreName,
                Subdomain = subdomain,
                Currency = request.Currency,
                Locale = request.Locale,
                Theme = request.Theme,
                Status = StorefrontStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.StorefrontConfigs.Add(storefrontConfig);

            // Audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Action = "Create",
                Entity = "Store",
                EntityId = tenant.Id,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    tenant.Name,
                    tenant.Slug,
                    hostname,
                    storefrontConfig.Currency,
                    storefrontConfig.Locale,
                    storefrontConfig.Theme
                }),
                CreatedAt = DateTime.UtcNow
            };

            context.AuditLogs.Add(auditLog);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Invalidar cache (se existir algum entry antigo)
            await TenantResolver.InvalidateCacheAsync(cache, hostname);

            return Results.Created(
                $"/api/admin/stores/{tenant.Id}",
                new StoreResponse(tenant.Id, hostname, storefrontConfig.Status.ToString()));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Results.Problem($"Error creating store: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateStoreConfig(
        Guid tenantId,
        [FromBody] UpdateStoreConfigRequest request,
        ApplicationDbContext context)
    {
        var config = await context.StorefrontConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (config == null)
        {
            return Results.Problem($"Store not found for tenant {tenantId}", statusCode: 404);
        }

        var updated = false;

        if (!string.IsNullOrWhiteSpace(request.StoreName))
        {
            config.StoreName = request.StoreName;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            config.Currency = request.Currency;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Locale))
        {
            config.Locale = request.Locale;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Theme))
        {
            config.Theme = request.Theme;
            updated = true;
        }

        if (updated)
        {
            config.UpdatedAt = DateTime.UtcNow;

            // Audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Action = "Update",
                Entity = "StorefrontConfig",
                EntityId = config.Id,
                NewValues = System.Text.Json.JsonSerializer.Serialize(request),
                CreatedAt = DateTime.UtcNow
            };

            context.AuditLogs.Add(auditLog);
            await context.SaveChangesAsync();
        }

        var domain = await context.Domains
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .Select(d => d.Hostname)
            .FirstOrDefaultAsync();

        return Results.Ok(new StoreResponse(tenantId, domain ?? "", config.Status.ToString()));
    }

    private static async Task<IResult> PublishStore(
        Guid tenantId,
        ApplicationDbContext context,
        IDistributedCache cache)
    {
        var config = await context.StorefrontConfigs
            .Include(c => c.Tenant)
            .ThenInclude(t => t.Domains)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (config == null)
        {
            return Results.Problem($"Store not found for tenant {tenantId}", statusCode: 404);
        }

        // Validar pré-requisitos
        if (!config.Tenant.Domains.Any(d => d.IsActive))
        {
            return Results.Problem("Store must have at least one active domain", statusCode: 400);
        }

        if (config.Status == StorefrontStatus.Live)
        {
            return Results.Problem("Store is already published", statusCode: 400);
        }

        // Publicar
        config.Status = StorefrontStatus.Live;
        config.PublishedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;

        // Audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Action = "Publish",
            Entity = "StorefrontConfig",
            EntityId = config.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { Status = "Live", config.PublishedAt }),
            CreatedAt = DateTime.UtcNow
        };

        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync();

        var hostname = config.Tenant.Domains.First(d => d.IsActive).Hostname;

        // Invalidar cache para forçar reload
        await TenantResolver.InvalidateCacheAsync(cache, hostname);

        return Results.Ok(new PublishStoreResponse(
            tenantId,
            hostname,
            config.Status.ToString(),
            config.PublishedAt.Value));
    }

    private static async Task<IResult> CreateStore(
        [FromBody] CreateStoreRequest request,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = request.StoreName,
            Slug = Slugify(request.StoreName),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var config = new StorefrontConfig
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StoreName = request.StoreName,
            Status = StoreStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tenants.Add(tenant);
        context.StorefrontConfigs.Add(config);
        await context.SaveChangesAsync();

        // Audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = "admin", // TODO: get from auth
            Action = "tenant.created",
            EntityType = "Tenant",
            EntityId = tenantId.ToString(),
            Details = $"Created tenant {request.StoreName}",
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync();

        return Results.Created($"/api/admin/stores/{tenantId}", new { tenantId, storeName = request.StoreName });
    }

    private static async Task<IResult> UpdateConfig(
        Guid tenantId,
        [FromBody] UpdateConfigRequest request,
        ApplicationDbContext context)
    {
        var config = await context.StorefrontConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId);
        if (config == null) return Results.NotFound();

        config.Theme = request.Theme;
        config.Currency = request.Currency;
        config.Locale = request.Locale;
        config.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<IResult> CreateDomain(
        Guid tenantId,
        [FromBody] CreateDomainRequest request,
        ApplicationDbContext context,
        IDistributedCache cache)
    {
        var hostname = $"{request.Subdomain}.localtest.me";
        if (await context.Domains.AnyAsync(d => d.Hostname == hostname))
            return Results.Conflict("Domain already exists");

        var domain = new Domain
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Hostname = hostname,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Domains.Add(domain);
        await context.SaveChangesAsync();

        // Invalidate cache
        await TenantResolver.InvalidateCacheAsync(cache, hostname);

        return Results.Created($"/api/admin/stores/{tenantId}/domain", domain);
    }

    private static string Slugify(string input)
    {
        return Regex.Replace(input.ToLower(), @"[^a-z0-9\-]", "-");
    }
}

public record CreateStoreRequest(string StoreName);
public record UpdateConfigRequest(string Theme, string Currency, string Locale);
public record CreateDomainRequest(string Subdomain);
