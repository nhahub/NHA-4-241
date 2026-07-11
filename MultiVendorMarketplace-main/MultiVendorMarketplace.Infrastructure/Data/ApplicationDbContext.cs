using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiVendorMarketplace.Application.Interfaces;
using MultiVendorMarketplace.Domain.Entities;

namespace MultiVendorMarketplace.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vendor> Vendors => Set<Vendor>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Vendor
            builder.Entity<Vendor>()
                .HasOne(v => v.User)
                .WithOne()
                .HasForeignKey<Vendor>(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Vendor>()
                .Property(v => v.CommissionRate)
                .HasColumnType("decimal(18,4)");

            // Configure Customer
            builder.Entity<Customer>()
                .HasOne(c => c.User)
                .WithOne()
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Product
            builder.Entity<Product>()
                .HasOne(p => p.Vendor)
                .WithMany(v => v.Products)
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // EF Optimization: Index on Product Name for fast listing/search queries
            builder.Entity<Product>()
                .HasIndex(p => p.Name);

            // Configure Order
            builder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Order>()
                .Property(o => o.PlatformCommission)
                .HasColumnType("decimal(18,2)");

            // EF Optimization: Index on CustomerId for quick customer history loading
            builder.Entity<Order>()
                .HasIndex(o => o.CustomerId);

            // Configure OrderItem
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Vendor)
                .WithMany()
                .HasForeignKey(oi => oi.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<OrderItem>()
                .Property(oi => oi.PayoutAmount)
                .HasColumnType("decimal(18,2)");

            // EF Optimization: Index on VendorId for vendor payout query acceleration
            builder.Entity<OrderItem>()
                .HasIndex(oi => oi.VendorId);
        }
    }
}
