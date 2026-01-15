using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MarketplaceBuilder.Storefront.Pages;

public class CartModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CartModel> _logger;

    public CartModel(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<CartModel> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public List<CartItemDisplay> CartItems { get; set; } = new();
    public long TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? StoreName { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var hostname = Request.Host.Host;
        var domain = await _context.Domains
            .Include(d => d.Tenant)
                .ThenInclude(t => t.StorefrontConfig)
            .FirstOrDefaultAsync(d => d.Hostname == hostname);

        if (domain == null)
            return NotFound("Store not found");

        StoreName = domain.Tenant.StorefrontConfig?.StoreName;
        Currency = domain.Tenant.StorefrontConfig?.Currency ?? "USD";

        // Read cart from cookie
        var cartJson = Request.Cookies["cart"];
        if (string.IsNullOrEmpty(cartJson))
            return Page();

        try
        {
            var cart = JsonSerializer.Deserialize<CartCookie>(cartJson);
            if (cart?.Items == null || !cart.Items.Any())
                return Page();

            var variantIds = cart.Items.Select(i => i.VariantId).ToList();
            var variants = await _context.ProductVariants
                .Include(v => v.Product)
                .Where(v => variantIds.Contains(v.Id) && v.TenantId == domain.TenantId && v.Product.Status == ProductStatus.Active)
                .ToListAsync();

            foreach (var cartItem in cart.Items)
            {
                var variant = variants.FirstOrDefault(v => v.Id == cartItem.VariantId);
                if (variant != null)
                {
                    var lineTotal = variant.PriceAmount * cartItem.Quantity;
                    TotalAmount += lineTotal;

                    CartItems.Add(new CartItemDisplay
                    {
                        VariantId = variant.Id,
                        ProductTitle = variant.Product.Title,
                        VariantName = variant.Name,
                        UnitPrice = variant.PriceAmount,
                        Quantity = cartItem.Quantity,
                        LineTotal = lineTotal,
                        Currency = variant.Currency,
                        PrimaryImageUrl = variant.Product.PrimaryImageUrl
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing cart cookie");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        var hostname = Request.Host.Host;
        var domain = await _context.Domains.FirstOrDefaultAsync(d => d.Hostname == hostname);
        
        if (domain == null)
            return NotFound();

        // Read cart from cookie
        var cartJson = Request.Cookies["cart"];
        if (string.IsNullOrEmpty(cartJson))
            return RedirectToPage("/Cart");

        var cart = JsonSerializer.Deserialize<CartCookie>(cartJson);
        if (cart?.Items == null || !cart.Items.Any())
            return RedirectToPage("/Cart");

        // Call API to create checkout session
        var apiPort = _configuration.GetValue<int>("Platform:ApiPort", 5001);
        var apiUrl = $"https://{hostname}:{apiPort}/api/storefront/checkout/session";

        using var httpClient = new HttpClient();
        var request = new
        {
            items = cart.Items.Select(i => new { variantId = i.VariantId, quantity = i.Quantity }).ToList(),
            customerEmail = (string?)null
        };

        var response = await httpClient.PostAsJsonAsync(apiUrl, request);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to create checkout session. Status: {StatusCode}", response.StatusCode);
            return RedirectToPage("/Cart");
        }

        var result = await response.Content.ReadFromJsonAsync<CheckoutSessionResponse>();
        if (result == null || string.IsNullOrEmpty(result.CheckoutUrl))
        {
            _logger.LogError("Invalid checkout session response");
            return RedirectToPage("/Cart");
        }

        // Redirect to Stripe Checkout
        return Redirect(result.CheckoutUrl);
    }
}

public class CartCookie
{
    public List<CartCookieItem> Items { get; set; } = new();
}

public class CartCookieItem
{
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }
}

public class CartItemDisplay
{
    public Guid VariantId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long LineTotal { get; set; }
    public string Currency { get; set; } = "USD";
    public string? PrimaryImageUrl { get; set; }

    public string FormattedUnitPrice => $"{(UnitPrice / 100.0m):N2} {Currency}";
    public string FormattedLineTotal => $"{(LineTotal / 100.0m):N2} {Currency}";
}

public record CheckoutSessionResponse(Guid OrderId, string CheckoutUrl);
