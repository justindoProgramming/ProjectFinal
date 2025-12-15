using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PetClinicSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===================== PASSWORD HASHING =====================
        private string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return null;

            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha.ComputeHash(bytes);
            var sb = new StringBuilder();

            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        // ===================== LOGIN =====================
        [HttpGet]
        public IActionResult Login()
        {
            TempData["OpenLoginModal"] = "true";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Username and password are required.";
                TempData["OpenLoginModal"] = "true";
                return RedirectToAction("Index", "Home");
            }

            var hash = HashPassword(password);

            var user = await _db.Accounts
                .Include(a => a.Owner)
                .FirstOrDefaultAsync(a =>
                    a.Username == username &&
                    a.PasswordHash == hash &&
                    a.IsActive == true);

            if (user == null)
            {
                TempData["Error"] = "Invalid username or password.";
                TempData["OpenLoginModal"] = "true";
                return RedirectToAction("Index", "Home");
            }

            // Save session login
            HttpContext.Session.SetInt32("UserId", user.AccountId);
            HttpContext.Session.SetString("UserName", user.FullName ?? user.Username);
            HttpContext.Session.SetInt32("UserRole", user.IsAdmin);

            // Role Redirects
            return user.IsAdmin switch
            {
                1 => RedirectToAction("Index", "Admin"), // Admin
                2 => RedirectToAction("Index", "Staff"), // Staff
                _ => RedirectToAction("Index", "Client") // Client
            };
        }

        // ===================== REGISTER (ACCOUNT + OWNER) =====================
        [HttpPost]
        public async Task<IActionResult> Register(
            string FullName,
            string Email,
            string Username,
            string Password,
            string PhoneNumber,
            string Address,
            string EmergencyContact1,
            string EmergencyContact2)
        {
            // Validate duplicate email or username
            bool exists = await _db.Accounts
                .AnyAsync(a => a.Email == Email || a.Username == Username);

            if (exists)
            {
                TempData["Error"] = "Email or username already exists.";
                TempData["OpenSignupModal"] = "true";
                return RedirectToAction("Index", "Home");
            }

            // 1. Create Account
            var account = new Account
            {
                FullName = FullName,
                Email = Email,
                Username = Username,
                PasswordHash = HashPassword(Password),
                IsAdmin = 0, // Client
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _db.Accounts.AddAsync(account);
            await _db.SaveChangesAsync();

            // 2. Create Owner linked to account
            var owner = new Owner
            {
                FullName = FullName,
                Email = Email,
                PhoneNumber = PhoneNumber,
                Address = Address,
                EmergencyContact1 = EmergencyContact1,
                EmergencyContact2 = EmergencyContact2,
                AccountId = account.AccountId
            };

            await _db.Owners.AddAsync(owner);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Registration successful! You may now log in.";
            TempData["OpenLoginModal"] = "true";

            return RedirectToAction("Index", "Home");
        }

        // ===================== PROFILE (VIEW PAGE) =====================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Index", "Home");

            var user = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountId == userId);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index", "Home");
            }

            var model = new AccountProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Username = user.Username
            };

            return View(model);
        }

        // ===================== PROFILE (SAVE CHANGES) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AccountProfileViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Index", "Home");

            if (!ModelState.IsValid)
                return View(model);

            var user = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountId == userId);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index", "Home");
            }

            // Check duplicates (exclude self)
            bool exists = await _db.Accounts.AnyAsync(a =>
                a.AccountId != user.AccountId &&
                (a.Email == model.Email || a.Username == model.Username));

            if (exists)
            {
                ModelState.AddModelError(string.Empty, "Email or username is already in use.");
                return View(model);
            }

            // Update account fields
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Username = model.Username;
            user.UpdatedAt = DateTime.Now;

            // Update password if entered
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");
                    return View(model);
                }

                user.PasswordHash = HashPassword(model.NewPassword);
            }

            await _db.SaveChangesAsync();

            // Update session
            HttpContext.Session.SetString("UserName", user.FullName);

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }


        // ===================== LOGOUT =====================
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }
    }
}
