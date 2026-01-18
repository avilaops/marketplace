using System.Text.RegularExpressions;

namespace MarketplaceBuilder.Infrastructure.AI;

public class AiPromptRenderer
{
    public static string Render(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var kvp in variables)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }
        return result;
    }
}