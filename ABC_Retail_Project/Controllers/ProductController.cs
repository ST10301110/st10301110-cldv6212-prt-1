using ABC_Retail_Project.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail_Project.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetProductsAsync();
            return View(products);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            // Remove validation for fields that are set programmatically
            ModelState.Remove("ImageUrl");
            ModelState.Remove("PartitionKey");
            ModelState.Remove("RowKey");
            ModelState.Remove("Timestamp");
            ModelState.Remove("ETag");

            if (ModelState.IsValid)
            {
                try
                {
                    // Generate keys programmatically
                    product.RowKey = Guid.NewGuid().ToString();
                    product.PartitionKey = "Product";

                    // Handle image upload if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        product.ImageUrl = await _productService.UploadImageAsync(imageFile, product.RowKey);
                    }
                    else
                    {
                        product.ImageUrl = null; // Or set a default image URL
                    }

                    await _productService.AddProductAsync(product);
                    TempData["SuccessMessage"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log the actual error for debugging
                    Console.WriteLine($"Error creating product: {ex.Message}");
                    TempData["ErrorMessage"] = "Error creating product. Please try again.";
                }
            }
            else
            {
                // Log validation errors for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }

                TempData["ErrorMessage"] = "Please fix the validation errors.";
            }

            return View(product);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var product = await _productService.GetProductAsync("Product", id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _productService.GetProductAsync("Product", id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Product product, IFormFile imageFile)
        {
            if (id != product.RowKey)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        product.ImageUrl = await _productService.UploadImageAsync(imageFile, product.RowKey);
                    }

                    await _productService.UpdateProductAsync(product);
                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error updating product";
                }
            }
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            var product = await _productService.GetProductAsync("Product", id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _productService.DeleteProductAsync("Product", id);
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting product";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}