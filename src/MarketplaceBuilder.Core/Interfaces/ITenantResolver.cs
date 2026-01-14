namespace MarketplaceBuilder.Core.Interfaces;

/// <summary>
/// Interface para resolução de tenant no contexto da requisição
/// </summary>
public interface ITenantResolver
{
    Guid? TenantId { get; }
    Task<Guid?> ResolveTenantAsync(string hostname);
}
