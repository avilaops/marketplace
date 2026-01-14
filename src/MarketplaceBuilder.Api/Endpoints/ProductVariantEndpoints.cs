using MarketplaceBuilder.Api.Models;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceBuilder.Api.Endpoints;

public static class ProductVariantEndpoints
{
    public static void MapProductVariantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin/products/{productId:guid}/variants")
            .WithTags("Product Variants");

        group.MapGet("/", GetAll);
        group.MapPost("/", Create);
        group.MapPut("/{variantId:guid}", Update);
        group.MapDelete("/{variantId:guid}", Delete);
    }

    private static async Task<IResult> GetAll(
        Guid productId,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        // Verify product belongs to tenant
        var productExists = await context.Products
            .AnyAsync(p => p.Id == productId && p.TenantId == tenantId.Value);

        if (!productExists)
            return Results.NotFound(new { message = "Product not found" });

        var variants = await context.ProductVariants
            .Where(v => v.ProductId == productId)
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.Name)
            .ToListAsync();

        var response = variants.Select(v => new ProductVariantResponse(
            v.Id, v.Name, v.Sku, v.PriceAmount, v.Currency, v.StockQty, v.IsDefault, v.CreatedAt
        ));

        return Results.Ok(response);
    }

    private static async Task<IResult> Create(
        Guid productId,
        [FromBody] CreateVariantRequest request,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        // Verify product belongs to tenant
        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId.Value);

        if (product == null)
            return Results.NotFound(new { message = "Product not found" });

        // Validations
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.Problem("Name is required", statusCode: 400);

        if (request.PriceAmount < 0)
            return Results.Problem("Price amount must be >= 0", statusCode: 400);

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
            return Results.Problem("Currency must be a valid 3-letter ISO code", statusCode: 400);

        // Check if this will be the first variant (auto-default)
        var hasVariants = await context.ProductVariants
            .AnyAsync(v => v.ProductId == productId);

        var isDefault = request.IsDefault || !hasVariants;

        // If setting as default, unset others
        if (isDefault)
        {
            var existingVariants = await context.ProductVariants
                .Where(v => v.ProductId == productId && v.IsDefault)
                .ToListAsync();

            foreach (var v in existingVariants)
            {
                v.IsDefault = false;
            }
        }

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            ProductId = productId,
            Name = request.Name,
            Sku = request.Sku,
            PriceAmount = request.PriceAmount,
            Currency = request.Currency.ToUpperInvariant(),
            StockQty = request.StockQty,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.ProductVariants.Add(variant);

        // Audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Action = "Create",
            Entity = "ProductVariant",
            EntityId = variant.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                variant.ProductId,
                variant.Name,
                variant.Sku,
                variant.PriceAmount,
                variant.Currency,
                variant.IsDefault
            }),
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync();

        return Results.Created(
            $"/api/admin/products/{productId}/variants/{variant.Id}",
            new ProductVariantResponse(
                variant.Id, variant.Name, variant.Sku, variant.PriceAmount,
                variant.Currency, variant.StockQty, variant.IsDefault, variant.CreatedAt));
    }

    private static async Task<IResult> Update(
        Guid productId,
        Guid variantId,
        [FromBody] UpdateVariantRequest request,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var variant = await context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId && v.TenantId == tenantId.Value);

        if (variant == null)
            return Results.NotFound();

        var updated = false;

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != variant.Name)
        {
            variant.Name = request.Name;
            updated = true;
        }

        if (request.Sku != variant.Sku)
        {
            variant.Sku = request.Sku;
            updated = true;
        }

        if (request.PriceAmount.HasValue && request.PriceAmount.Value >= 0 && request.PriceAmount.Value != variant.PriceAmount)
        {
            variant.PriceAmount = request.PriceAmount.Value;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Currency) && request.Currency.Length == 3 && request.Currency.ToUpperInvariant() != variant.Currency)
        {
            variant.Currency = request.Currency.ToUpperInvariant();
            updated = true;
        }

        if (request.StockQty.HasValue && request.StockQty.Value != variant.StockQty)
        {
            variant.StockQty = request.StockQty.Value;
            updated = true;
        }

        if (request.IsDefault.HasValue && request.IsDefault.Value && !variant.IsDefault)
        {
            // Unset other defaults
            var otherVariants = await context.ProductVariants
                .Where(v => v.ProductId == productId && v.Id != variantId && v.IsDefault)
                .ToListAsync();

            foreach (var v in otherVariants)
            {
                v.IsDefault = false;
            }

            variant.IsDefault = true;
            updated = true;
        }

        if (updated)
        {
            variant.UpdatedAt = DateTime.UtcNow;

            // Audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                Action = "Update",
                Entity = "ProductVariant",
                EntityId = variant.Id,
                NewValues = System.Text.Json.JsonSerializer.Serialize(request),
                CreatedAt = DateTime.UtcNow
            };
            context.AuditLogs.Add(auditLog);

            await context.SaveChangesAsync();
        }

        return Results.Ok(new ProductVariantResponse(
            variant.Id, variant.Name, variant.Sku, variant.PriceAmount,
            variant.Currency, variant.StockQty, variant.IsDefault, variant.CreatedAt));
    }

    private static async Task<IResult> Delete(
        Guid productId,
        Guid variantId,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var variant = await context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId && v.TenantId == tenantId.Value);

        if (variant == null)
            return Results.NotFound();

        // Check if it's the only variant
        var variantsCount = await context.ProductVariants
            .CountAsync(v => v.ProductId == productId);

        if (variantsCount == 1)
            return Results.Problem("Cannot delete the only variant. Products must have at least one variant.", statusCode: 400);

        // If deleting default, set another as default
        if (variant.IsDefault)
        {
            var nextVariant = await context.ProductVariants
                .Where(v => v.ProductId == productId && v.Id != variantId)
                .FirstOrDefaultAsync();

            if (nextVariant != null)
            {
                nextVariant.IsDefault = true;
            }
        }

        context.ProductVariants.Remove(variant);

        // Audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Action = "Delete",
            Entity = "ProductVariant",
            EntityId = variant.Id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                variant.Name,
                variant.Sku,
                variant.PriceAmount,
                variant.Currency
            }),
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync();

        return Results.NoContent();
    }
}
