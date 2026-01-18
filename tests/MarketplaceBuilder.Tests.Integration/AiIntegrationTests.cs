using System.Net;
using System.Net.Http.Json;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceBuilder.Tests.Integration;

public class AiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Test");
            builder.UseSetting("OPENAI_API_KEY", "sk-test-key"); // Mock key
        });
        _client = _factory.CreateClient();

        VerifyDatabaseSchema();
    }

    private void VerifyDatabaseSchema()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.True(db.Database.CanConnect(), "Cannot connect to test database");

        var tableNames = new[] { "tenants", "domains", "categories", "products", "ai_prompts", "ai_runs" };
        foreach (var table in tableNames)
        {
            var sql = $"SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = '{table}')";
            var exists = db.Database.SqlQueryRaw<bool>(sql).AsEnumerable().First();
            Assert.True(exists, $"Required table '{table}' does not exist in database.");
        }
    }

    [Fact]
    public async Task GenerateProductDescription_ReturnsOk()
    {
        // Arrange: Create a product first
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenant = await db.Tenants.FirstAsync();
        var category = await db.Categories.FirstAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            CategoryId = category.Id,
            Title = "Test Product",
            Slug = "test-product",
            Description = "Old description",
            Price = 10.0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        // Act: Call generate description
        var response = await _client.PostAsync($"/api/admin/products/{product.Id}/generate-description", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
        Assert.NotNull(result.description);
    }
}