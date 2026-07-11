namespace MultiVendorMarketplace.Domain.Entities
{
    public class Vendor
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string StripeAccountId { get; set; } = string.Empty;
        public decimal CommissionRate { get; set; } = 0.10M; // default 10%
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
