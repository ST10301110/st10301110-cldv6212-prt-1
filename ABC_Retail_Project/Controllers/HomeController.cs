using System.Diagnostics;
using ABC_Retail_Project.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CustomerService _customerService;
        private readonly OrderService _orderService;
        private readonly ProductService _productService;

        public HomeController(ILogger<HomeController> logger,
                            CustomerService customerService,
                            OrderService orderService,
                            ProductService productService)
        {
            _logger = logger;
            _customerService = customerService;
            _orderService = orderService;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = new DashboardViewModel
            {
                TotalCustomers = (await _customerService.GetCustomersAsync()).Count,
                TotalProducts = (await _productService.GetProductsAsync()).Count,
                PendingOrders = await GetPendingOrdersCount()
            };
            return View(dashboard);
        }

        private async Task<int> GetPendingOrdersCount()
        {
            return 0;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
