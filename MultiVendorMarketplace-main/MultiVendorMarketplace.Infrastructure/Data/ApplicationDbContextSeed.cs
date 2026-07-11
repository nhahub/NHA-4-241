using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiVendorMarketplace.Domain.Entities;

namespace MultiVendorMarketplace.Infrastructure.Data
{
    public static class ApplicationDbContextSeed
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Ensure Database is Created & Migrated
            await context.Database.MigrateAsync();

            // Seed Roles
            var roles = new[] { "Admin", "Vendor", "Customer" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin User
            string adminEmail = "admin@marketplace.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    UserType = "Admin"
                };

                var result = await userManager.CreateAsync(admin, "AdminPassword123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // Seed Categories
            if (!await context.Categories.AnyAsync())
            {
                context.Categories.AddRange(
                    new Category { Name = "Handmade Crafts", Description = "Artisanal handcrafted goods and items." },
                    new Category { Name = "Vintage Apparel", Description = "Retro, classic, and pre-loved fashion." },
                    new Category { Name = "Home Decor", Description = "Unique items to beautify your living space." }
                );
                await context.SaveChangesAsync();
            }

            // Seed Vendor User
            string vendorEmail = "vendor@marketplace.com";
            if (await userManager.FindByEmailAsync(vendorEmail) == null)
            {
                var vendorUser = new ApplicationUser
                {
                    UserName = vendorEmail,
                    Email = vendorEmail,
                    EmailConfirmed = true,
                    UserType = "Vendor"
                };

                var result = await userManager.CreateAsync(vendorUser, "VendorPassword123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(vendorUser, "Vendor");

                    // Create Vendor profile
                    var vendorProfile = new Vendor
                    {
                        UserId = vendorUser.Id,
                        StoreName = "Artisanal Wonders",
                        Description = "Beautifully crafted ceramic and wooden goods made locally.",
                        LogoUrl = "https://images.unsplash.com/photo-1541256996761-85df2efdf1ac?w=100&h=100&fit=crop",
                        StripeAccountId = "acct_mock_payout_12345", // Mock ID
                        CommissionRate = 0.10M, // 10%
                        IsApproved = true
                    };
                    context.Vendors.Add(vendorProfile);
                    await context.SaveChangesAsync();

                    // Seed some Vendor Products
                    var craftCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Handmade Crafts");
                    var decorCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Home Decor");

                    if (craftCategory != null && decorCategory != null)
                    {
                        context.Products.AddRange(
                            new Product
                            {
                                Name = "Handcrafted Ceramic Mug",
                                Description = "Each mug is individually hand-thrown on the wheel. Food, microwave, and dishwasher safe.",
                                Price = 24.99M,
                                ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=400&fit=crop",
                                StockQuantity = 12,
                                IsApproved = true,
                                VendorId = vendorProfile.Id,
                                CategoryId = craftCategory.Id
                            },
                            new Product
                            {
                                Name = "Carved Wooden Jewelry Box",
                                Description = "Hand-carved walnut jewelry box with velvet-lined interior compartments.",
                                Price = 49.99M,
                                ImageUrl = "https://images.unsplash.com/photo-1582139329536-e7284fece509?w=400&fit=crop",
                                StockQuantity = 5,
                                IsApproved = true,
                                VendorId = vendorProfile.Id,
                                CategoryId = decorCategory.Id
                            }
                        );
                        await context.SaveChangesAsync();
                    }
                }
            }

            // Seed Customer User
            string customerEmail = "customer@marketplace.com";
            if (await userManager.FindByEmailAsync(customerEmail) == null)
            {
                var customerUser = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    EmailConfirmed = true,
                    UserType = "Customer"
                };

                var result = await userManager.CreateAsync(customerUser, "CustomerPassword123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "Customer");

                    var customerProfile = new Customer
                    {
                        UserId = customerUser.Id,
                        FirstName = "Jane",
                        LastName = "Doe"
                    };
                    context.Customers.Add(customerProfile);
                    await context.SaveChangesAsync();
                }
            }

            // Seed additional beautiful products if count is low
            if (await context.Products.CountAsync() < 5)
            {
                var vendorProfile = await context.Vendors.FirstOrDefaultAsync();
                if (vendorProfile != null)
                {
                    var craftCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Handmade Crafts");
                    var apparelCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Vintage Apparel") ?? new Category { Name = "Vintage Apparel", Description = "Retro, classic, and pre-loved fashion." };
                    var decorCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Home Decor");

                    if (craftCategory != null && decorCategory != null)
                    {
                        if (context.Entry(apparelCategory).State == EntityState.Detached && apparelCategory.Id == 0)
                        {
                            context.Categories.Add(apparelCategory);
                            await context.SaveChangesAsync();
                        }

                        context.Products.AddRange(
                            new Product
                            {
                                Name = "Handwoven Macrame Wall Hanging",
                                Description = "Intricate cotton macrame wall tapestry on a natural drift wood branch. Adds an elegant boho touch.",
                                Price = 34.99M,
                                ImageUrl = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=400&fit=crop",
                                StockQuantity = 8,
                                IsApproved = true,
                                VendorId = vendorProfile.Id,
                                CategoryId = craftCategory.Id
                            },
                            new Product
                            {
                                Name = "Leather Passport Wallet",
                                Description = "Minimalist vegetable-tanned leather holder with multiple card slots and passport pocket. Hand-stitched.",
                                Price = 39.99M,
                                ImageUrl = "https://images.unsplash.com/photo-1627124118303-624c8f5c8088?w=400&fit=crop",
                                StockQuantity = 15,
                                IsApproved = true,
                                VendorId = vendorProfile.Id,
                                CategoryId = craftCategory.Id
                            },
                            new Product
                            {
                                Name = "Retro Denim Jacket",
                                Description = "1990s oversized fit classic blue denim jacket. Premium heavy cotton, unisex design.",
                                Price = 59.99M,
                                ImageUrl = "https://images.unsplash.com/photo-1576995853123-5a10305d93c0?w=400&fit=crop",
                                StockQuantity = 4,
                                IsApproved = true,
                                VendorId = vendorProfile.Id,
                                CategoryId = apparelCategory.Id
                            },
                            new Product
                            {
                                Name = "Vintage Leather Combat Boots",
                                Description = "Distressed brown genuine leather boots with durable rubber grip soles. Built to last.",
                                Price = 89.99M,
                                ImageUrl = "https://images.unsplash.com/photo-1520639888713-7851133b1ed0?w=400&fit=crop",
                                StockQuantity = 3,
                                IsApproved = true,
                                VendorId = vendorProfile.Id,
                                CategoryId = apparelCategory.Id
                            },
                            new Product
                            {
                                Name = "Knit Wool Sweater",
                                Description = "Thick cable knit vintage cream sweater. Extremely warm, sourced from organic local wool.",
                                Price = 45.00M,
                                ImageUrl = "https://images.unsplash.com/photo-1620799140408-edc6dcb6d633?w=400&fit=crop",
                                StockQuantity = 6,
                                IsApproved = true,
                                VendorId = vendorProfile.Id,
                                CategoryId = apparelCategory.Id
                            },
                            new Product
                            {
                                Name = "Minimalist Brass Table Lamp",
                                Description = "Sleek gold brass base lamp with a frosted glass dome. Exudes a warm, diffused ambient glow.",
                                Price = 74.99M,
                                ImageUrl = "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=400&fit=crop",
                                StockQuantity = 7,
                                IsApproved = true,
                                VendorId = vendorProfile.Id,
                                CategoryId = decorCategory.Id
                            },
                            new Product
                            {
                                Name = "Aromatic Soy Candle Set",
                                Description = "Set of three hand-poured candles: lavender, warm vanilla, and cedarwood amber. 30 hours burn time each.",
                                Price = 28.00M,
                                ImageUrl = "https://images.unsplash.com/photo-1603006905003-be475563bc59?w=400&fit=crop",
                                StockQuantity = 20,
                                IsApproved = true,
                                VendorId = vendorProfile.Id,
                                CategoryId = decorCategory.Id
                            },
                            new Product
                            {
                                Name = "Wool Throw Blanket",
                                Description = "Ultra-soft merino wool knit throw blanket in olive green. Perfect accent for sofas and beds.",
                                Price = 65.00M,
                                ImageUrl = "https://images.unsplash.com/photo-1580301762395-21ce84d00bc6?w=400&fit=crop",
                                StockQuantity = 10,
                                IsApproved = true,
                                VendorId = vendorProfile.Id,
                                CategoryId = decorCategory.Id
                            }
                        );
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
