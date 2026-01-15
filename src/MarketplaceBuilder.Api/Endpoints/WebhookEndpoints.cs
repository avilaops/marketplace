using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace MarketplaceBuilder.Api.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/webhooks/stripe", HandleStripeWebhook)
            .AllowAnonymous()
            .WithTags("Webhooks");
    }

    private static async Task<IResult> HandleStripeWebhook(
        HttpRequest request,
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<Program> logger)
    {
        var json = await new StreamReader(request.Body).ReadToEndAsync();
        var webhookSecret = configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(webhookSecret))
        {
            logger.LogWarning("Stripe webhook secret not configured");
            return Results.BadRequest("Webhook not configured");
        }

        try
        {
            // Validate signature
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                request.Headers["Stripe-Signature"],
                webhookSecret
            );

            logger.LogInformation("Stripe webhook received: {EventType}, EventId: {EventId}",
                stripeEvent.Type, stripeEvent.Id);

            // Check idempotency - has this event been processed already?
            var existingEvent = await context.StripeWebhookEvents
                .FirstOrDefaultAsync(e => e.StripeEventId == stripeEvent.Id);

            if (existingEvent != null)
            {
                logger.LogInformation("Event {EventId} already processed. Returning 200 (idempotent).", stripeEvent.Id);
                return Results.Ok(new { message = "Event already processed" });
            }

            // Create webhook event record
            var webhookEvent = new StripeWebhookEvent
            {
                Id = Guid.NewGuid(),
                StripeEventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                ReceivedAt = DateTime.UtcNow,
                ProcessingStatus = WebhookProcessingStatus.Received
            };

            context.StripeWebhookEvents.Add(webhookEvent);
            await context.SaveChangesAsync();

            // Process event based on type
            try
            {
                await ProcessStripeEvent(stripeEvent, context, webhookEvent, logger);
                
                webhookEvent.ProcessingStatus = WebhookProcessingStatus.Processed;
                webhookEvent.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Stripe event {EventId}", stripeEvent.Id);
                webhookEvent.ProcessingStatus = WebhookProcessingStatus.Failed;
                webhookEvent.Error = ex.Message;
            }

            await context.SaveChangesAsync();

            return Results.Ok(new { received = true });
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe signature validation failed");
            return Results.BadRequest("Invalid signature");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling webhook");
            return Results.Problem("Error processing webhook");
        }
    }

    private static async Task ProcessStripeEvent(
        Event stripeEvent,
        ApplicationDbContext context,
        StripeWebhookEvent webhookEvent,
        ILogger logger)
    {
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompleted(stripeEvent, context, webhookEvent, logger);
                break;

            case "payment_intent.payment_failed":
            case "checkout.session.async_payment_failed":
                await HandlePaymentFailed(stripeEvent, context, logger);
                break;

            case "charge.refunded":
                await HandleChargeRefunded(stripeEvent, context, logger);
                break;

            default:
                logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private static async Task HandleCheckoutSessionCompleted(
        Event stripeEvent,
        ApplicationDbContext context,
        StripeWebhookEvent webhookEvent,
        ILogger logger)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null)
        {
            logger.LogWarning("Session object is null in checkout.session.completed event");
            return;
        }

        // Get order ID from client_reference_id
        if (string.IsNullOrEmpty(session.ClientReferenceId) || !Guid.TryParse(session.ClientReferenceId, out var orderId))
        {
            logger.LogWarning("Invalid or missing ClientReferenceId in session {SessionId}", session.Id);
            return;
        }

        var order = await context.Orders.FindAsync(orderId);
        if (order == null)
        {
            logger.LogWarning("Order {OrderId} not found for session {SessionId}", orderId, session.Id);
            return;
        }

        // Extract tenant from metadata
        if (session.Metadata != null && session.Metadata.TryGetValue("tenant_id", out var tenantIdStr))
        {
            if (Guid.TryParse(tenantIdStr, out var tenantId))
            {
                webhookEvent.TenantId = tenantId;
            }
        }

        // Update order status
        order.Status = OrderStatus.Paid;
        order.StripeCheckoutSessionId = session.Id;
        order.StripePaymentIntentId = session.PaymentIntentId;
        order.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("Order {OrderId} marked as Paid. Session: {SessionId}, PaymentIntent: {PaymentIntentId}",
            orderId, session.Id, session.PaymentIntentId);
    }

    private static async Task HandlePaymentFailed(
        Event stripeEvent,
        ApplicationDbContext context,
        ILogger logger)
    {
        // Try to extract order info from metadata
        dynamic eventData = stripeEvent.Data.Object;
        
        if (eventData.Metadata != null)
        {
            string? orderIdStr = eventData.Metadata.TryGetValue("order_id", out object? orderIdObj) ? orderIdObj?.ToString() : null;
            
            if (orderIdStr != null && Guid.TryParse(orderIdStr, out var orderId))
            {
                var order = await context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    order.Status = OrderStatus.Failed;
                    order.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    logger.LogInformation("Order {OrderId} marked as Failed", orderId);
                }
            }
        }
    }

    private static async Task HandleChargeRefunded(
        Event stripeEvent,
        ApplicationDbContext context,
        ILogger logger)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null) return;

        // Find order by payment intent
        if (!string.IsNullOrEmpty(charge.PaymentIntentId))
        {
            var order = await context.Orders
                .FirstOrDefaultAsync(o => o.StripePaymentIntentId == charge.PaymentIntentId);

            if (order != null)
            {
                order.Status = OrderStatus.Refunded;
                order.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                logger.LogInformation("Order {OrderId} marked as Refunded", order.Id);
            }
        }
    }
}
