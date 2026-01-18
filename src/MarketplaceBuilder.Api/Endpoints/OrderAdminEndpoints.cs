using MarketplaceBuilder.Api.Helpers;
using MarketplaceBuilder.Api.Models;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceBuilder.Api.Endpoints;

public static class OrderAdminEndpoints
{
    public static void MapOrderAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin/orders")
            .WithTags("Order Admin");

        group.MapGet("/", GetOrders)
            .WithName("GetOrders")
            .Produces<PagedResponse<OrderResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{orderId:guid}", GetOrder)
            .WithName("GetOrder")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> GetOrders(
        ApplicationDbContext context,
        [FromQuery] string? tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
        {
            return Results.Problem("Invalid tenantId", statusCode: 400);
        }

        IQueryable<Order> query = context.Orders
            .Where(o => o.TenantId == tenantGuid)
            .Include(o => o.Items);

        var totalCount = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var responses = orders.Select(o => new OrderResponse(
            o.Id,
            o.Status.ToString(),
            o.Currency,
            o.SubtotalAmount,
            o.TotalAmount,
            o.CustomerEmail,
            o.Items.Select(i => new OrderItemResponse(
                i.TitleSnapshot,
                i.SkuSnapshot,
                i.UnitPriceAmount,
                i.Quantity,
                i.Currency,
                i.LineTotalAmount
            )).ToList(),
            o.CreatedAt
        )).ToList();

        var pagedResponse = new PagedResponse<OrderResponse>(
            responses,
            page,
            pageSize,
            totalCount
        );

        return Results.Ok(pagedResponse);
    }

    internal static async Task<IResult> GetOrder(
        Guid tenantId,
        Guid orderId,
        ApplicationDbContext context)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId);

        if (order == null)
        {
            return Results.Problem($"Order not found", statusCode: 404);
        }

        var response = new OrderResponse(
            order.Id,
            order.Status.ToString(),
            order.Currency,
            order.SubtotalAmount,
            order.TotalAmount,
            order.CustomerEmail,
            order.Items.Select(i => new OrderItemResponse(
                i.TitleSnapshot,
                i.SkuSnapshot,
                i.UnitPriceAmount,
                i.Quantity,
                i.Currency,
                i.LineTotalAmount
            )).ToList(),
            order.CreatedAt
        );

        return Results.Ok(response);
    }
}