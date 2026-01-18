using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceBuilder.Core.Entities;

[Table("ai_prompts")]
public class AiPrompt
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Version { get; set; } = 1;
    public string Template { get; set; }
    public string VariablesSchema { get; set; } // JSON
    public string Channel { get; set; } // "admin", "storefront", etc.
}