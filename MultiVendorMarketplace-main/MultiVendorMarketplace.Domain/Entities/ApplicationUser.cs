using Microsoft.AspNetCore.Identity;

namespace MultiVendorMarketplace.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string UserType { get; set; } = "Customer"; // "Customer", "Vendor", "Admin"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
