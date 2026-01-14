using MarketplaceBuilder.Core.Entities;
using MarketplaceBuilder.Core.Interfaces;
using MarketplaceBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceBuilder.Storefront.Pages.Products;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(
        ApplicationDbContext context,
        ITenantResolver tenantResolver,
        ILogger<DetailsModel> logger)
    {
        _context = context;
        _tenantResolver = tenantResolver;
        _logger = logger;
    }

    public ProductDetail? Product { get; set; }
    public string? StoreName { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
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
            _logger.LogInformation("Store not published. Tenant: {TenantId}", tenantId.Value);
            return NotFound("Store not published");
        }

        StoreName = storefrontConfig.StoreName;

        // Get product by slug (only Active)
        var product = await _context.Products
            .Where(p => p.TenantId == tenantId.Value && p.Slug == slug && p.Status == ProductStatus.Active)
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync();

        if (product == null)
        {
            _logger.LogInformation("Product not found. Slug: {Slug}, Tenant: {TenantId}", slug, tenantId.Value);
            return NotFound("Product not found");
        }

        Product = new ProductDetail
        {
            Id = product.Id,
            Title = product.Title,
            Slug = product.Slug,
            Description = product.Description,
            CategoryName = product.Category?.Name,
            PrimaryImageUrl = product.PrimaryImageUrl,
            Images = product.Images.Select(i => new ProductImageInfo
            {
                PublicUrl = i.PublicUrl,
                SortOrder = i.SortOrder
            }).ToList(),
            Variants = product.Variants.Select(v => new ProductVariantInfo
            {
                Id = v.Id,
                Name = v.Name,
                Sku = v.Sku,
                PriceAmount = v.PriceAmount,
                Currency = v.Currency,
                StockQty = v.StockQty,
                IsDefault = v.IsDefault
            }).ToList()
        };

        return Page();
    }
}

public class ProductDetail
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public List<ProductImageInfo> Images { get; set; } = new();
    public List<ProductVariantInfo> Variants { get; set; } = new();

    public ProductVariantInfo? DefaultVariant => Variants.FirstOrDefault(v => v.IsDefault);
}

public class ProductImageInfo
{
    public string PublicUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class ProductVariantInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public long PriceAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public int StockQty { get; set; }
    public bool IsDefault { get; set; }

    public string FormattedPrice => $"{(PriceAmount / 100.0m):N2} {Currency}";
    public bool InStock => StockQty > 0;
}
