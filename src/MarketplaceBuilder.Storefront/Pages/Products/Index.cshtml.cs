using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Core.Interfaces;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceBuilder.Storefront.Pages.Products;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        ApplicationDbContext context,
        ITenantResolver tenantResolver,
        ILogger<IndexModel> logger)
    {
        _context = context;
        _tenantResolver = tenantResolver;
        _logger = logger;
    }

    public List<ProductListItem> Products { get; set; } = new();
    public string? StoreName { get; set; }
    public bool StoreNotLive { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Resolve tenant from Host header
        var hostname = Request.Host.Host;
        var tenantId = await _tenantResolver.ResolveTenantAsync(hostname);

        if (!tenantId.HasValue)
        {
            _logger.LogWarning("Tenant not found for hostname: {Hostname}", hostname);
            return NotFound("Store not found");
        }

        // Check if store is Live
        var storefrontConfig = await _context.StorefrontConfigs
            .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value);

        if (storefrontConfig == null || storefrontConfig.Status != StorefrontStatus.Live)
        {
            _logger.LogInformation("Store not published. Tenant: {TenantId}, Status: {Status}", 
                tenantId.Value, storefrontConfig?.Status);
            StoreNotLive = true;
            return Page();
        }

        StoreName = storefrontConfig.StoreName;

        // Get active products with default variant
        var products = await _context.Products
            .Where(p => p.TenantId == tenantId.Value && p.Status == ProductStatus.Active)
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsDefault))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        Products = products.Select(p => new ProductListItem
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            PrimaryImageUrl = p.PrimaryImageUrl,
            CategoryName = p.Category?.Name,
            PriceAmount = p.Variants.FirstOrDefault()?.PriceAmount ?? 0,
            Currency = p.Variants.FirstOrDefault()?.Currency ?? "USD"
        }).ToList();

        return Page();
    }
}

public class ProductListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? PrimaryImageUrl { get; set; }
    public string? CategoryName { get; set; }
    public long PriceAmount { get; set; }
    public string Currency { get; set; } = "USD";

    public string FormattedPrice => $"{(PriceAmount / 100.0m):N2} {Currency}";
}
