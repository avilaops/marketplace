using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MarketplaceBuilder.Tests.Integration;

public class ApiHealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiHealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Test");
        });
        _client = _factory.CreateClient();
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

    [Fact]
    public async Task TenantResolver_DoesNotThrow_WhenHostIsLocalhost()
    {
        // Arrange: Create client with localhost host header
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Host = "localhost";

        // Act: Call an endpoint that goes through tenant resolver middleware
        var response = await client.GetAsync("/");

        // Assert: Should return 200 OK, not throw exception
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
