using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiVendorMarketplace.Application.Interfaces;
using MultiVendorMarketplace.Application.Models;
using MultiVendorMarketplace.Application.Services;
using MultiVendorMarketplace.Infrastructure.Data;
using MultiVendorMarketplace.Domain.Entities;
using MultiVendorMarketplace.Web.Extensions;

namespace MultiVendorMarketplace.Web.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CheckoutController : Controller
    {
        private readonly OrderService _orderService;
        private readonly IStripeService _stripeService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "MarketplaceCart";

        public CheckoutController(
            OrderService orderService,
            IStripeService stripeService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _orderService = orderService;
            _stripeService = stripeService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey);
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null)
            {
                // Create customer profile if missing
                customer = new Customer { UserId = user.Id, FirstName = "Valued", LastName = "Customer" };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            ViewBag.Cart = cart;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(string streetAddress, string city, string state, string postalCode)
        {
            string shippingAddress = $"{streetAddress}, {city}, {state} {postalCode}";
            var cart = HttpContext.Session.GetObjectFromJson<Cart>(CartSessionKey);
            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return RedirectToAction("Index");

            try
            {
                // Create order (calculates platform commissions and vendor payout balances)
                var order = await _orderService.CreateOrderFromCartAsync(customer.Id, cart, shippingAddress);

                // Set callback URLs
                string domain = $"{Request.Scheme}://{Request.Host}";
                string successUrl = $"{domain}/Checkout/Success?orderId={order.Id}&session_id={{CHECKOUT_SESSION_ID}}";
                string cancelUrl = $"{domain}/Checkout/Cancel?orderId={order.Id}";

                // Invoke Stripe service for checkout session
                string stripeUrl = await _stripeService.CreateCheckoutSessionAsync(order, successUrl, cancelUrl);

                // Store session details in database
                order.StripeSessionId = stripeUrl.Contains("mock_session_id") ? "mock_session_id_" + Guid.NewGuid() : stripeUrl;
                await _context.SaveChangesAsync();

                // Redirect to payment screen
                return Redirect(stripeUrl);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Checkout failed: {ex.Message}");
                ViewBag.Cart = cart;
                return View("Index");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Success(int orderId, string session_id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Vendor)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Clear session cart
            HttpContext.Session.Remove(CartSessionKey);

            // In mock mode, we simulate the webhook payment completion directly here
            if (session_id.Contains("mock_session_id") || order.StripeSessionId.Contains("mock_session_id"))
            {
                if (order.Status == "Pending")
                {
                    // Complete order payment and generate vendor transfers
                    order.Status = "Paid";
                    
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Vendor != null && !string.IsNullOrEmpty(item.Vendor.StripeAccountId))
                        {
                            // Trigger mock Transfer
                            string transferId = await _stripeService.ProcessPayoutTransferAsync(item);
                            item.StripeTransferId = transferId;
                            item.PayoutStatus = "Transferred";
                        }
                    }
                    await _context.SaveChangesAsync();
                    
                    // Trigger email notification via OrderService update
                    await _orderService.UpdateOrderStatusAsync(order.Id, "Paid");
                }
            }

            return View(order);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Cancel(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }
    }
}
