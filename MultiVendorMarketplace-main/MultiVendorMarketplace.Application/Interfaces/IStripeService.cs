using MultiVendorMarketplace.Domain.Entities;

namespace MultiVendorMarketplace.Application.Interfaces
{
    public interface IStripeService
    {
        Task<string> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl);
        Task<string> ProcessPayoutTransferAsync(OrderItem item);
    }
}
