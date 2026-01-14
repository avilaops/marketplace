namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Status de processamento do webhook event
/// </summary>
public enum WebhookProcessingStatus
{
    Received,   // Recebido mas não processado
    Processed,  // Processado com sucesso
    Failed      // Falhou ao processar
}

/// <summary>
/// Registro de webhook events do Stripe (para idempotência)
/// </summary>
public class StripeWebhookEvent
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    
    /// <summary>
    /// ID do evento do Stripe (ex: evt_1234...)
    /// DEVE ser unique global para garantir idempotência
    /// </summary>
    public string StripeEventId { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo do evento (ex: checkout.session.completed)
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Quando o webhook foi recebido
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Quando foi processado (nullable se ainda não)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Status do processamento
    /// </summary>
    public WebhookProcessingStatus ProcessingStatus { get; set; } = WebhookProcessingStatus.Received;
    
    /// <summary>
    /// Mensagem de erro se falhou
    /// </summary>
    public string? Error { get; set; }
}
