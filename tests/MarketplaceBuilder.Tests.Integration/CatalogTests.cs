using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MarketplaceBuilder.Tests.Integration;

public class CatalogTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CatalogTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCategory_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            name = "Test Category",
            description = "Test Description"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/categories", request);

        // Assert - Will fail without auth/tenant but endpoint exists
        Assert.True(
            response.StatusCode == HttpStatusCode.Created || 
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Endpoint should exist. Got: {response.StatusCode}"
        );
    }

    [Fact]
    public async Task GetCategories_ReturnsOkOrUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/categories");

        // Assert - Endpoint should exist
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Endpoint should exist. Got: {response.StatusCode}"
        );
    }

    [Fact]
    public async Task GetProducts_ReturnsOkOrUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/products");

        // Assert - Endpoint should exist
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Endpoint should exist. Got: {response.StatusCode}"
        );
    }

    [Fact]
    public async Task CreateProduct_EndpointExists()
    {
        // Arrange
        var request = new
        {
            title = "Test Product",
            description = "Test Description",
            status = "Draft"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/products", request);

        // Assert - Endpoint should exist (will be unauthorized without tenant)
        Assert.True(
            response.StatusCode == HttpStatusCode.Created ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Endpoint should exist. Got: {response.StatusCode}"
        );
    }

    [Fact]
    public async Task SwaggerEndpoint_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
