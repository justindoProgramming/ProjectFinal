using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PetClinicSystem.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SettingsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===== Helper: get current logged in user based on session =====
        private Account GetCurrentUser()
        {
            var idString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrWhiteSpace(idString))
                return null;

            if (!int.TryParse(idString, out var id))
                return null;

            return _db.Accounts.FirstOrDefault(a => a.AccountId == id);
        }

        // ===== Helper: same password hash as everywhere else =====
        private string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return string.Empty;

            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = sha.ComputeHash(bytes);
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // ===== GET: /Settings =====
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Settings";

            var user = GetCurrentUser();
            if (user == null)
            {
                // If not logged in, send to login page
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        // ===== PROFILE: EDIT (GET) =====
        [HttpGet]
        public IActionResult EditProfile()
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            return PartialView("_Modal_EditProfile", user);
        }

        // ===== PROFILE: EDIT (POST) =====
        [HttpPost]
        public IActionResult EditProfile(Account model)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(model.FullName) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Username))
            {
                return BadRequest("Full name, email, and username are required.");
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Username = model.Username;
            user.UpdatedAt = DateTime.Now;

            _db.SaveChanges();

            // update cached name in session so header reflects changes
            HttpContext.Session.SetString("UserName", user.FullName);

            return Ok(new { message = "Profile updated successfully." });
        }

        // ===== CHANGE PASSWORD: GET =====
        [HttpGet]
        public IActionResult ChangePassword()
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            return PartialView("_Modal_ChangePassword", user);
        }

        // DTO for password change
        public class ChangePasswordDto
        {
            public int AccountId { get; set; }
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
            public string ConfirmPassword { get; set; }
        }

        // ===== CHANGE PASSWORD: POST =====
        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordDto dto)
        {
            var user = GetCurrentUser();
            if (user == null || user.AccountId != dto.AccountId)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
                string.IsNullOrWhiteSpace(dto.NewPassword) ||
                string.IsNullOrWhiteSpace(dto.ConfirmPassword))
            {
                return BadRequest("Please fill out all fields.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return BadRequest("New password and confirm password do not match.");
            }

            // verify current password
            var currentHash = HashPassword(dto.CurrentPassword);
            if (!string.Equals(currentHash, user.PasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Current password is incorrect.");
            }

            // set new password
            user.PasswordHash = HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.Now;
            _db.SaveChanges();

            return Ok(new { message = "Password changed successfully." });
        }
    }
}
