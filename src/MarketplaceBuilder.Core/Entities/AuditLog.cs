namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Registro de auditoria para rastreamento de operações
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
