using MultiVendorMarketplace.Domain.Entities;

namespace MultiVendorMarketplace.Application.Models
{
    public class VendorDashboardViewModel
    {
        public int VendorId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string StripeAccountId { get; set; } = string.Empty;
        public decimal CommissionRate { get; set; }
        public bool IsApproved { get; set; }

        public decimal TotalSales { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal PlatformCommissionDeducted { get; set; }
        
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }

        public List<RecentVendorOrderViewModel> RecentOrders { get; set; } = new List<RecentVendorOrderViewModel>();
        public List<MonthlySalesData> SalesHistory { get; set; } = new List<MonthlySalesData>();
    }

    public class RecentVendorOrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Payout { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PayoutStatus { get; set; } = string.Empty;
    }

    public class MonthlySalesData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public decimal Earnings { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public decimal TotalMarketplaceSales { get; set; }
        public decimal TotalCommissionEarned { get; set; }
        
        public int TotalVendorsCount { get; set; }
        public int PendingVendorsCount { get; set; }
        public int CustomersCount { get; set; }
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }

        public List<Vendor> PendingVendors { get; set; } = new List<Vendor>();
        public List<Order> RecentOrders { get; set; } = new List<Order>();
        public List<MonthlySalesData> GlobalSalesHistory { get; set; } = new List<MonthlySalesData>();
        
        public string SystemUptime { get; set; } = "99.9%";
        public string DBConnectionStatus { get; set; } = "Healthy";
    }
}
