using Microsoft.EntityFrameworkCore;
using MultiVendorMarketplace.Domain.Entities;

namespace MultiVendorMarketplace.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Vendor> Vendors { get; }
        DbSet<Customer> Customers { get; }
        DbSet<Product> Products { get; }
        DbSet<Category> Categories { get; }
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
