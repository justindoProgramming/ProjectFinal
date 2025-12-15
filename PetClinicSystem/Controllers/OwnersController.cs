using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;

namespace PetClinicSystem.Controllers
{
    public class OwnersController : Controller
    {
        private readonly ApplicationDbContext _db;


        public OwnersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ======== Role helpers ========
        private int UserRole => HttpContext.Session.GetInt32("UserRole") ?? -1;
        private bool CanManageOwners => UserRole == 1 || UserRole == 2; // admin or staff

        // ======== Index (cards list) ========
        // /Owners?search=...
        public IActionResult Index(string? search)
        {
            ViewBag.ActiveMenu = "Owners";
            ViewBag.Search = search ?? string.Empty;

            var query = _db.Owners
                           .Include(o => o.Pets)
                           .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                query = query.Where(o =>
                    o.FullName.ToLower().Contains(q) ||
                    (o.Email != null && o.Email.ToLower().Contains(q)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.ToLower().Contains(q)));
            }

            var owners = query
                .OrderBy(o => o.FullName)
                .ToList();

            return View(owners);
        }

        // ===================== CLIENT SELF-REGISTRATION =====================
        // /Owners/RegisterAsOwner   (GET)
        [HttpGet]
        public IActionResult RegisterAsOwner()
        {

            if (UserRole != 0)
                return Unauthorized(); // only clients allowed

            var model = new Owner();
            return View("ClientRegisterOwner", model);
        }

        // /Owners/RegisterAsOwner   (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterAsOwner(Owner model)
        {
            if (UserRole != 0)
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                // redisplay form with validation errors
                return View("ClientRegisterOwner", model);
            }

            _db.Owners.Add(model);
            _db.SaveChanges();

            TempData["Success"] = "Your owner profile has been registered.";

            // redirect to whatever client page you prefer
            // (change "ClientRecords" if you want a different landing page)
            return RedirectToAction("Index", "ClientRecords");
        }


        // ======== CREATE ========
        [HttpGet]
        public IActionResult CreateOwner()
        {
            if (!CanManageOwners)
                return Unauthorized();

            var model = new Owner();
            return PartialView("_Modal_CreateOwner", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateOwner(Owner model)
        {
            if (!CanManageOwners)
                return Json(new { success = false, message = "You are not allowed to create owners." });

            if (string.IsNullOrWhiteSpace(model.FullName))
                return Json(new { success = false, message = "Full name is required." });

            try
            {
                _db.Owners.Add(model);
                _db.SaveChanges();
                return Json(new { success = true, message = "Owner added successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error while saving owner." });
            }
        }

        // ======== VIEW ========
        [HttpGet]
        public IActionResult ViewOwner(int id)
        {
            var owner = _db.Owners
                           .Include(o => o.Pets)
                           .FirstOrDefault(o => o.OwnerId == id);

            if (owner == null)
                return NotFound();

            return PartialView("_Modal_ViewOwner", owner);
        }

        // ======== EDIT ========
        [HttpGet]
        public IActionResult EditOwner(int id)
        {
            if (!CanManageOwners)
                return Unauthorized();

            var owner = _db.Owners.FirstOrDefault(o => o.OwnerId == id);
            if (owner == null)
                return NotFound();

            return PartialView("_Modal_EditOwner", owner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditOwner(Owner model)
        {
            if (!CanManageOwners)
                return Json(new { success = false, message = "You are not allowed to edit owners." });

            if (model.OwnerId <= 0)
                return Json(new { success = false, message = "Invalid owner." });

            if (string.IsNullOrWhiteSpace(model.FullName))
                return Json(new { success = false, message = "Full name is required." });

            var owner = _db.Owners.FirstOrDefault(o => o.OwnerId == model.OwnerId);
            if (owner == null)
                return Json(new { success = false, message = "Owner not found." });

            try
            {
                // Update the fields your modals use
                owner.FullName = model.FullName;
                owner.PhoneNumber = model.PhoneNumber;
                owner.Email = model.Email;
                owner.Address = model.Address;
                owner.EmergencyContact1 = model.EmergencyContact1;
                owner.EmergencyContact2 = model.EmergencyContact2;

                _db.Owners.Update(owner);
                _db.SaveChanges();

                return Json(new { success = true, message = "Owner updated successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error while updating owner." });
            }
        }

        // ======== DELETE ========
        [HttpGet]
        public IActionResult DeleteOwner(int id)
        {
            if (!CanManageOwners)
                return Unauthorized();

            var owner = _db.Owners
                           .Include(o => o.Pets)
                           .FirstOrDefault(o => o.OwnerId == id);

            if (owner == null)
                return NotFound();

            return PartialView("_Modal_DeleteOwner", owner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteOwnerConfirmed(int id)
        {
            if (!CanManageOwners)
                return Json(new { success = false, message = "You are not allowed to delete owners." });

            var owner = _db.Owners
                           .Include(o => o.Pets)
                           .FirstOrDefault(o => o.OwnerId == id);

            if (owner == null)
                return Json(new { success = false, message = "Owner not found." });

            if (owner.Pets != null && owner.Pets.Any())
            {
                return Json(new { success = false, message = "Cannot delete owner with existing patients." });
            }

            try
            {
                _db.Owners.Remove(owner);
                _db.SaveChanges();
                return Json(new { success = true, message = "Owner deleted successfully." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error while deleting owner." });
            }
        }
    }
}
