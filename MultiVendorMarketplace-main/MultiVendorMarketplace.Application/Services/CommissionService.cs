namespace MultiVendorMarketplace.Application.Services
{
    public class CommissionService
    {
        public (decimal PlatformCommission, decimal VendorPayout) CalculateCommissionAndPayout(decimal unitPrice, int quantity, decimal commissionRate)
        {
            decimal totalLinePrice = unitPrice * quantity;
            decimal platformCommission = Math.Round(totalLinePrice * commissionRate, 2);
            decimal vendorPayout = totalLinePrice - platformCommission;

            return (platformCommission, vendorPayout);
        }
    }
}
