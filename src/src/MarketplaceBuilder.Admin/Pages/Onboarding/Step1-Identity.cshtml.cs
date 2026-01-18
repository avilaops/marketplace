using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Admin.Pages.Onboarding;

public class Step1_IdentityModel : PageModel
{
    [BindProperty]
    public string StoreName { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Call API
        using var client = new HttpClient();
        var response = await client.PostAsJsonAsync("http://localhost:5000/api/admin/stores", new { StoreName });
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Failed to create store");
            return Page();
        }

        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        TempData["TenantId"] = result.tenantId;
        TempData["StoreName"] = StoreName;

        return RedirectToPage("/Onboarding/Step2-Theme");
    }
}