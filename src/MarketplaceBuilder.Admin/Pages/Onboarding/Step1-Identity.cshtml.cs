using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace MarketplaceBuilder.Admin.Pages.Onboarding
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class Step1_IdentityModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public Step1_IdentityModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public string StoreName { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Chamar API para criar tenant
            var client = _httpClientFactory.CreateClient("ApiClient");
            var request = new { StoreName };
            var response = await client.PostAsJsonAsync("/api/admin/stores", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Erro ao criar loja: {error}";
                return Page();
            }

            var result = await response.Content.ReadFromJsonAsync<StoreResponse>();
            if (result == null)
            {
                TempData["Error"] = "Erro ao processar resposta da API";
                return Page();
            }

            // Guardar tenantId na sessão
            HttpContext.Session.SetString("OnboardingTenantId", result.TenantId.ToString());

            TempData["Message"] = "Loja criada com sucesso!";
            return RedirectToPage("Step2-Theme");
        }

        private class StoreResponse
        {
            public Guid TenantId { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}