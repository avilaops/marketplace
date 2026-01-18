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
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MarketplaceBuilder.Tests.Integration;

public class OrderTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrderTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOrders_ShouldReturnOrders_ForTenant()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create a tenant and some orders in the database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Tenants.Add(tenant);

        var order1 = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = OrderStatus.Paid,
            Currency = "USD",
            SubtotalAmount = 10000,
            TotalAmount = 10000,
            CustomerEmail = "customer1@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Orders.Add(order1);

        var order2 = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = OrderStatus.Pending,
            Currency = "USD",
            SubtotalAmount = 20000,
            TotalAmount = 20000,
            CustomerEmail = "customer2@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Orders.Add(order2);

        await context.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/api/admin/orders?tenantId={tenantId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetOrders_ShouldNotReturnOrders_FromOtherTenant()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenantId1 = Guid.NewGuid();
        var tenant1 = new Tenant
        {
            Id = tenantId1,
            Name = "Test Tenant 1",
            Slug = "test-tenant-1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Tenants.Add(tenant1);

        var tenantId2 = Guid.NewGuid();
        var tenant2 = new Tenant
        {
            Id = tenantId2,
            Name = "Test Tenant 2",
            Slug = "test-tenant-2",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Tenants.Add(tenant2);

        var order1 = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId1,
            Status = OrderStatus.Paid,
            Currency = "USD",
            SubtotalAmount = 10000,
            TotalAmount = 10000,
            CustomerEmail = "customer1@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Orders.Add(order1);

        var order2 = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId2,
            Status = OrderStatus.Paid,
            Currency = "USD",
            SubtotalAmount = 20000,
            TotalAmount = 20000,
            CustomerEmail = "customer2@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Orders.Add(order2);

        await context.SaveChangesAsync();

        // Act - request orders for tenant1
        var response = await client.GetAsync($"/api/admin/orders?tenantId={tenantId1}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
        Assert.NotNull(result);
        Assert.Single(result.Items); // Only order1
        Assert.Equal(order1.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnOrderDetails_WithItems()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Tenants.Add(tenant);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = OrderStatus.Paid,
            Currency = "USD",
            SubtotalAmount = 10000,
            TotalAmount = 10000,
            CustomerEmail = "customer@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Orders.Add(order);

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            VariantId = Guid.NewGuid(),
            TitleSnapshot = "Test Product",
            SkuSnapshot = "TEST-001",
            UnitPriceAmount = 10000,
            Quantity = 1,
            Currency = "USD",
            LineTotalAmount = 10000,
            CreatedAt = DateTime.UtcNow
        };
        context.OrderItems.Add(orderItem);

        await context.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/api/admin/orders/{order.Id}?tenantId={tenantId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal("Paid", result.Status);
        Assert.Equal("customer@example.com", result.CustomerEmail);
        Assert.Single(result.Items);
        Assert.Equal("Test Product", result.Items[0].TitleSnapshot);
    }
}