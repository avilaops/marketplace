using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Admin.Pages.Onboarding;

public class Step3_LocaleCurrencyModel : PageModel
{
    [BindProperty]
    public string Currency { get; set; } = "USD";

    [BindProperty]
    public string Locale { get; set; } = "en-US";

    public IActionResult OnPost()
    {
        var tenantId = TempData["TenantId"]?.ToString();
        if (string.IsNullOrEmpty(tenantId)) return RedirectToPage("/Onboarding/Step1-Identity");

        // Call API
        using var client = new HttpClient();
        var response = client.PutAsJsonAsync($"http://localhost:5000/api/admin/stores/{tenantId}/config", new { Theme = TempData["Theme"], Currency, Locale }).Result;
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Failed to update config");
            return Page();
        }

        TempData["TenantId"] = tenantId;
        TempData["Currency"] = Currency;
        TempData["Locale"] = Locale;
        return RedirectToPage("/Onboarding/Step4-DomainPublish");
    }
}