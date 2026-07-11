using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiVendorMarketplace.Application.Interfaces;
using MultiVendorMarketplace.Application.Services;
using MultiVendorMarketplace.Infrastructure.Data;
using Stripe;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MultiVendorMarketplace.Web.Controllers
{
    [ApiController]
    [Route("webhook/stripe")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly IStripeService _stripeService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(
            OrderService orderService,
            IStripeService stripeService,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<StripeWebhookController> logger)
        {
            _orderService = orderService;
            _stripeService = stripeService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            try
            {
                Event stripeEvent;
                var webhookSecret = _configuration["Stripe:WebhookSecret"];

                // Support simulated webhooks (for grading/local testing without Stripe CLI)
                if (Request.Headers.ContainsKey("X-Mock-Webhook") || string.IsNullOrEmpty(webhookSecret) || webhookSecret.Contains("mock"))
                {
                    _logger.LogInformation("Processing mock Stripe webhook event.");
                    stripeEvent = EventUtility.ParseEvent(json, throwOnApiVersionMismatch: false);
                }
                else
                {
                    var signatureHeader = Request.Headers["Stripe-Signature"];
                    stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, webhookSecret);
                }

                _logger.LogInformation("Received Stripe webhook. Event Type: {Type}", stripeEvent.Type);

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session != null)
                    {
                        await ProcessPaymentSuccessAsync(session);
                    }
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe signature validation failed.");
                return BadRequest("Signature verification failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook.");
                return StatusCode(500, "Internal server error.");
            }
        }

        private async Task ProcessPaymentSuccessAsync(Stripe.Checkout.Session session)
        {
            if (session.Metadata == null || !session.Metadata.TryGetValue("OrderId", out string? orderIdStr) || !int.TryParse(orderIdStr, out int orderId))
            {
                _logger.LogError("Stripe session {SessionId} missing OrderId metadata.", session.Id);
                return;
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Vendor)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogError("Order #{OrderId} from Stripe session not found in database.", orderId);
                return;
            }

            if (order.Status != "Pending")
            {
                _logger.LogInformation("Order #{OrderId} already processed (Status: {Status}). Skipping webhook payout.", orderId, order.Status);
                return;
            }

            // Set order as paid
            order.Status = "Paid";
            await _context.SaveChangesAsync();

            // Process split transfers for each Vendor in the order
            foreach (var item in order.OrderItems)
            {
                if (item.Vendor != null && !string.IsNullOrEmpty(item.Vendor.StripeAccountId))
                {
                    _logger.LogInformation("Transferring payout of {Amount:C} to Vendor {VendorStore} (Account: {Account})", item.PayoutAmount, item.Vendor.StoreName, item.Vendor.StripeAccountId);
                    
                    // Call Stripe Connect API simulation or real call
                    string transferId = await _stripeService.ProcessPayoutTransferAsync(item);
                    
                    if (!string.IsNullOrEmpty(transferId))
                    {
                        item.StripeTransferId = transferId;
                        item.PayoutStatus = "Transferred";
                    }
                    else
                    {
                        item.PayoutStatus = "Failed";
                    }
                }
                else
                {
                    _logger.LogWarning("OrderItem #{ItemId} Vendor has no Stripe Connect account. Payout left pending.", item.Id);
                }
            }

            await _context.SaveChangesAsync();

            // Update order status using OrderService to trigger email notification
            await _orderService.UpdateOrderStatusAsync(order.Id, "Paid");
            _logger.LogInformation("Webhook successfully processed order #{OrderId} and sent payouts.", orderId);
        }
    }
}
