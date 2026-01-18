using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Admin.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (!User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Account/Login");
        }

        return RedirectToPage("/Onboarding/Step1-Identity");
    }
}
