using Microsoft.AspNetCore.Mvc;
using MultiVendorMarketplace.Application.Services;
using System.Diagnostics;

namespace MultiVendorMarketplace.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProductService _productService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ProductService productService, ILogger<HomeController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId)
        {
            ViewBag.Categories = await _productService.GetCategoriesAsync();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchTerm = search;

            var products = await _productService.GetActiveProductsAsync(search, categoryId);
            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
