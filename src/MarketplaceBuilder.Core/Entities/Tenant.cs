namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Representa um tenant (cliente/loja) no sistema multi-tenant
/// </summary>
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Domain> Domains { get; set; } = new List<Domain>();
    public StorefrontConfig? StorefrontConfig { get; set; }
}
