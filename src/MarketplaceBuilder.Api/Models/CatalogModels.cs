namespace MarketplaceBuilder.Api.Models;

// Category DTOs
public record CreateCategoryRequest(string Name, string? Description);
public record UpdateCategoryRequest(string? Name, string? Description);
public record CategoryResponse(Guid Id, Guid TenantId, string Name, string Slug, string? Description, DateTime CreatedAt, DateTime UpdatedAt);

// Product DTOs
public record CreateProductRequest(
    string Title,
    Guid? CategoryId,
    string? Description,
    string Status = "Draft"
);

public record UpdateProductRequest(
    string? Title,
    Guid? CategoryId,
    string? Description,
    string? Status
);

public record ProductListResponse(
    Guid Id,
    string Title,
    string Slug,
    string? CategoryName,
    string Status,
    string? PrimaryImageUrl,
    DateTime CreatedAt
);

public record ProductDetailResponse(
    Guid Id,
    Guid TenantId,
    Guid? CategoryId,
    string Title,
    string Slug,
    string? Description,
    string Status,
    string? PrimaryImageUrl,
    List<ProductVariantResponse> Variants,
    List<ProductImageResponse> Images,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// Variant DTOs
public record CreateVariantRequest(
    string Name,
    string? Sku,
    long PriceAmount,
    string Currency,
    int StockQty = 0,
    bool IsDefault = false
);

public record UpdateVariantRequest(
    string? Name,
    string? Sku,
    long? PriceAmount,
    string? Currency,
    int? StockQty,
    bool? IsDefault
);

public record ProductVariantResponse(
    Guid Id,
    string Name,
    string? Sku,
    long PriceAmount,
    string Currency,
    int StockQty,
    bool IsDefault,
    DateTime CreatedAt
);

// Image DTOs
public record ProductImageResponse(
    Guid Id,
    string ObjectKey,
    string PublicUrl,
    string? ContentType,
    long? SizeBytes,
    int SortOrder,
    DateTime CreatedAt
);

public record UploadImageResponse(
    Guid ImageId,
    string PublicUrl
);
