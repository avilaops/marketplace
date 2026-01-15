namespace MarketplaceBuilder.Api.Models;

// Checkout DTOs
public record CreateCheckoutSessionRequest(
    List<CheckoutItemRequest> Items,
    string? CustomerEmail
);

public record CheckoutItemRequest(
    Guid VariantId,
    int Quantity
);

public record CreateCheckoutSessionResponse(
    Guid OrderId,
    string CheckoutUrl
);

// Order DTOs
public record OrderResponse(
    Guid Id,
    string Status,
    string Currency,
    long SubtotalAmount,
    long TotalAmount,
    string? CustomerEmail,
    List<OrderItemResponse> Items,
    DateTime CreatedAt
);

public record OrderItemResponse(
    string TitleSnapshot,
    string? SkuSnapshot,
    long UnitPriceAmount,
    int Quantity,
    string Currency,
    long LineTotalAmount
);
