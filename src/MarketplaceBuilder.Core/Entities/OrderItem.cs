namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Item de um pedido (snapshot no momento da compra)
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// Referências aos produtos (podem ser deletados depois)
    /// </summary>
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    
    /// <summary>
    /// Snapshot do título no momento da compra
    /// </summary>
    public string TitleSnapshot { get; set; } = string.Empty;
    
    /// <summary>
    /// Snapshot do SKU no momento da compra
    /// </summary>
    public string? SkuSnapshot { get; set; }
    
    /// <summary>
    /// Preço unitário em minor units no momento da compra
    /// </summary>
    public long UnitPriceAmount { get; set; }
    
    /// <summary>
    /// Quantidade comprada
    /// </summary>
    public int Quantity { get; set; }
    
    /// <summary>
    /// Moeda
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// Total da linha (UnitPriceAmount * Quantity)
    /// </summary>
    public long LineTotalAmount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Order Order { get; set; } = null!;
}
