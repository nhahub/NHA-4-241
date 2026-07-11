using Microsoft.AspNetCore.Mvc;
using MultiVendorMarketplace.Application.Models;
using MultiVendorMarketplace.Application.Services;
using MultiVendorMarketplace.Web.Extensions;

namespace MultiVendorMarketplace.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ProductService _productService;
        private const string CartSessionKey = "MarketplaceCart";

        public CartController(ProductService productService)
        {
            _productService = productService;
        }

        private Cart GetCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey);
            if (cart == null)
            {
                cart = new Cart();
                HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
            }
            return cart;
        }

        private void SaveCart(Cart cart)
        {
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var cart = GetCart();
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    VendorId = product.VendorId,
                    VendorName = product.Vendor?.StoreName ?? "Independent Seller",
                    ImageUrl = product.ImageUrl
                });
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                cart.Items.Remove(item);
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return RemoveFromCart(productId);
            }

            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToAction("Index");
        }
    }
}
