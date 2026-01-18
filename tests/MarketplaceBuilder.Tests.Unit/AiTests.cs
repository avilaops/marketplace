using MarketplaceBuilder.Infrastructure.AI;
using Moq;
using OpenAI.Chat;

namespace MarketplaceBuilder.Tests.Unit;

public class AiTests
{
    [Fact]
    public async Task AiRunner_RunAsync_ReturnsResult()
    {
        // Arrange
        var mockClient = new Mock<ChatClient>();
        var mockResponse = new Mock<ChatCompletion>();
        mockResponse.Setup(r => r.Content).Returns([new ChatMessageContent(ChatMessageRole.Assistant, "Generated description")]);
        mockResponse.Setup(r => r.Usage).Returns(new ChatTokenUsage()); // Mock usage

        mockClient.Setup(c => c.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatCompletionOptions?>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(ChatResult.Generate(mockResponse.Object, null));

        var runner = new AiRunner(mockClient.Object);

        // Act
        var result = await runner.RunAsync("Test prompt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Generated description", result.Output);
        Assert.Equal(0, result.TokensUsed); // Since mocked
    }
}