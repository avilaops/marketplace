namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Variante de produto (tamanho, cor, etc.) com preço e estoque próprios
/// </summary>
public class ProductVariant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// Nome da variante (ex: "Padrão", "Tamanho M", "Cor Azul")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// SKU (Stock Keeping Unit) - código único
    /// </summary>
    public string? Sku { get; set; }
    
    /// <summary>
    /// Preço em minor units (centavos/cents)
    /// Ex: €10.50 = 1050
    /// </summary>
    public long PriceAmount { get; set; }
    
    /// <summary>
    /// Código da moeda ISO 4217 (EUR, USD, BRL, etc.)
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// Quantidade em estoque
    /// </summary>
    public int StockQty { get; set; } = 0;
    
    /// <summary>
    /// Indica se é a variante padrão (deve haver exatamente 1 por produto)
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Product Product { get; set; } = null!;
}
