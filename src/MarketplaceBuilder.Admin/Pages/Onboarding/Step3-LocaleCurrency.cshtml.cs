using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Admin.Pages.Onboarding
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class Step3_LocaleCurrencyModel : PageModel
    {
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

        public IActionResult OnPost()
        {
            var tenantId = HttpContext.Session.GetString("OnboardingTenantId");
            if (string.IsNullOrEmpty(tenantId))
            {
                return RedirectToPage("Step1-Identity");
            }

            // TODO: Call API to update config
            TempData["Currency"] = Currency;
            TempData["Locale"] = Locale;
            return RedirectToPage("Step4-DomainPublish");
        }
    }
}