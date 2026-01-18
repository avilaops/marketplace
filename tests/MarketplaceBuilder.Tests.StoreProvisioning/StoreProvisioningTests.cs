using System.Net;
using System.Net.Http.Json;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceBuilder.Tests.Integration;

public class StoreProvisioningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StoreProvisioningTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Test");
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateStore_CreatesTenantAndDraftConfig_WhenValidRequest()
    {
        // Arrange
        var request = new { StoreName = "Test Store" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/stores", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<StoreResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.TenantId);
        Assert.Equal("Draft", result.Status);

        // Verify in database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenant = await context.Tenants.FindAsync(result.TenantId);
        Assert.NotNull(tenant);
        Assert.Equal("Test Store", tenant.Name);
        Assert.Equal("test-store", tenant.Slug);
        Assert.True(tenant.IsActive);

        var config = await context.StorefrontConfigs.FirstOrDefaultAsync(c => c.TenantId == result.TenantId);
        Assert.NotNull(config);
        Assert.Equal("Test Store", config.StoreName);
        Assert.Equal(StorefrontStatus.Draft, config.Status);

        // Verify audit logs
        var auditLogs = await context.AuditLogs.Where(a => a.TenantId == result.TenantId).ToListAsync();
        Assert.Equal(2, auditLogs.Count); // One for tenant, one for config
        Assert.Contains(auditLogs, a => a.Entity == "Tenant" && a.Action == "Create");
        Assert.Contains(auditLogs, a => a.Entity == "StorefrontConfig" && a.Action == "Create");
    }

    [Fact]
    public async Task CreateStore_ReturnsBadRequest_WhenStoreNameIsEmpty()
    {
        // Arrange
        var request = new { StoreName = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/stores", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private class StoreResponse
    {
        public Guid TenantId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}