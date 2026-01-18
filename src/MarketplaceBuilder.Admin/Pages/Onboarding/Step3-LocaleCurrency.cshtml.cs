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

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            TempData["Currency"] = Currency;
            TempData["Locale"] = Locale;
            return RedirectToPage("Step4-DomainPublish");
        }
    }
}