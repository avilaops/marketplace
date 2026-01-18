using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;

namespace MarketplaceBuilder.Admin.Pages.Onboarding
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class Step2_ThemeModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public Step2_ThemeModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public string Theme { get; set; } = "default";

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
            var request = new { Theme };
            var response = await client.PutAsJsonAsync($"/api/admin/stores/{tenantId}/config", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Erro ao atualizar tema: {error}";
                return Page();
            }

            TempData["Message"] = "Tema atualizado com sucesso!";
            return RedirectToPage("Step3-LocaleCurrency");
        }
    }
}