using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MultiVendorMarketplace.Application.Services;
using MultiVendorMarketplace.Infrastructure.Data;
using MultiVendorMarketplace.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace MultiVendorMarketplace.Web.Controllers
{
    [Authorize(Roles = "Vendor")]
    public class VendorController : Controller
    {
        private readonly AnalyticsService _analyticsService;
        private readonly ProductService _productService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public VendorController(
            AnalyticsService analyticsService,
            ProductService productService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _analyticsService = analyticsService;
            _productService = productService;
            _userManager = userManager;
            _context = context;
            _env = env;
        }

        private async Task<Vendor?> GetCurrentVendorAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Vendors.FirstOrDefaultAsync(v => v.UserId == user.Id);
        }

        public async Task<IActionResult> Dashboard()
        {
            var vendor = await GetCurrentVendorAsync();
            if (vendor == null) return Challenge();

            var viewModel = await _analyticsService.GetVendorDashboardAsync(vendor.Id);
            return View(viewModel);
        }

        public async Task<IActionResult> Products()
        {
            var vendor = await GetCurrentVendorAsync();
            if (vendor == null) return Challenge();

            var products = await _productService.GetProductsByVendorIdAsync(vendor.Id);
            ViewBag.VendorIsApproved = vendor.IsApproved;
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            var categories = await _productService.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product, IFormFile imageFile)
        {
            var vendor = await GetCurrentVendorAsync();
            if (vendor == null) return Challenge();

            // Ignore fields that are assigned programmatically
            ModelState.Remove("Vendor");
            ModelState.Remove("Category");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                product.VendorId = vendor.Id;
                product.IsApproved = false; // Requires administrator approval
                product.CreatedAt = DateTime.UtcNow;
                
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(uploadsDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    product.ImageUrl = "/uploads/" + fileName;
                }
                else
                {
                    // Fallback placeholder image if none provided
                    product.ImageUrl = "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=400&fit=crop";
                }

                await _productService.CreateProductAsync(product);
                TempData["Success"] = "Product created successfully. It will list on the marketplace once approved by an Admin.";
                return RedirectToAction("Products");
            }

            var categories = await _productService.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var vendor = await GetCurrentVendorAsync();
            if (vendor == null) return Challenge();

            var product = await _productService.GetProductByIdAsync(id);
            if (product == null || product.VendorId != vendor.Id)
            {
                return NotFound();
            }

            var categories = await _productService.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product, IFormFile imageFile)
        {
            var vendor = await GetCurrentVendorAsync();
            if (vendor == null) return Challenge();

            if (id != product.Id) return BadRequest();

            ModelState.Remove("Vendor");
            ModelState.Remove("Category");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                var existing = await _context.Products.FindAsync(id);
                if (existing == null || existing.VendorId != vendor.Id)
                {
                    return NotFound();
                }

                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.StockQuantity = product.StockQuantity;
                existing.CategoryId = product.CategoryId;
                
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(uploadsDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    existing.ImageUrl = "/uploads/" + fileName;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Product updated successfully.";
                return RedirectToAction("Products");
            }

            var categories = await _productService.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var vendor = await GetCurrentVendorAsync();
            if (vendor == null) return Challenge();

            var product = await _context.Products.FindAsync(id);
            if (product == null || product.VendorId != vendor.Id)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Product deleted successfully.";
            return RedirectToAction("Products");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnboardStripe()
        {
            var vendor = await GetCurrentVendorAsync();
            if (vendor == null) return Challenge();

            vendor.StripeAccountId = $"acct_mock_connect_{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10)}";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Successfully linked Stripe Connect Sandbox account!";
            return RedirectToAction("Dashboard");
        }
    }
}
