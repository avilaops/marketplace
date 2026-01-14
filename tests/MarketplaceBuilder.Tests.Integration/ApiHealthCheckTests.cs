using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MarketplaceBuilder.Tests.Integration;

public class ApiHealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiHealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy_WhenServicesAreAvailable()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task RootEndpoint_ReturnsServiceInfo()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("MarketplaceBuilder API", content);
        Assert.Contains("version", content);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/")]
    public async Task Endpoint_Returns200(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        Assert.True(
            response.IsSuccessStatusCode,
            $"Endpoint {endpoint} returned {response.StatusCode}"
        );
    }
}
