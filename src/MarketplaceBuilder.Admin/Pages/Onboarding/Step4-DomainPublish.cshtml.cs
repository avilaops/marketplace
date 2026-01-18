using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Admin.Pages.Onboarding
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class Step4_DomainPublishModel : PageModel
    {
        [BindProperty]
        public string Subdomain { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // TODO: Publish logic
            return RedirectToPage("Complete");
        }
    }
}