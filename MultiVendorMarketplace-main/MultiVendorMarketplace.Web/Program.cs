using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiVendorMarketplace.Application.Interfaces;
using MultiVendorMarketplace.Application.Services;
using MultiVendorMarketplace.Infrastructure.Data;
using MultiVendorMarketplace.Infrastructure.Services;
using MultiVendorMarketplace.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// 1. Add SQL Server Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("MultiVendorMarketplace.Infrastructure")));

// 2. Add ASP.NET Identity Framework
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Simple configuration for development/testing environments
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Add Session Support (for Shopping Cart)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Configure Application Cookies (Redirects for unauthorized paths)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// 5. Register Clean Architecture Dependencies
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IEmailSender, MockEmailSender>();

// Register Application Services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CommissionService>();
builder.Services.AddScoped<AnalyticsService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 6. Database Migration and Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Seed default database entities
        await ApplicationDbContextSeed.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database migration or seeding.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable session state
app.UseSession();

// Authentication & Authorization middleware sequence
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
