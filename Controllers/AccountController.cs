using Batarilan_Exercise1.Data;
using Batarilan_Exercise1.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Batarilan_Exercise1.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            // Already logged in → redirect to their dashboard
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                var role = HttpContext.Session.GetString("UserRole") ?? "";
                return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                    ? RedirectToAction("Dashboard", "Admin")
                    : RedirectToAction("Dashboard", "User");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string idNumber, string password)
        {
            if (string.IsNullOrWhiteSpace(idNumber) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter both ID Number and password.";
                return View();
            }

            var user = _context.Users
                .FirstOrDefault(u => u.IdNumber == idNumber && u.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Invalid ID Number or password. Please try again.";
                return View();
            }

            // ── Cookie authentication ──
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(ClaimTypes.Name,           user.FirstName ?? string.Empty),
                new System.Security.Claims.Claim(ClaimTypes.Role,           user.Role      ?? "User")
            };

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // ── Session values ──
            HttpContext.Session.SetInt32("UserId",    user.Id);
            HttpContext.Session.SetString("UserRole", user.Role      ?? "User");
            HttpContext.Session.SetString("UserName", user.FirstName ?? string.Empty);

            // ── Redirect based on role (case-insensitive) ──
            return user.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Dashboard", "User");
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(UserAccount model)
        {
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "An account with this email already exists.");
                    return View(model);
                }

                model.Role   = "Student";
                model.Points = 0;

                _context.Users.Add(model);
                _context.SaveChanges();

                TempData["Success"] = "Registration successful! Please log in.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
