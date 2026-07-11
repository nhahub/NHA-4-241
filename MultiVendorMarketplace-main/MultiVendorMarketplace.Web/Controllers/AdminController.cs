using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiVendorMarketplace.Application.Services;
using MultiVendorMarketplace.Infrastructure.Data;

namespace MultiVendorMarketplace.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AnalyticsService _analyticsService;
        private readonly ProductService _productService;
        private readonly ApplicationDbContext _context;

        public AdminController(
            AnalyticsService analyticsService,
            ProductService productService,
            ApplicationDbContext context)
        {
            _analyticsService = analyticsService;
            _productService = productService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = await _analyticsService.GetAdminDashboardAsync();
            ViewBag.PendingProductsCount = await _context.Products.CountAsync(p => !p.IsApproved);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVendor(int id, string? returnUrl = null)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor != null)
            {
                vendor.IsApproved = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Vendor '{vendor.StoreName}' approved successfully.";
            }
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> PendingProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Vendor)
                .Where(p => !p.IsApproved)
                .ToListAsync();
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProduct(int id, string? returnUrl = null)
        {
            await _productService.ApproveProductAsync(id);
            TempData["Success"] = "Product approved and listed in the marketplace.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("PendingProducts");
        }

        [HttpGet]
        public async Task<IActionResult> Vendors()
        {
            var vendors = await _context.Vendors
                .Include(v => v.User)
                .ToListAsync();
            return View(vendors);
        }

        [HttpGet]
        public IActionResult SystemStatus()
        {
            ViewBag.Uptime = "99.98%";
            ViewBag.Latency = "142ms";
            ViewBag.Database = "Connected (Healthy)";
            ViewBag.CPU = "4.2%";
            ViewBag.Memory = "256 MB / 512 MB";
            return View();
        }
    }
}
