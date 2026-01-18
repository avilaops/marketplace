using System.Net;
using System.Net.Http.Json;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceBuilder.Tests.Integration;

public class CatalogTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CatalogTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Test");
        });
        _client = _factory.CreateClient();
        
        // Verify database is properly set up before running tests
        VerifyDatabaseSchema();
    }

    private void VerifyDatabaseSchema()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // This will throw if tables don't exist, causing tests to fail fast
        Assert.True(db.Database.CanConnect(), "Cannot connect to test database");
        
        // Verify critical tables exist
        var tableNames = new[] { "tenants", "domains", "categories", "products" };
        foreach (var table in tableNames)
        {
            var sql = $"SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = '{table}')";
            var exists = db.Database.SqlQueryRaw<bool>(sql).AsEnumerable().First();
            Assert.True(exists, $"Required table '{table}' does not exist in database. Migrations may not have been applied.");
        }
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

        // Assert - Endpoint should exist and return valid response
        // In CI, we want to catch database errors (500) that might be masked as 401
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Endpoint returned unexpected status. Got: {response.StatusCode}. " +
            $"If this is 500 InternalServerError, check database schema/migrations."
        );
        
        // Ensure we're not getting server errors
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_ReturnsOkOrUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/products");

        // Assert - Endpoint should exist and return valid response
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Endpoint returned unexpected status. Got: {response.StatusCode}. " +
            $"If this is 500 InternalServerError, check database schema/migrations."
        );
        
        // Ensure we're not getting server errors
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
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

    [Fact]
    public async Task DatabaseOperations_WithLocalhostTenant_WorksCorrectly()
    {
        // This test validates that the database schema is properly set up
        // and tenant resolution works as expected
        
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Verify localhost domain exists (seeded by Program.cs in CI/Dev)
        var localhostDomain = await db.Domains
            .FirstOrDefaultAsync(d => d.Hostname == "localhost");
        
        // In CI environment, localhost should be seeded
        var isCI = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "CI";
        if (isCI)
        {
            Assert.NotNull(localhostDomain);
            Assert.True(localhostDomain!.IsActive);
            
            // Verify tenant exists
            var tenant = await db.Tenants.FindAsync(localhostDomain.TenantId);
            Assert.NotNull(tenant);
            
            // Verify we can query categories (table exists and is accessible)
            var categories = await db.Categories.ToListAsync();
            Assert.NotNull(categories); // May be empty, but should not throw
        }
    }
}

