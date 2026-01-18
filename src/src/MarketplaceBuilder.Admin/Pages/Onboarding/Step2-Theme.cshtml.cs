using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Admin.Pages.Onboarding;

public class Step2_ThemeModel : PageModel
{
    [BindProperty]
    public string Theme { get; set; } = "default";

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        var tenantId = TempData["TenantId"]?.ToString();
        if (string.IsNullOrEmpty(tenantId)) return RedirectToPage("/Onboarding/Step1-Identity");

        // Call API
        using var client = new HttpClient();
        var response = client.PutAsJsonAsync($"http://localhost:5000/api/admin/stores/{tenantId}/config", new { Theme, Currency = "USD", Locale = "en-US" }).Result;
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Failed to update config");
            return Page();
        }

        TempData["TenantId"] = tenantId;
        TempData["Theme"] = Theme;
        return RedirectToPage("/Onboarding/Step3-LocaleCurrency");
    }
}