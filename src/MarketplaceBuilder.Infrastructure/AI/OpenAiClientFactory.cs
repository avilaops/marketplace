using OpenAI;
using OpenAI.Chat;
using System.Net.Http;

namespace MarketplaceBuilder.Infrastructure.AI;

public class OpenAiClientFactory
{
    public static ChatClient CreateChatClient(string apiKey)
    {
        var client = new OpenAIClient(apiKey);
        return client.GetChatClient("gpt-4o-mini"); // Default model
    }
}