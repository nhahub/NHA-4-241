namespace MultiVendorMarketplace.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        
        public int VendorId { get; set; }
        public decimal PayoutAmount { get; set; }
        public string PayoutStatus { get; set; } = "Pending"; // Pending, Transferred, Failed
        public string StripeTransferId { get; set; } = string.Empty;

        // Navigation properties
        public Order? Order { get; set; }
        public Product? Product { get; set; }
        public Vendor? Vendor { get; set; }
    }
}
