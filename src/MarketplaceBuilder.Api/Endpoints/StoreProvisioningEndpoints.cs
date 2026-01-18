using MarketplaceBuilder.Api.Helpers;
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

    internal static async Task<IResult> CreateStore(
        [FromBody] CreateStoreRequest request,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        // Validações
        if (string.IsNullOrWhiteSpace(request.StoreName))
        {
            return Results.BadRequest(new { error = "StoreName is required" });
        }

        // Gerar slug do tenant baseado no nome da loja
        var slug = SlugHelper.Slugify(request.StoreName);

        // Verificar se slug já existe
        var existingTenant = await context.Tenants
            .AnyAsync(t => t.Slug == slug);

        if (existingTenant)
        {
            return Results.Problem($"Store name '{request.StoreName}' results in duplicate slug '{slug}'", statusCode: 400);
        }

        // Criar Tenant e StorefrontConfig de forma transacional (pular para InMemory)
        var isInMemory = context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
        
        if (!isInMemory)
        {
            transaction = await context.Database.BeginTransactionAsync();
        }

        try
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.StoreName,
                Slug = slug,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Tenants.Add(tenant);

            var storefrontConfig = new StorefrontConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                StoreName = request.StoreName,
                Subdomain = "", // será definido no passo 4
                Currency = "EUR", // default
                Locale = "pt-PT", // default
                Theme = "default",
                Status = StorefrontStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.StorefrontConfigs.Add(storefrontConfig);

            // Audit logs
            var tenantAuditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Action = "Create",
                Entity = "Tenant",
                EntityId = tenant.Id,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    tenant.Name,
                    tenant.Slug
                }),
                CreatedAt = DateTime.UtcNow
            };

            var configAuditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Action = "Create",
                Entity = "StorefrontConfig",
                EntityId = storefrontConfig.Id,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    storefrontConfig.StoreName,
                    storefrontConfig.Status
                }),
                CreatedAt = DateTime.UtcNow
            };

            context.AuditLogs.Add(tenantAuditLog);
            context.AuditLogs.Add(configAuditLog);

            await context.SaveChangesAsync();
            
            if (!isInMemory && transaction != null)
            {
                await transaction.CommitAsync();
            }

            return Results.Created(
                $"/api/admin/stores/{tenant.Id}",
                new StoreResponse(tenant.Id, storefrontConfig.Status.ToString()));
        }
        catch (Exception ex)
        {
            if (!isInMemory && transaction != null)
            {
                await transaction.RollbackAsync();
            }
            return Results.Problem($"Error creating store: {ex.Message}", statusCode: 500);
        }
    }

    internal static async Task<IResult> UpdateStoreConfig(
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
        var auditLogs = new List<AuditLog>();

        if (!string.IsNullOrWhiteSpace(request.StoreName))
        {
            var oldValue = config.StoreName;
            config.StoreName = request.StoreName;
            updated = true;
            auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Action = "Update",
                Entity = "StorefrontConfig",
                EntityId = config.Id,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { StoreName = oldValue }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { StoreName = request.StoreName }),
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            var oldValue = config.Currency;
            config.Currency = request.Currency;
            updated = true;
            auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Action = "Update",
                Entity = "StorefrontConfig",
                EntityId = config.Id,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { Currency = oldValue }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Currency = request.Currency }),
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!string.IsNullOrWhiteSpace(request.Locale))
        {
            var oldValue = config.Locale;
            config.Locale = request.Locale;
            updated = true;
            auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Action = "Update",
                Entity = "StorefrontConfig",
                EntityId = config.Id,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { Locale = oldValue }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Locale = request.Locale }),
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!string.IsNullOrWhiteSpace(request.Theme))
        {
            var oldValue = config.Theme;
            config.Theme = request.Theme;
            updated = true;
            auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Action = "Update",
                Entity = "StorefrontConfig",
                EntityId = config.Id,
                OldValues = System.Text.Json.JsonSerializer.Serialize(new { Theme = oldValue }),
                NewValues = System.Text.Json.JsonSerializer.Serialize(new { Theme = request.Theme }),
                CreatedAt = DateTime.UtcNow
            });
        }

        if (updated)
        {
            config.UpdatedAt = DateTime.UtcNow;
            context.AuditLogs.AddRange(auditLogs);
            await context.SaveChangesAsync();
        }

        return Results.Ok(new StoreResponse(tenantId, config.Status.ToString()));
    }

    private static async Task<IResult> PublishStore(
        Guid tenantId,
        ApplicationDbContext context,
        IDistributedCache cache,
        IConfiguration configuration)
    {
        var config = await context.StorefrontConfigs
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId);

        if (config == null)
        {
            return Results.Problem($"Store not found for tenant {tenantId}", statusCode: 404);
        }

        if (config.Status != StorefrontStatus.Draft)
        {
            return Results.Problem("Store is not in draft status", statusCode: 400);
        }

        if (string.IsNullOrWhiteSpace(config.Subdomain))
        {
            return Results.Problem("Subdomain must be set before publishing", statusCode: 400);
        }

        var baseDomain = configuration["Platform:BaseDomain"] ?? "localtest.me";
        var hostname = $"{config.Subdomain}.{baseDomain}";

        // Verificar se domain existe
        var domain = await context.Domains
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Hostname == hostname);

        if (domain == null)
        {
            return Results.Problem("Domain not found. Please create domain first.", statusCode: 400);
        }

        // Publicar
        config.Status = StorefrontStatus.Live;
        config.PublishedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // Invalidar cache
        await TenantResolver.InvalidateCacheAsync(cache, hostname);

        return Results.Ok(new PublishStoreResponse(
            tenantId,
            hostname,
            config.Status.ToString(),
            config.PublishedAt.Value));
    }
}
