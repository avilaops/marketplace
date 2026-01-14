namespace MarketplaceBuilder.Core.Interfaces;

/// <summary>
/// Resultado da criação de checkout session
/// </summary>
public record CheckoutSessionResult(
    string SessionId,
    string CheckoutUrl
);

/// <summary>
/// Item para checkout
/// </summary>
public record CheckoutLineItem(
    string Name,
    long UnitAmountCents,
    string Currency,
    int Quantity
);

/// <summary>
/// Gateway para integração com Stripe
/// </summary>
public interface IStripeGateway
{
    /// <summary>
    /// Cria uma Checkout Session no Stripe
    /// </summary>
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        List<CheckoutLineItem> lineItems,
        string successUrl,
        string cancelUrl,
        string clientReferenceId,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default);
}
