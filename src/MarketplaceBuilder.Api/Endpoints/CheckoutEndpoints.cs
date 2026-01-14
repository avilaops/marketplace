using MarketplaceBuilder.Api.Models;
using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Core.Interfaces;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceBuilder.Api.Endpoints;

public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/storefront/checkout")
            .WithTags("Checkout");

        group.MapPost("/session", CreateCheckoutSession);
    }

    private static async Task<IResult> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionRequest request,
        ApplicationDbContext context,
        IStripeGateway stripeGateway,
        IConfiguration configuration,
        HttpContext httpContext)
    {
        // Resolve tenant from Host header
        var hostname = httpContext.Request.Host.Host;
        var domain = await context.Domains
            .Include(d => d.Tenant)
            .FirstOrDefaultAsync(d => d.Hostname == hostname);

        if (domain == null)
            return Results.Problem("Store not found", statusCode: 404);

        var tenantId = domain.TenantId;

        // Validate storefront is Live
        var storefront = await context.StorefrontConfigs
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (storefront == null || storefront.Status != StorefrontStatus.Live)
            return Results.Problem("Store not published", statusCode: 400);

        // Validate items
        if (request.Items == null || !request.Items.Any())
            return Results.Problem("Cart is empty", statusCode: 400);

        var variantIds = request.Items.Select(i => i.VariantId).ToList();
        var variants = await context.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id) && v.TenantId == tenantId)
            .ToListAsync();

        if (variants.Count != variantIds.Count)
            return Results.Problem("Some products not found", statusCode: 400);

        // Check all products are Active
        if (variants.Any(v => v.Product.Status != ProductStatus.Active))
            return Results.Problem("Some products are not available", statusCode: 400);

        // Calculate totals server-side
        var orderItems = new List<OrderItem>();
        long subtotal = 0;
        var currency = storefront.Currency;

        foreach (var item in request.Items)
        {
            var variant = variants.First(v => v.Id == item.VariantId);
            
            if (variant.Currency != currency)
                return Results.Problem("Currency mismatch", statusCode: 400);

            var lineTotal = variant.PriceAmount * item.Quantity;
            subtotal += lineTotal;

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductId = variant.ProductId,
                VariantId = variant.Id,
                TitleSnapshot = variant.Product.Title,
                SkuSnapshot = variant.Sku,
                UnitPriceAmount = variant.PriceAmount,
                Quantity = item.Quantity,
                Currency = currency,
                LineTotalAmount = lineTotal,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Create Order
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = OrderStatus.Pending,
            Currency = currency,
            SubtotalAmount = subtotal,
            TotalAmount = subtotal,
            CustomerEmail = request.CustomerEmail,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = orderItems
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Create Stripe Checkout Session
        var apiPort = configuration.GetValue<int>("Platform:ApiPort", 5001);
        var storefrontPort = configuration.GetValue<int>("Platform:StorefrontPort", 5003);
        var successPath = configuration["Platform:CheckoutSuccessPath"] ?? "/checkout/success";
        var cancelPath = configuration["Platform:CheckoutCancelPath"] ?? "/cart";

        var successUrl = $"http://{hostname}:{storefrontPort}{successPath}?orderId={order.Id}";
        var cancelUrl = $"http://{hostname}:{storefrontPort}{cancelPath}";

        var lineItems = orderItems.Select(item => new CheckoutLineItem(
            item.TitleSnapshot,
            item.UnitPriceAmount,
            item.Currency,
            item.Quantity
        )).ToList();

        var metadata = new Dictionary<string, string>
        {
            { "tenant_id", tenantId.ToString() },
            { "order_id", order.Id.ToString() },
            { "store_name", storefront.StoreName }
        };

        var session = await stripeGateway.CreateCheckoutSessionAsync(
            lineItems,
            successUrl,
            cancelUrl,
            order.Id.ToString(),
            metadata,
            httpContext.RequestAborted
        );

        // Update order with Stripe session ID
        order.StripeCheckoutSessionId = session.SessionId;
        order.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Results.Ok(new CreateCheckoutSessionResponse(order.Id, session.CheckoutUrl));
    }

    public static void MapOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/storefront/orders")
            .WithTags("Orders");

        group.MapGet("/{orderId:guid}", GetOrder);
    }

    private static async Task<IResult> GetOrder(
        Guid orderId,
        ApplicationDbContext context,
        HttpContext httpContext)
    {
        // Resolve tenant from Host header
        var hostname = httpContext.Request.Host.Host;
        var domain = await context.Domains
            .FirstOrDefaultAsync(d => d.Hostname == hostname);

        if (domain == null)
            return Results.Problem("Store not found", statusCode: 404);

        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == domain.TenantId);

        if (order == null)
            return Results.NotFound();

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
