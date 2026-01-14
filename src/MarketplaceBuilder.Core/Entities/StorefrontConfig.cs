namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Status de publicação da loja
/// </summary>
public enum StorefrontStatus
{
    Draft,
    Live
}

/// <summary>
/// Configuração da vitrine/storefront de um tenant
/// </summary>
public class StorefrontConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public string StoreName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    
    /// <summary>
    /// Código da moeda ISO 4217 (ex: EUR, USD, BRL)
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// Código do idioma/locale (ex: pt-PT, en-US, pt-BR)
    /// </summary>
    public string Locale { get; set; } = "en-US";
    
    /// <summary>
    /// Tema visual da loja
    /// </summary>
    public string Theme { get; set; } = "default";
    
    public StorefrontStatus Status { get; set; } = StorefrontStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Tenant Tenant { get; set; } = null!;
}
