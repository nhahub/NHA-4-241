namespace MultiVendorMarketplace.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public decimal PlatformCommission { get; set; }
        public string StripeSessionId { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped, OutForDelivery, Delivered, Cancelled
        public string ShippingAddress { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Customer? Customer { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
