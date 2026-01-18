using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Admin.Pages.Onboarding;

public class Step4_DomainPublishModel : PageModel
{
    [BindProperty]
    public string Subdomain { get; set; }

    public string PreviewUrl => $"https://{Subdomain}.localtest.me";

    public IActionResult OnPost()
    {
        var tenantId = TempData["TenantId"]?.ToString();
        if (string.IsNullOrEmpty(tenantId)) return RedirectToPage("/Onboarding/Step1-Identity");

        // Create domain
        using var client = new HttpClient();
        var domainResponse = client.PostAsJsonAsync($"http://localhost:5000/api/admin/stores/{tenantId}/domain", new { Subdomain }).Result;
        if (!domainResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Failed to create domain");
            return Page();
        }

        // Publish
        var publishResponse = client.PostAsync($"http://localhost:5000/api/admin/stores/{tenantId}/publish", null).Result;
        if (!publishResponse.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Failed to publish store");
            return Page();
        }

        TempData["TenantId"] = tenantId;
        TempData["Subdomain"] = Subdomain;
        return RedirectToPage("/Onboarding/Complete");
    }
}