using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceBuilder.Core.Entities;

[Table("ai_runs")]
public class AiRun
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; }

    public int PromptId { get; set; }
    public AiPrompt Prompt { get; set; }

    public string InputHash { get; set; }
    public string Output { get; set; }
    public string Model { get; set; }
    public int TokensUsed { get; set; }
    public decimal CostUsd { get; set; }
    public string CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
}