using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace MarketplaceBuilder.Admin.Pages.Onboarding
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class Step3_LocaleCurrencyModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public Step3_LocaleCurrencyModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public string Currency { get; set; } = "EUR";

        [BindProperty]
        public string Locale { get; set; } = "pt-PT";

        public IActionResult OnGet()
        {
            var tenantId = HttpContext.Session.GetString("OnboardingTenantId");
            if (string.IsNullOrEmpty(tenantId))
            {
                return RedirectToPage("Step1-Identity");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var tenantId = HttpContext.Session.GetString("OnboardingTenantId");
            if (string.IsNullOrEmpty(tenantId))
            {
                return RedirectToPage("Step1-Identity");
            }

            // Call API to update config
            var client = _httpClientFactory.CreateClient("ApiClient");
            var request = new { Currency, Locale };
            var response = await client.PutAsJsonAsync($"/api/admin/stores/{tenantId}/config", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Erro ao atualizar moeda e locale: {error}";
                return Page();
            }

            TempData["Message"] = "Moeda e locale atualizados com sucesso!";
            return RedirectToPage("Step4-DomainPublish");
        }
    }
}