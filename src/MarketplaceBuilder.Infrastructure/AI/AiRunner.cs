using OpenAI.Chat;

namespace MarketplaceBuilder.Infrastructure.AI;

public class AiRunner
{
    private readonly ChatClient _client;

    public AiRunner(ChatClient client)
    {
        _client = client;
    }

    public async Task<AiRunResult> RunAsync(string prompt, string model = "gpt-4o-mini")
    {
        var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
        var response = await _client.CompleteChatAsync(messages);

        var output = response.Value.Content[0].Text;
        var tokens = 0; // TODO: calcular tokens corretamente

        return new AiRunResult
        {
            Output = output,
            TokensUsed = tokens,
            CostUsd = EstimateCost(model, tokens)
        };
    }

    private decimal EstimateCost(string model, int tokens)
    {
        // Estimativa simples: gpt-4o-mini ~$0.15/1M tokens input, $0.60 output (aprox.)
        // Ajustar conforme pricing real
        return (decimal)tokens * 0.00000015m; // Exemplo
    }
}

public class AiRunResult
{
    public required string Output { get; set; }
    public int TokensUsed { get; set; }
    public decimal CostUsd { get; set; }
}