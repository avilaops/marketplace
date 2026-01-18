using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace MarketplaceBuilder.Admin.Pages.Admin.Orders
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DetailsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public Guid OrderId { get; set; }

        public OrderResponse? Order { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var tenantId = GetTenantId();
            if (!tenantId.HasValue)
            {
                return RedirectToPage("Index");
            }

            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"/api/admin/orders/{OrderId}?tenantId={tenantId.Value}");

            if (response.IsSuccessStatusCode)
            {
                Order = await response.Content.ReadFromJsonAsync<OrderResponse>();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            return Page();
        }

        private Guid? GetTenantId()
        {
            // For now, assume single tenant or get from session/context
            // In a real multi-tenant setup, this would come from user context
            return Guid.Parse("00000000-0000-0000-0000-000000000001"); // Placeholder
        }
    }
}