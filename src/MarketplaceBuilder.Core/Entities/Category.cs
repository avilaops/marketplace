namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Categoria de produtos no catálogo
/// </summary>
public class Category
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Slug único por tenant para URLs amigáveis
    /// </summary>
    public string Slug { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
