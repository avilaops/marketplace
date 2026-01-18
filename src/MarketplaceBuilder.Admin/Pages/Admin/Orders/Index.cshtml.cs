using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace MarketplaceBuilder.Admin.Pages.Admin.Orders
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? MinTotal { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? MaxTotal { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CustomerEmail { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? OrderId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public PagedResponse<OrderResponse>? Orders { get; set; }

        public async Task OnGetAsync()
        {
            var tenantId = GetTenantId();
            if (!tenantId.HasValue)
            {
                return;
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            var queryParams = new List<string>
            {
                $"tenantId={tenantId.Value}",
                $"page={CurrentPage}",
                $"pageSize={PageSize}"
            };

            if (!string.IsNullOrWhiteSpace(Status))
                queryParams.Add($"status={Status}");
            if (FromDate.HasValue)
                queryParams.Add($"fromDate={FromDate.Value:yyyy-MM-dd}");
            if (ToDate.HasValue)
                queryParams.Add($"toDate={ToDate.Value:yyyy-MM-dd}");
            if (MinTotal.HasValue)
                queryParams.Add($"minTotal={MinTotal.Value}");
            if (MaxTotal.HasValue)
                queryParams.Add($"maxTotal={MaxTotal.Value}");
            if (!string.IsNullOrWhiteSpace(CustomerEmail))
                queryParams.Add($"customerEmail={CustomerEmail}");
            if (OrderId.HasValue)
                queryParams.Add($"orderId={OrderId.Value}");

            var queryString = string.Join("&", queryParams);
            var response = await client.GetAsync($"/api/admin/orders?{queryString}");

            if (response.IsSuccessStatusCode)
            {
                Orders = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();
            }
        }

        private Guid? GetTenantId()
        {
            // For now, assume single tenant or get from session/context
            // In a real multi-tenant setup, this would come from user context
            return Guid.Parse("00000000-0000-0000-0000-000000000001"); // Placeholder
        }
    }

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

    public record PagedResponse<T>(
        List<T> Items,
        int Page,
        int PageSize,
        int TotalCount
    );
}