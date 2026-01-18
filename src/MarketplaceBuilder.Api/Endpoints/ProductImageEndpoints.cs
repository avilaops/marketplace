using MarketplaceBuilder.Api.Models;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Core.Interfaces;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceBuilder.Api.Endpoints;

public static class ProductImageEndpoints
{
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public static void MapProductImageEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin/products/{productId:guid}/images")
            .WithTags("Product Images")
            .DisableAntiforgery(); // Required for file uploads

        group.MapGet("/", GetAll);
        group.MapPost("/", Upload);
        group.MapDelete("/{imageId:guid}", Delete);
        group.MapPut("/{imageId:guid}/sort-order", UpdateSortOrder);
    }

    private static async Task<IResult> GetAll(
        Guid productId,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var images = await context.ProductImages
            .Where(i => i.ProductId == productId && i.TenantId == tenantId.Value)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ProductImageResponse(
                i.Id,
                i.ObjectKey,
                i.PublicUrl,
                i.ContentType,
                i.SizeBytes,
                i.SortOrder,
                i.CreatedAt
            ))
            .ToListAsync();

        return Results.Ok(images);
    }

    private static async Task<IResult> Upload(
        Guid productId,
        IFormFile file,
        ApplicationDbContext context,
        IObjectStorage storage,
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
        if (file == null || file.Length == 0)
            return Results.Problem("File is required", statusCode: 400);

        if (file.Length > MaxFileSizeBytes)
            return Results.Problem($"File size exceeds {MaxFileSizeBytes / 1024 / 1024}MB limit", statusCode: 400);

        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            return Results.Problem($"File type not allowed. Allowed types: {string.Join(", ", AllowedContentTypes)}", statusCode: 400);

        try
        {
            // Generate unique key
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                extension = file.ContentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    "image/gif" => ".gif",
                    _ => ".jpg"
                };

            var uniqueId = Guid.NewGuid().ToString("N");
            var objectKey = $"tenants/{tenantId.Value}/products/{productId}/{uniqueId}{extension}";

            // Upload to S3
            UploadResult uploadResult;
            using (var stream = file.OpenReadStream())
            {
                uploadResult = await storage.UploadAsync(
                    tenantId.Value,
                    objectKey,
                    stream,
                    file.ContentType,
                    httpContext.RequestAborted);
            }

            // Get next sort order
            var maxSortOrder = await context.ProductImages
                .Where(i => i.ProductId == productId)
                .MaxAsync(i => (int?)i.SortOrder) ?? -1;

            var productImage = new ProductImage
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                ProductId = productId,
                ObjectKey = uploadResult.ObjectKey,
                PublicUrl = uploadResult.PublicUrl,
                ContentType = uploadResult.ContentType,
                SizeBytes = uploadResult.SizeBytes,
                SortOrder = maxSortOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            context.ProductImages.Add(productImage);

            // If this is the first image, set as primary
            if (!await context.ProductImages.AnyAsync(i => i.ProductId == productId && i.Id != productImage.Id))
            {
                product.PrimaryImageUrl = uploadResult.PublicUrl;
                product.UpdatedAt = DateTime.UtcNow;
            }

            // Audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                Action = "Upload",
                Entity = "ProductImage",
                EntityId = productImage.Id,
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    productImage.ProductId,
                    productImage.ObjectKey,
                    productImage.SizeBytes,
                    productImage.ContentType
                }),
                CreatedAt = DateTime.UtcNow
            };
            context.AuditLogs.Add(auditLog);

            await context.SaveChangesAsync();

            return Results.Created(
                $"/api/admin/products/{productId}/images/{productImage.Id}",
                new UploadImageResponse(productImage.Id, productImage.PublicUrl));
        }
        catch (Exception ex)
        {
            return Results.Problem($"Error uploading image: {ex.Message}", statusCode: 500);
        }
    }

    private static async Task<IResult> Delete(
        Guid productId,
        Guid imageId,
        ApplicationDbContext context,
        IObjectStorage storage,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var productImage = await context.ProductImages
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId && i.TenantId == tenantId.Value);

        if (productImage == null)
            return Results.NotFound();

        try
        {
            // Delete from S3
            await storage.DeleteAsync(productImage.ObjectKey, httpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            // Log but continue - DB cleanup is more important
            // In production, consider a cleanup job for orphaned S3 objects
            Console.WriteLine($"Warning: Failed to delete S3 object {productImage.ObjectKey}: {ex.Message}");
        }

        // If this was the primary image, update product
        if (productImage.Product.PrimaryImageUrl == productImage.PublicUrl)
        {
            // Set next image as primary
            var nextImage = await context.ProductImages
                .Where(i => i.ProductId == productId && i.Id != imageId)
                .OrderBy(i => i.SortOrder)
                .FirstOrDefaultAsync();

            productImage.Product.PrimaryImageUrl = nextImage?.PublicUrl;
            productImage.Product.UpdatedAt = DateTime.UtcNow;
        }

        context.ProductImages.Remove(productImage);

        // Audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Action = "Delete",
            Entity = "ProductImage",
            EntityId = productImage.Id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                productImage.ObjectKey,
                productImage.PublicUrl
            }),
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> UpdateSortOrder(
        Guid productId,
        Guid imageId,
        [FromBody] UpdateSortOrderRequest request,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var productImage = await context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId && i.TenantId == tenantId.Value);

        if (productImage == null)
            return Results.NotFound();

        productImage.SortOrder = request.SortOrder;
        await context.SaveChangesAsync();

        return Results.Ok(new { imageId, sortOrder = request.SortOrder });
    }
}

public record UpdateSortOrderRequest(int SortOrder);
