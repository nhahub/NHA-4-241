using Microsoft.EntityFrameworkCore;
using MultiVendorMarketplace.Application.Interfaces;
using MultiVendorMarketplace.Domain.Entities;

namespace MultiVendorMarketplace.Application.Services
{
    public class ProductService
    {
        private readonly IApplicationDbContext _context;

        public ProductService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetActiveProductsAsync(string? search, int? categoryId)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Vendor)
                .Where(p => p.IsApproved && p.Vendor!.IsApproved && p.StockQuantity > 0)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Vendor)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Product>> GetProductsByVendorIdAsync(int vendorId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.VendorId == vendorId)
                .ToListAsync();
        }

        public async Task CreateProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Product product)
        {
            var existing = await _context.Products.FindAsync(product.Id);
            if (existing != null)
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.StockQuantity = product.StockQuantity;
                existing.CategoryId = product.CategoryId;
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    existing.ImageUrl = product.ImageUrl;
                }
                // When modified, we can keep approval state or auto-approve. Let's keep approval state
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task ApproveProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsApproved = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
