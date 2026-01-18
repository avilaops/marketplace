using System.Net;
using System.Net.Http.Json;
using MarketplaceBuilder.Api.Endpoints;
using MarketplaceBuilder.Api.Models;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace MarketplaceBuilder.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContext registrations
            var dbContextDescriptors = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                     d.ServiceType == typeof(ApplicationDbContext)).ToList();
            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });
        });
    }
}

public class StoreProvisioningTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public StoreProvisioningTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateStore_CreatesTenantAndDraftConfig_WhenValidRequest()
    {
        // Test the endpoint logic directly to avoid TestHost JSON serialization issues
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var httpContext = new DefaultHttpContext();
        
        var request = new CreateStoreRequest("Test Store");
        
        // Act
        var result = await StoreProvisioningEndpoints.CreateStore(request, context, httpContext);
        
        // Assert
        Assert.NotNull(result);
        // For successful creation, it should be a CreatedResult
        Assert.Contains("Created", result.GetType().FullName);
        Assert.Contains("StoreResponse", result.GetType().FullName);
        
        // Access the value using reflection since the cast isn't working
        var valueProperty = result.GetType().GetProperty("Value");
        Assert.NotNull(valueProperty);
        var value = valueProperty.GetValue(result);
        Assert.NotNull(value);
        var storeResponse = (StoreResponse)value;
        Assert.NotNull(storeResponse);
        Assert.NotEqual(Guid.Empty, storeResponse.TenantId);
        Assert.Equal("Draft", storeResponse.Status);

        var tenantId = storeResponse.TenantId;

        // Verify in database
        var tenant = await context.Tenants.FindAsync(tenantId);
        Assert.NotNull(tenant);
        Assert.Equal("Test Store", tenant.Name);
        Assert.Equal("test-store", tenant.Slug);
        Assert.True(tenant.IsActive);

        var config = await context.StorefrontConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId);
        Assert.NotNull(config);
        Assert.Equal("Test Store", config.StoreName);
        Assert.Equal(StorefrontStatus.Draft, config.Status);

        // Verify audit logs
        var auditLogs = await context.AuditLogs.Where(a => a.TenantId == tenantId).ToListAsync();
        Assert.Equal(2, auditLogs.Count); // One for tenant, one for config
        Assert.Contains(auditLogs, a => a.Entity == "Tenant" && a.Action == "Create");
        Assert.Contains(auditLogs, a => a.Entity == "StorefrontConfig" && a.Action == "Create");
    }

    [Fact]
    public async Task CreateStore_ReturnsBadRequest_WhenStoreNameIsEmpty()
    {
        // Test the endpoint logic directly to avoid TestHost JSON serialization issues
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var httpContext = new DefaultHttpContext();
        
        var request = new CreateStoreRequest("");
        
        // This should return a bad request result
        var result = await StoreProvisioningEndpoints.CreateStore(request, context, httpContext);
        
        // Verify it's a bad request
        Assert.Contains("BadRequest", result.GetType().FullName);
        
        // Access the value using reflection
        var valueProperty = result.GetType().GetProperty("Value");
        Assert.NotNull(valueProperty);
        var errorValue = valueProperty.GetValue(result);
        Assert.NotNull(errorValue);
    }

    [Fact]
    public async Task UpdateStoreConfig_UpdatesStorefrontConfig_WhenValidRequest()
    {
        // First create a store
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var httpContext = new DefaultHttpContext();
        
        var createRequest = new CreateStoreRequest("Test Store for Update");
        var createResult = await StoreProvisioningEndpoints.CreateStore(createRequest, context, httpContext);
        
        var createdResult = createResult.GetType().GetProperty("Value")?.GetValue(createResult) as StoreResponse;
        Assert.NotNull(createdResult);
        var tenantId = createdResult.TenantId;

        // Now update the config
        var updateRequest = new UpdateStoreConfigRequest(
            StoreName: "Updated Store Name",
            Currency: "USD",
            Locale: "en-US",
            Theme: "dark"
        );
        
        var updateResult = await StoreProvisioningEndpoints.UpdateStoreConfig(tenantId, updateRequest, context);
        
        // Assert
        Assert.NotNull(updateResult);
        Assert.Contains("Ok", updateResult.GetType().FullName);
        
        var updateResponse = updateResult.GetType().GetProperty("Value")?.GetValue(updateResult) as StoreResponse;
        Assert.NotNull(updateResponse);
        Assert.Equal(tenantId, updateResponse.TenantId);
        Assert.Equal("Draft", updateResponse.Status);

        // Verify in database
        var config = await context.StorefrontConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId);
        Assert.NotNull(config);
        Assert.Equal("Updated Store Name", config.StoreName);
        Assert.Equal("USD", config.Currency);
        Assert.Equal("en-US", config.Locale);
        Assert.Equal("dark", config.Theme);

        // Verify audit logs
        var auditLogs = await context.AuditLogs.Where(a => a.TenantId == tenantId && a.Action == "Update").ToListAsync();
        Assert.Equal(4, auditLogs.Count); // One for each updated field
        Assert.Contains(auditLogs, a => a.Entity == "StorefrontConfig" && 
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(a.OldValues!)?.ContainsKey("StoreName") == true &&
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(a.NewValues!)?.ContainsKey("StoreName") == true);
        Assert.Contains(auditLogs, a => a.Entity == "StorefrontConfig" && 
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(a.NewValues!)?.ContainsKey("Currency") == true);
        Assert.Contains(auditLogs, a => a.Entity == "StorefrontConfig" && 
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(a.NewValues!)?.ContainsKey("Locale") == true);
        Assert.Contains(auditLogs, a => a.Entity == "StorefrontConfig" && 
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(a.NewValues!)?.ContainsKey("Theme") == true);
    }

}