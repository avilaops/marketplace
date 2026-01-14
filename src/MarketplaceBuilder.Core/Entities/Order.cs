namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Status do pedido
/// </summary>
public enum OrderStatus
{
    Pending,   // Criado, aguardando pagamento
    Paid,      // Pagamento confirmado
    Failed,    // Pagamento falhou
    Refunded,  // Reembolsado
    Canceled   // Cancelado manualmente
}

/// <summary>
/// Pedido de compra
/// </summary>
public class Order
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    /// <summary>
    /// Código da moeda ISO 4217
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// Subtotal (soma dos line totals) em minor units
    /// </summary>
    public long SubtotalAmount { get; set; }
    
    /// <summary>
    /// Total final em minor units (por enquanto = subtotal, futuro: + shipping/tax)
    /// </summary>
    public long TotalAmount { get; set; }
    
    /// <summary>
    /// Email do cliente (opcional)
    /// </summary>
    public string? CustomerEmail { get; set; }
    
    /// <summary>
    /// ID da Checkout Session do Stripe
    /// </summary>
    public string? StripeCheckoutSessionId { get; set; }
    
    /// <summary>
    /// ID do PaymentIntent do Stripe (preenchido quando disponível)
    /// </summary>
    public string? StripePaymentIntentId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
