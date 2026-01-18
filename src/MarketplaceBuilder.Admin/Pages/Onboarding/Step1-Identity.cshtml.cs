using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Admin.Pages.Onboarding
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class Step1_IdentityModel : PageModel
    {
        [BindProperty]
        public string StoreName { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // TODO: Persist later
            TempData["StoreName"] = StoreName;

            return RedirectToPage("Step2-Theme");
        }
    }
}