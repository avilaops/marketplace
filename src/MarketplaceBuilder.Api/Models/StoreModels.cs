namespace MarketplaceBuilder.Api.Models;

public record CreateStoreRequest(
    string StoreName,
    string Subdomain,
    string Currency = "USD",
    string Locale = "en-US",
    string Theme = "default"
);

public record UpdateStoreConfigRequest(
    string? StoreName,
    string? Currency,
    string? Locale,
    string? Theme
);

public record StoreResponse(
    Guid TenantId,
    string Hostname,
    string Status
);

public record PublishStoreResponse(
    Guid TenantId,
    string Hostname,
    string Status,
    DateTime PublishedAt
);
