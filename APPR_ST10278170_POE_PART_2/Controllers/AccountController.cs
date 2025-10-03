using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace APPR_ST10278170_POE_PART_2.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔓 GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl)
        {
            return View("Register", new AccountViewModel
            {
                ReturnUrl = returnUrl ?? "/Dashboard/Index"
            });
        }

        // ✅ POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Register(AccountViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Register", model);

            if (_context.AppUsers.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View("Register", model);
            }

            var newUser = new AppUser
            {
                Username = model.Username,
                PasswordHash = HashPassword(model.Password),
                Role = "User"
            };

            _context.AppUsers.Add(newUser);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Registration successful. Please log in.";
            return RedirectToAction("Login", "Account");
        }

        // 🔓 GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl)
        {
            return View("Login", new AccountViewModel
            {
                ReturnUrl = returnUrl ?? "/Dashboard/Index"
            });
        }

        // ✅ POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Login(AccountViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Login", model);

            var hashed = HashPassword(model.Password);
            var user = _context.AppUsers.FirstOrDefault(u =>
                u.Username == model.Username && u.PasswordHash == hashed);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View("Login", model);
            }

            TempData["Username"] = user.Username;
            return Redirect(model.ReturnUrl ?? "/Dashboard/Index");
        }

        // 🔐 Secure password hashing
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
    }
}
