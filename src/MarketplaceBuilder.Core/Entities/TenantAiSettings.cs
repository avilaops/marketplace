using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceBuilder.Core.Entities;

[Table("tenant_ai_settings")]
public class TenantAiSettings
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; }

    public bool Enabled { get; set; } = true;
    public string ModelDefault { get; set; } = "gpt-4o-mini";
    public decimal BudgetMonthly { get; set; } = 10.0m; // USD
    public string? ApiKey { get; set; } // Override opcional
}