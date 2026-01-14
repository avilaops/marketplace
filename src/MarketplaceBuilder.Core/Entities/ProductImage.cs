namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Imagem de produto armazenada em S3
/// </summary>
public class ProductImage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// Chave do objeto no S3 (path completo)
    /// Ex: tenants/{tenantId}/products/{productId}/{uuid}.jpg
    /// </summary>
    public string ObjectKey { get; set; } = string.Empty;
    
    /// <summary>
    /// URL pública completa para acesso
    /// Ex: https://cdn.example.com/marketplace/tenants/.../image.jpg
    /// </summary>
    public string PublicUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo MIME (image/jpeg, image/png, etc.)
    /// </summary>
    public string? ContentType { get; set; }
    
    /// <summary>
    /// Tamanho do arquivo em bytes
    /// </summary>
    public long? SizeBytes { get; set; }
    
    /// <summary>
    /// Ordem de exibição (menor = primeiro)
    /// </summary>
    public int SortOrder { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Product Product { get; set; } = null!;
}
