using MarketplaceBuilder.Api.Helpers;
using MarketplaceBuilder.Api.Models;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceBuilder.Api.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin/categories")
            .WithTags("Categories");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);
    }

    private static async Task<IResult> GetAll(
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var categories = await context.Categories
            .Where(c => c.TenantId == tenantId.Value)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var response = categories.Select(c => new CategoryResponse(
            c.Id, c.TenantId, c.Name, c.Slug, c.Description, c.CreatedAt, c.UpdatedAt));

        return Results.Ok(response);
    }

    private static async Task<IResult> GetById(
        Guid id,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value);

        if (category == null)
            return Results.NotFound();

        return Results.Ok(new CategoryResponse(
            category.Id, category.TenantId, category.Name, category.Slug,
            category.Description, category.CreatedAt, category.UpdatedAt));
    }

    private static async Task<IResult> Create(
        [FromBody] CreateCategoryRequest request,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.Problem("Name is required", statusCode: 400);

        var slug = SlugHelper.Slugify(request.Name);

        if (slug.Length < 3)
            return Results.Problem("Name must generate a slug of at least 3 characters", statusCode: 400);

        // Check slug uniqueness per tenant
        var exists = await context.Categories
            .AnyAsync(c => c.TenantId == tenantId.Value && c.Slug == slug);

        if (exists)
            return Results.Problem($"Category with slug '{slug}' already exists", statusCode: 400);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);

        // Audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Action = "Create",
            Entity = "Category",
            EntityId = category.Id,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { category.Name, category.Slug }),
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync();

        return Results.Created(
            $"/api/admin/categories/{category.Id}",
            new CategoryResponse(category.Id, category.TenantId, category.Name,
                category.Slug, category.Description, category.CreatedAt, category.UpdatedAt));
    }

    private static async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value);

        if (category == null)
            return Results.NotFound();

        var updated = false;

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != category.Name)
        {
            var newSlug = SlugHelper.Slugify(request.Name);

            // Check new slug uniqueness
            var slugExists = await context.Categories
                .AnyAsync(c => c.TenantId == tenantId.Value && c.Slug == newSlug && c.Id != id);

            if (slugExists)
                return Results.Problem($"Category with slug '{newSlug}' already exists", statusCode: 400);

            category.Name = request.Name;
            category.Slug = newSlug;
            updated = true;
        }

        if (request.Description != category.Description)
        {
            category.Description = request.Description;
            updated = true;
        }

        if (updated)
        {
            category.UpdatedAt = DateTime.UtcNow;

            // Audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                Action = "Update",
                Entity = "Category",
                EntityId = category.Id,
                NewValues = System.Text.Json.JsonSerializer.Serialize(request),
                CreatedAt = DateTime.UtcNow
            };
            context.AuditLogs.Add(auditLog);

            await context.SaveChangesAsync();
        }

        return Results.Ok(new CategoryResponse(
            category.Id, category.TenantId, category.Name, category.Slug,
            category.Description, category.CreatedAt, category.UpdatedAt));
    }

    private static async Task<IResult> Delete(
        Guid id,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        var tenantId = (Guid?)httpContext.Items["TenantId"];
        if (!tenantId.HasValue)
            return Results.Problem("Tenant not resolved", statusCode: 401);

        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId.Value);

        if (category == null)
            return Results.NotFound();

        // Check if category has products
        var hasProducts = await context.Products
            .AnyAsync(p => p.CategoryId == id);

        if (hasProducts)
            return Results.Problem("Cannot delete category with products. Remove products first or set category to null.", statusCode: 400);

        context.Categories.Remove(category);

        // Audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Action = "Delete",
            Entity = "Category",
            EntityId = category.Id,
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { category.Name, category.Slug }),
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);

        await context.SaveChangesAsync();

        return Results.NoContent();
    }
}
