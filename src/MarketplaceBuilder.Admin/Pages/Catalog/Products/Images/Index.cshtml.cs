using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace MarketplaceBuilder.Admin.Pages.Catalog.Products.Images
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public Guid ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public List<ProductImageViewModel> Images { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid productId)
        {
            ProductId = productId;

            // Get product info
            var client = _httpClientFactory.CreateClient("ApiClient");
            var productResponse = await client.GetAsync($"/api/admin/products/{productId}");

            if (!productResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Produto não encontrado";
                return RedirectToPage("/Catalog/Index");
            }

            var product = await productResponse.Content.ReadFromJsonAsync<ProductViewModel>();
            if (product != null)
            {
                ProductTitle = product.Title;
            }

            // Get images
            var imagesResponse = await client.GetAsync($"/api/admin/products/{productId}/images");
            if (imagesResponse.IsSuccessStatusCode)
            {
                var images = await imagesResponse.Content.ReadFromJsonAsync<List<ProductImageViewModel>>();
                if (images != null)
                {
                    Images = images.OrderBy(i => i.SortOrder).ToList();
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(Guid productId, List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                TempData["Error"] = "Selecione pelo menos uma imagem";
                return RedirectToPage(new { productId });
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.FileName);

                var response = await client.PostAsync($"/api/admin/products/{productId}/images", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Erro ao fazer upload da imagem {file.FileName}: {error}";
                    break;
                }
            }

            TempData["Success"] = "Imagens enviadas com sucesso";
            return RedirectToPage(new { productId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid productId, Guid imageId)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.DeleteAsync($"/api/admin/products/{productId}/images/{imageId}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Imagem removida com sucesso";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Erro ao remover imagem: {error}";
            }

            return RedirectToPage(new { productId });
        }

        public async Task<IActionResult> OnPostSetPrimaryAsync(Guid productId, Guid imageId)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            // First, get the image to get its public URL
            var imageResponse = await client.GetAsync($"/api/admin/products/{productId}/images/{imageId}");
            if (!imageResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Imagem não encontrada";
                return RedirectToPage(new { productId });
            }

            var image = await imageResponse.Content.ReadFromJsonAsync<ProductImageViewModel>();
            if (image == null)
            {
                TempData["Error"] = "Imagem não encontrada";
                return RedirectToPage(new { productId });
            }

            // Update product primary image
            var updateRequest = new { PrimaryImageUrl = image.PublicUrl };
            var response = await client.PutAsJsonAsync($"/api/admin/products/{productId}", updateRequest);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Imagem principal definida com sucesso";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Erro ao definir imagem principal: {error}";
            }

            return RedirectToPage(new { productId });
        }

        public async Task<IActionResult> OnPostReorderAsync(Guid productId, string imageOrder)
        {
            var imageIds = imageOrder.Split(',').Select(Guid.Parse).ToList();
            var client = _httpClientFactory.CreateClient("ApiClient");

            for (int i = 0; i < imageIds.Count; i++)
            {
                var request = new { SortOrder = i };
                var response = await client.PutAsJsonAsync(
                    $"/api/admin/products/{productId}/images/{imageIds[i]}/sort-order",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Erro ao reordenar imagens";
                    return RedirectToPage(new { productId });
                }
            }

            TempData["Success"] = "Imagens reordenadas com sucesso";
            return RedirectToPage(new { productId });
        }
    }

    public class ProductViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? PrimaryImageUrl { get; set; }
    }

    public class ProductImageViewModel
    {
        public Guid Id { get; set; }
        public string ObjectKey { get; set; } = string.Empty;
        public string PublicUrl { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long? SizeBytes { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}