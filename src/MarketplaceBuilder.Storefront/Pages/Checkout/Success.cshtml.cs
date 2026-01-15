using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Storefront.Pages.Checkout;

public class SuccessModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SuccessModel> _logger;

    public SuccessModel(IConfiguration configuration, ILogger<SuccessModel> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public OrderStatusDisplay? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? orderId)
    {
        if (!orderId.HasValue)
            return RedirectToPage("/Index");

        var hostname = Request.Host.Host;
        var apiPort = _configuration.GetValue<int>("Platform:ApiPort", 5001);
        var apiUrl = $"https://{hostname}:{apiPort}/api/storefront/orders/{orderId}";

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch order {OrderId}. Status: {StatusCode}", orderId, response.StatusCode);
                return Page();
            }

            Order = await response.Content.ReadFromJsonAsync<OrderStatusDisplay>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order {OrderId}", orderId);
        }

        return Page();
    }
}

public record OrderStatusDisplay(
    Guid Id,
    string Status,
    string Currency,
    long SubtotalAmount,
    long TotalAmount,
    string? CustomerEmail,
    List<OrderItemDisplay> Items,
    DateTime CreatedAt
);

public record OrderItemDisplay(
    string TitleSnapshot,
    string? SkuSnapshot,
    long UnitPriceAmount,
    int Quantity,
    string Currency,
    long LineTotalAmount
);
