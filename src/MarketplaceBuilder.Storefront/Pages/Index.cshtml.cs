using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MarketplaceBuilder.Storefront.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        // Redirect to products page
        return RedirectToPage("/Products/Index");
    }
}

