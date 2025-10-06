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
        public async Task<IActionResult> Index(string statusFilter)
        {
            var orders = await _orderService.GetOrdersWithDetailsAsync();

            // Apply status filter if provided
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                orders = orders.Where(o => o.Status == statusFilter).ToList();
                ViewBag.StatusFilter = statusFilter;
            }
            else
            {
                ViewBag.StatusFilter = "All";
            }

            ViewBag.AllStatuses = Order.GetAllStatuses();
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

            // Load customer and product details for display
            var customers = await _customerService.GetCustomersAsync();
            var products = await _productService.GetProductsAsync();
            order.Customer = customers.FirstOrDefault(c => c.RowKey == order.CustomerId);
            order.Product = products.FirstOrDefault(p => p.RowKey == order.ProductId);

            ViewBag.AllStatuses = Order.GetAllStatuses();
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
            // Remove validation for fields that are set programmatically or are computed
            ModelState.Remove("PartitionKey");
            ModelState.Remove("RowKey");
            ModelState.Remove("Timestamp");
            ModelState.Remove("ETag");
            ModelState.Remove("Customer");
            ModelState.Remove("Product");
            ModelState.Remove("CustomerName");
            ModelState.Remove("ProductName");
            ModelState.Remove("FormattedTotalAmount");
            ModelState.Remove("FormattedOrderDate");

            if (ModelState.IsValid)
            {
                try
                {
                    // Set values programmatically
                    order.RowKey = Guid.NewGuid().ToString();
                    order.PartitionKey = "Order";
                    order.OrderDate = DateTime.Now;
                    order.Status = "Pending"; // Set default status

                    await _orderService.AddOrderToQueueAsync(order);
                    TempData["SuccessMessage"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating order: {ex.Message}");
                    TempData["ErrorMessage"] = $"Error creating order: {ex.Message}";
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
            ViewBag.AllStatuses = Order.GetAllStatuses();
            ViewBag.CurrentStatus = order.Status;
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
            ModelState.Remove("CustomerName");
            ModelState.Remove("ProductName");
            ModelState.Remove("FormattedTotalAmount");
            ModelState.Remove("FormattedOrderDate");

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate status transition
                    var existingOrder = await _orderService.GetOrderAsync("Order", id);
                    if (existingOrder != null && !existingOrder.CanUpdateStatus(order.Status))
                    {
                        TempData["ErrorMessage"] = $"Invalid status transition from {existingOrder.Status} to {order.Status}";
                    }
                    else
                    {
                        await _orderService.UpdateOrderAsync(order);
                        TempData["SuccessMessage"] = "Order updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error updating order: {ex.Message}";
                }
            }

            ViewBag.Customers = await _customerService.GetCustomersAsync();
            ViewBag.Products = await _productService.GetProductsAsync();
            ViewBag.AllStatuses = Order.GetAllStatuses();
            ViewBag.CurrentStatus = order.Status;
            return View(order);
        }

        // POST: Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, string newStatus)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newStatus))
            {
                TempData["ErrorMessage"] = "Invalid request parameters.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var order = await _orderService.GetOrderAsync("Order", id);
                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate status transition
                if (!order.CanUpdateStatus(newStatus))
                {
                    TempData["ErrorMessage"] = $"Cannot change status from {order.Status} to {newStatus}.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Update status
                order.Status = newStatus;
                await _orderService.UpdateOrderAsync(order);

                TempData["SuccessMessage"] = $"Order status updated to {newStatus} successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating order status: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
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