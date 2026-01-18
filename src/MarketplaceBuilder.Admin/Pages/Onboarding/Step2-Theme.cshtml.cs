using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Admin.Pages.Onboarding
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class Step2_ThemeModel : PageModel
    {
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

        public IActionResult OnPost()
        {
            var tenantId = HttpContext.Session.GetString("OnboardingTenantId");
            if (string.IsNullOrEmpty(tenantId))
            {
                return RedirectToPage("Step1-Identity");
            }

            // TODO: Call API to update config
            TempData["Theme"] = Theme;
            return RedirectToPage("Step3-LocaleCurrency");
        }
    }
}