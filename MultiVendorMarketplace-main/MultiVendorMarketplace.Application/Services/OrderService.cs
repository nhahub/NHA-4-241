using Microsoft.EntityFrameworkCore;
using MultiVendorMarketplace.Application.Interfaces;
using MultiVendorMarketplace.Application.Models;
using MultiVendorMarketplace.Domain.Entities;

namespace MultiVendorMarketplace.Application.Services
{
    public class OrderService
    {
        private readonly IApplicationDbContext _context;
        private readonly CommissionService _commissionService;
        private readonly IEmailSender _emailSender;

        public OrderService(IApplicationDbContext context, CommissionService commissionService, IEmailSender emailSender)
        {
            _context = context;
            _commissionService = commissionService;
            _emailSender = emailSender;
        }

        public async Task<Order> CreateOrderFromCartAsync(int customerId, Cart cart, string shippingAddress)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == customerId);
            
            if (customer == null)
                throw new ArgumentException("Customer not found");

            decimal totalCommission = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                var product = await _context.Products
                    .Include(p => p.Vendor)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null)
                    throw new ArgumentException($"Product {item.ProductId} not found");

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for product: {product.Name}");

                // Decrement stock
                product.StockQuantity -= item.Quantity;

                var vendor = product.Vendor;
                decimal commRate = vendor?.CommissionRate ?? 0.10M;

                var (platformComm, vendorPayout) = _commissionService.CalculateCommissionAndPayout(item.Price, item.Quantity, commRate);
                totalCommission += platformComm;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    VendorId = product.VendorId,
                    PayoutAmount = vendorPayout,
                    PayoutStatus = "Pending"
                });
            }

            var order = new Order
            {
                CustomerId = customerId,
                TotalAmount = cart.TotalAmount,
                PlatformCommission = totalCommission,
                Status = "Pending",
                ShippingAddress = shippingAddress,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Send order initial creation email
            if (customer.User?.Email != null)
            {
                await _emailSender.SendEmailAsync(
                    customer.User.Email,
                    $"Order Placed Successfully #{order.Id}",
                    $"<h3>Thank you for your order!</h3><p>Your order #{order.Id} for a total of {order.TotalAmount:C} is currently pending payment.</p>"
                );
            }

            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .ThenInclude(c => c!.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Vendor)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<Order>> GetOrdersForCustomerAsync(int customerId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null)
            {
                string oldStatus = order.Status;
                order.Status = status;
                await _context.SaveChangesAsync();

                // Trigger status update email notifications
                if (order.Customer?.User?.Email != null && oldStatus != status)
                {
                    string subject = $"Order Status Updated #{order.Id}";
                    string message = $"<p>Your order #{order.Id} status has changed from <strong>{oldStatus}</strong> to <strong>{status}</strong>.</p>";
                    
                    if (status.Equals("Shipped", StringComparison.OrdinalIgnoreCase))
                    {
                        message += "<p>Great news! Your package has been handed to the courier. You will receive it shortly.</p>";
                    }
                    else if (status.Equals("OutForDelivery", StringComparison.OrdinalIgnoreCase))
                    {
                        message += "<p>Your order is out for delivery today. Please ensure someone is available to receive it!</p>";
                    }
                    else if (status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                    {
                        message += "<p>Your package has been delivered. Thank you for shopping with us!</p>";
                    }

                    await _emailSender.SendEmailAsync(order.Customer.User.Email, subject, message);
                }
            }
        }

        public async Task<List<OrderItem>> GetOrderItemsForVendorAsync(int vendorId)
        {
            return await _context.OrderItems
                .Include(oi => oi.Order)
                .ThenInclude(o => o!.Customer)
                .Include(oi => oi.Product)
                .Where(oi => oi.VendorId == vendorId)
                .OrderByDescending(oi => oi.Order!.OrderDate)
                .ToListAsync();
        }
    }
}
