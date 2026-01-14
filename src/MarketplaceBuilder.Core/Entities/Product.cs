namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Status de visibilidade do produto
/// </summary>
public enum ProductStatus
{
    Draft,
    Active,
    Archived
}

/// <summary>
/// Produto no catálogo
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CategoryId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Slug único por tenant para URLs amigáveis
    /// </summary>
    public string Slug { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    
    /// <summary>
    /// URL da imagem principal (primeira ou selecionada)
    /// </summary>
    public string? PrimaryImageUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public Category? Category { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
