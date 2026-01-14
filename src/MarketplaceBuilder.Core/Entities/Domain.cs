namespace MarketplaceBuilder.Core.Entities;

/// <summary>
/// Representa um domínio/hostname associado a um tenant
/// Usado para resolução de tenant por Host header
/// </summary>
public class Domain
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Hostname completo (ex: minhaloja.localtest.me)
    /// Deve ser único globalmente
    /// </summary>
    public string Hostname { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Tenant Tenant { get; set; } = null!;
}
