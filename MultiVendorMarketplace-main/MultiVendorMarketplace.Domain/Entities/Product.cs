namespace MultiVendorMarketplace.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public bool IsApproved { get; set; } = false;
        public int VendorId { get; set; }
        public int CategoryId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Vendor? Vendor { get; set; }
        public Category? Category { get; set; }
    }
}
