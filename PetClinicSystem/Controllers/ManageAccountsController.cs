using Microsoft.AspNetCore.Mvc;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PetClinicSystem.Controllers
{
    public class ManageAccountsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ManageAccountsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ========= HELPER: HASH PASSWORD =========
        private string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return null;

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

        // ========= MAIN PAGE =========
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "ManageAccounts";

            var accounts = _db.Accounts
                .OrderBy(a => a.FullName)
                .ToList();

            return View(accounts);
        }

        // LIST PARTIAL (AJAX refresh)
        public IActionResult AccountList()
        {
            var accounts = _db.Accounts
                .OrderBy(a => a.FullName)
                .ToList();

            return PartialView("_AccountCards", accounts);
        }

        // ========= CREATE =========
        [HttpGet]
        public IActionResult CreateAccount()
        {
            var model = new Account
            {
                IsActive = true,
                IsAdmin = 2,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            return PartialView("_Modal_CreateAccount", model);
        }

        [HttpPost]
        public IActionResult CreateAccount(Account model, string password)
        {
            // Minimal manual validation
            if (string.IsNullOrWhiteSpace(model.FullName) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Username))
            {
                return BadRequest("Name, email, and username are required.");
            }

            if (string.IsNullOrWhiteSpace(password))
                return BadRequest("Password is required.");

            // Read IsActive from checkbox:
            // if checkbox present -> true, if missing -> false
            bool isActive = !string.IsNullOrEmpty(Request.Form["IsActive"]);

            // Always hash password
            var hash = HashPassword(password);
            model.PasswordHash = hash;

            model.IsActive = isActive;
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _db.Accounts.Add(model);
            _db.SaveChanges();

            return Ok(new { message = "User created successfully." });
        }

        // ========= VIEW =========
        [HttpGet]
        public IActionResult ViewAccount(int id)
        {
            var acc = _db.Accounts.FirstOrDefault(a => a.AccountId == id);
            if (acc == null) return NotFound();

            return PartialView("_Modal_ViewAccount", acc);
        }

        // ========= EDIT =========
        [HttpGet]
        public IActionResult EditAccount(int id)
        {
            var acc = _db.Accounts.FirstOrDefault(a => a.AccountId == id);
            if (acc == null) return NotFound();

            return PartialView("_Modal_EditAccount", acc);
        }

        [HttpPost]
        public IActionResult EditAccount(Account model, string newPassword)
        {
            var acc = _db.Accounts.FirstOrDefault(a => a.AccountId == model.AccountId);
            if (acc == null) return NotFound();

            // Minimal checks
            if (string.IsNullOrWhiteSpace(model.FullName) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Username))
            {
                return BadRequest("Name, email, and username are required.");
            }

            // Read IsActive from checkbox (same pattern as create)
            bool isActive = !string.IsNullOrEmpty(Request.Form["IsActive"]);

            // Copy editable fields
            acc.FullName = model.FullName;
            acc.Email = model.Email;
            acc.Username = model.Username;
            acc.IsAdmin = model.IsAdmin;
            acc.IsActive = isActive;

            // If new password was provided, hash & update
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var hash = HashPassword(newPassword);
                acc.PasswordHash = hash;
            }

            acc.UpdatedAt = DateTime.Now;

            _db.SaveChanges();

            return Ok(new { message = "User updated successfully." });
        }

        // ========= DELETE =========
        [HttpGet]
        public IActionResult DeleteAccount(int id)
        {
            var acc = _db.Accounts.FirstOrDefault(a => a.AccountId == id);
            if (acc == null) return NotFound();

            return PartialView("_Modal_DeleteAccount", acc);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAccountConfirmed(int id)
        {
            var acc = _db.Accounts.FirstOrDefault(a => a.AccountId == id);
            if (acc == null)
                return NotFound();

            // Soft delete instead of hard delete
            acc.IsActive = false;
            acc.UpdatedAt = DateTime.Now;

            _db.SaveChanges();

            return Ok(new { message = "Account deactivated successfully." });
        }

    }
}
