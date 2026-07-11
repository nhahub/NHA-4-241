using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MultiVendorMarketplace.Infrastructure.Data;
using MultiVendorMarketplace.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MultiVendorMarketplace.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    UserType = model.UserType
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Add to appropriate role
                    await _userManager.AddToRoleAsync(user, model.UserType);

                    // Create secondary profile
                    if (model.UserType == "Vendor")
                    {
                        var vendor = new Vendor
                        {
                            UserId = user.Id,
                            StoreName = model.StoreName ?? "My Shop",
                            Description = model.StoreDescription ?? string.Empty,
                            CommissionRate = 0.10M, // 10% standard fee
                            IsApproved = false // requires admin approval
                        };
                        _context.Vendors.Add(vendor);
                    }
                    else
                    {
                        var customer = new Customer
                        {
                            UserId = user.Id,
                            FirstName = model.FirstName ?? "Guest",
                            LastName = model.LastName ?? "User"
                        };
                        _context.Customers.Add(customer);
                    }

                    await _context.SaveChangesAsync();

                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Register As")]
        public string UserType { get; set; } = "Customer"; // Customer or Vendor

        // Customer details
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }
        
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        // Vendor details
        [Display(Name = "Store Name")]
        public string? StoreName { get; set; }
        
        [Display(Name = "Store Description")]
        public string? StoreDescription { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
