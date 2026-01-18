using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Admin.Pages.Onboarding
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class Step2_ThemeModel : PageModel
    {
        [BindProperty]
        public string Theme { get; set; } = "default";

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            TempData["Theme"] = Theme;
            return RedirectToPage("Step3-LocaleCurrency");
        }
    }
}