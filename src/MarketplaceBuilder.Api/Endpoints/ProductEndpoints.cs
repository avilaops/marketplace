using MarketplaceBuilder.Api.Helpers;
using MarketplaceBuilder.Api.Models;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.AI;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace MarketplaceBuilder.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin/products")
            .WithTags("Products");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);
        group.MapPost("/{id:guid}/generate-description", GenerateDescription);
    }

    private static async Task<IResult> GetAll(
        ApplicationDbContext context,
        HttpContext httpContext,
        [FromQuery] string? query = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var productsQuery = context.Products
            .Where(p => p.TenantId == tenantId.Value)
            .Include(p => p.Category)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLowerInvariant();
            productsQuery = productsQuery.Where(p =>
                p.Title.ToLower().Contains(lowerQuery) ||
                (p.Description != null && p.Description.ToLower().Contains(lowerQuery)));
        }

        if (categoryId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProductStatus>(status, true, out var productStatus))
        {
            productsQuery = productsQuery.Where(p => p.Status == productStatus);
        }

        var total = await productsQuery.CountAsync();

        var products = await productsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = products.Select(p => new ProductListResponse(
            p.Id,
            p.Title,
            p.Slug,
            p.Category?.Name,
            p.Status.ToString(),
            p.PrimaryImageUrl,
            p.CreatedAt
        ));

        return Results.Ok(new
        {
            items = response,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    private static async Task<IResult> GetById(
        Guid id,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var product = await context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);

        if (product == null)
            return Results.NotFound();

        var response = new ProductDetailResponse(
            product.Id,
            product.TenantId,
            product.CategoryId,
            product.Title,
            product.Slug,
            product.Description,
            product.Status.ToString(),
            product.PrimaryImageUrl,
            product.Variants.Select(v => new ProductVariantResponse(
                v.Id, v.Name, v.Sku, v.PriceAmount, v.Currency, v.StockQty, v.IsDefault, v.CreatedAt
            )).ToList(),
            product.Images.Select(i => new ProductImageResponse(
                i.Id, i.ObjectKey, i.PublicUrl, i.ContentType, i.SizeBytes, i.SortOrder, i.CreatedAt
            )).ToList(),
            product.CreatedAt,
            product.UpdatedAt
        );

        return Results.Ok(response);
    }

    private static async Task<IResult> Create(
        [FromBody] CreateProductRequest request,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.Problem("Title is required", statusCode: 400);

        var slug = SlugHelper.Slugify(request.Title);

        if (slug.Length < 3)
            return Results.Problem("Title must generate a slug of at least 3 characters", statusCode: 400);

        // Check slug uniqueness per tenant
        var exists = await context.Products
            .AnyAsync(p => p.TenantId == tenantId.Value && p.Slug == slug);

        if (exists)
            return Results.Problem($"Product with slug '{slug}' already exists", statusCode: 400);

        // Validate category if provided
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await context.Categories
                .AnyAsync(c => c.Id == request.CategoryId.Value && c.TenantId == tenantId.Value);

            if (!categoryExists)
                return Results.Problem("Category not found", statusCode: 404);
        }

        // Parse status
        if (!Enum.TryParse<ProductStatus>(request.Status, true, out var productStatus))
        {
            productStatus = ProductStatus.Draft;
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            CategoryId = request.CategoryId,
            Title = request.Title,
            Slug = slug,
            Description = request.Description,
            Status = productStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);

        // Audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Action = "Create",
            Entity = "Product",
            EntityId = product.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                product.Title,
                product.Slug,
                product.Status,
                product.CategoryId
            }),
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync();

        return Results.Created(
            $"/api/admin/products/{product.Id}",
            new ProductDetailResponse(
                product.Id, product.TenantId, product.CategoryId, product.Title, product.Slug,
                product.Description, product.Status.ToString(), product.PrimaryImageUrl,
                new List<ProductVariantResponse>(), new List<ProductImageResponse>(),
                product.CreatedAt, product.UpdatedAt));
    }

    private static async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);

        if (product == null)
            return Results.NotFound();

        var updated = false;

        if (!string.IsNullOrWhiteSpace(request.Title) && request.Title != product.Title)
        {
            var newSlug = SlugHelper.Slugify(request.Title);
            var slugExists = await context.Products
                .AnyAsync(p => p.TenantId == tenantId.Value && p.Slug == newSlug && p.Id != id);

            if (slugExists)
                return Results.Problem($"Product with slug '{newSlug}' already exists", statusCode: 400);

            product.Title = request.Title;
            product.Slug = newSlug;
            updated = true;
        }

        if (request.CategoryId != product.CategoryId)
        {
            if (request.CategoryId.HasValue)
            {
                var categoryExists = await context.Categories
                    .AnyAsync(c => c.Id == request.CategoryId.Value && c.TenantId == tenantId.Value);

                if (!categoryExists)
                    return Results.Problem("Category not found", statusCode: 404);
            }

            product.CategoryId = request.CategoryId;
            updated = true;
        }

        if (request.Description != product.Description)
        {
            product.Description = request.Description;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ProductStatus>(request.Status, true, out var newStatus) &&
            newStatus != product.Status)
        {
            product.Status = newStatus;
            updated = true;
        }

        if (updated)
        {
            product.UpdatedAt = DateTime.UtcNow;

            // Audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                Action = "Update",
                Entity = "Product",
                EntityId = product.Id,
                NewValues = System.Text.Json.JsonSerializer.Serialize(request),
                CreatedAt = DateTime.UtcNow
            };
            context.AuditLogs.Add(auditLog);

            await context.SaveChangesAsync();
        }

        // Reload with relations
        product = await context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstAsync(p => p.Id == id);

        return Results.Ok(new ProductDetailResponse(
            product.Id, product.TenantId, product.CategoryId, product.Title, product.Slug,
            product.Description, product.Status.ToString(), product.PrimaryImageUrl,
            product.Variants.Select(v => new ProductVariantResponse(
                v.Id, v.Name, v.Sku, v.PriceAmount, v.Currency, v.StockQty, v.IsDefault, v.CreatedAt
            )).ToList(),
            product.Images.Select(i => new ProductImageResponse(
                i.Id, i.ObjectKey, i.PublicUrl, i.ContentType, i.SizeBytes, i.SortOrder, i.CreatedAt
            )).ToList(),
            product.CreatedAt, product.UpdatedAt));
    }

    private static async Task<IResult> Delete(
        Guid id,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var product = await context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);

        if (product == null)
            return Results.NotFound();

        // Cascade delete will handle variants and images via EF Core configuration
        context.Products.Remove(product);

        // Audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Action = "Delete",
            Entity = "Product",
            EntityId = product.Id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                product.Title,
                product.Slug,
                VariantsCount = product.Variants.Count,
                ImagesCount = product.Images.Count
            }),
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> GenerateDescription(
        Guid id,
        ApplicationDbContext context,
        AiRunner aiRunner,
        AiUsageRecorder usageRecorder,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var product = await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId.Value);

        if (product == null)
            return Results.NotFound();

        // Check AI settings
        var aiSettings = await context.TenantAiSettings.FindAsync(tenantId.Value);
        if (aiSettings == null || !aiSettings.Enabled)
            return Results.Problem("AI not enabled for this tenant", statusCode: 403);

        // Get prompt
        var prompt = await context.AiPrompts.FirstOrDefaultAsync(p => p.Name == "generate-product-description");
        if (prompt == null)
            return Results.Problem("AI prompt not found", statusCode: 500);

        // Render prompt
        var variables = new Dictionary<string, string>
        {
            ["nome"] = product.Title,
            ["categoria"] = product.Category?.Name ?? "Geral",
            ["idioma"] = "português" // TODO: from tenant settings
        };
        var renderedPrompt = AiPromptRenderer.Render(prompt.Template, variables);

        // Run AI
        var result = await aiRunner.RunAsync(renderedPrompt);

        // Record usage
        var inputHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(renderedPrompt)));
        await usageRecorder.RecordAsync(tenantId.Value, prompt.Id, inputHash, result, Guid.NewGuid().ToString());

        return Results.Ok(new
        {
            description = result.Output,
            tokens = result.TokensUsed,
            cost = result.CostUsd
        });
    }
}
