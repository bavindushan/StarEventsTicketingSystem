using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarEventsTicketingSystem.Models;
using StarEventsTicketingSystem.ViewModels;
using StarEventsTicketingSystem.Enums;
using System;
using System.Threading.Tasks;
using StarEventsTicketingSystem.Data;

namespace StarEventsTicketingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrEmpty(model.Role))
            {
                ModelState.AddModelError("Role", "Invalid role selected.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                LoyaltyPoints = new LoyaltyPoints { Points = 0 },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }
                await _userManager.AddToRoleAsync(user, model.Role);

                // 🔹 Audit Log → Account Registered
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    user.Id,
                    AuditLogAction.Register,
                    $"User registered with email {user.Email}, Role: {model.Role}"
                );

                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    await _signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty, "User not found.");
                    return View(model);
                }

                // 🔹 Audit Log → Successful Login
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    user.Id,
                    AuditLogAction.Login,
                    $"User logged in with email {user.Email}"
                );

                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                else if (roles.Contains("Organizer"))
                    return RedirectToAction("Index", "OrganizerDashboard");
                else if (roles.Contains("Customer"))
                    return RedirectToAction("Index", "CustomerDashboard");
                else
                    return RedirectToAction("Index", "Home");
            }

            // 🔹 Audit Log → Failed Login Attempt
            var failedUser = await _userManager.FindByEmailAsync(model.Email);
            var failedUserId = failedUser?.Id ?? "Unknown";

            var auditLogFailedController = new AuditLogController(_context);
            await auditLogFailedController.InsertLog(
                failedUserId,
                AuditLogAction.LoginFailed,
                $"Failed login attempt for email {model.Email}"
            );

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = _userManager.GetUserId(User);
            var userEmail = _userManager.GetUserName(User);

            await _signInManager.SignOutAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                var auditLogController = new AuditLogController(_context);
                await auditLogController.InsertLog(
                    userId,
                    AuditLogAction.Logout,
                    $"User {userEmail} logged out"
                );
            }

            return Redirect("https://localhost:7166/"); // redirect to homepage
        }
    }
}
