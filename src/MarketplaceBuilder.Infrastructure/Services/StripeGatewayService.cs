using MarketplaceBuilder.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace MarketplaceBuilder.Infrastructure.Services;

public class StripeGatewayService : IStripeGateway
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeGatewayService> _logger;

    public StripeGatewayService(
        IConfiguration configuration,
        ILogger<StripeGatewayService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Configure Stripe API key
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        List<CheckoutLineItem> lineItems,
        string successUrl,
        string cancelUrl,
        string clientReferenceId,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = clientReferenceId,
                Metadata = metadata,
                LineItems = lineItems.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = item.Currency.ToLowerInvariant(),
                        UnitAmount = item.UnitAmountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Name
                        }
                    },
                    Quantity = item.Quantity
                }).ToList()
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Stripe Checkout Session created. SessionId: {SessionId}, ClientReferenceId: {ClientReferenceId}",
                session.Id, clientReferenceId);

            return new CheckoutSessionResult(session.Id, session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session. ClientReferenceId: {ClientReferenceId}",
                clientReferenceId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session. ClientReferenceId: {ClientReferenceId}",
                clientReferenceId);
            throw;
        }
    }
}
