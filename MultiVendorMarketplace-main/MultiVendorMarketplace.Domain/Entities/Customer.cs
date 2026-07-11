namespace MultiVendorMarketplace.Domain.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
