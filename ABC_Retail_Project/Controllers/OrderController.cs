using ABC_Retail_Project.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail_Project.Controllers
{
    public class OrdersController : Controller
    {
        private readonly OrderService _orderService;
        private readonly CustomerService _customerService;
        private readonly ProductService _productService;

        public OrdersController(OrderService orderService, CustomerService customerService, ProductService productService)
        {
            _orderService = orderService;
            _customerService = customerService;
            _productService = productService;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetOrdersWithDetailsAsync();
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _orderService.GetOrderAsync("Order", id);
            if (order == null)
                return NotFound();

            var customers = await _customerService.GetCustomersAsync();
            var products = await _productService.GetProductsAsync();
            order.Customer = customers.FirstOrDefault(c => c.RowKey == order.CustomerId);
            order.Product = products.FirstOrDefault(p => p.RowKey == order.ProductId);

            return View(order);
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _customerService.GetCustomersAsync();
            ViewBag.Products = await _productService.GetProductsAsync();
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {

            ModelState.Remove("OrderDate");
            ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                try
                {
                    order.RowKey = Guid.NewGuid().ToString();
                    order.PartitionKey = "Order";
                    order.Status = "Pending"; 
                    order.OrderDate = DateTime.Now; 

                    await _orderService.AddOrderToQueueAsync(order);
                    TempData["SuccessMessage"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating order: {ex.Message}");
                    TempData["ErrorMessage"] = "Error creating order. Please try again.";
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }

                TempData["ErrorMessage"] = "Please fix the validation errors.";
            }

            ViewBag.Customers = await _customerService.GetCustomersAsync();
            ViewBag.Products = await _productService.GetProductsAsync();

            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _orderService.GetOrderAsync("Order", id);
            if (order == null)
                return NotFound();

            ViewBag.Customers = await _customerService.GetCustomersAsync();
            ViewBag.Products = await _productService.GetProductsAsync();
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Order order)
        {
            if (id != order.RowKey)
                return NotFound();

            ModelState.Remove("Customer");
            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                try
                {
                    await _orderService.UpdateOrderAsync(order);
                    TempData["SuccessMessage"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error updating order: {ex.Message}";
                }
            }

            ViewBag.Customers = await _customerService.GetCustomersAsync();
            ViewBag.Products = await _productService.GetProductsAsync();
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _orderService.GetOrderAsync("Order", id);
            if (order == null)
                return NotFound();

            // Load customer and product details for display
            var customers = await _customerService.GetCustomersAsync();
            var products = await _productService.GetProductsAsync();
            order.Customer = customers.FirstOrDefault(c => c.RowKey == order.CustomerId);
            order.Product = products.FirstOrDefault(p => p.RowKey == order.ProductId);

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _orderService.DeleteOrderAsync("Order", id);
            return RedirectToAction(nameof(Index));
        }
    }
}