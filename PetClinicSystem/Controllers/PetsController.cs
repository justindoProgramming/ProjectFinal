using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;

namespace PetClinicSystem.Controllers
{
    public class PetsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PetsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ========= SESSION / ROLE HELPERS =========
        private int UserId => HttpContext.Session.GetInt32("UserId") ?? 0;
        private int UserRole => HttpContext.Session.GetInt32("UserRole") ?? -1;

        private bool IsAdmin => UserRole == 1;
        private bool IsStaff => UserRole == 2;
        private bool IsClient => UserRole == 0;


        // =========================================
        // AUTO-GENERATE UNIQUE PET CODE
        // =========================================
        private string GeneratePetCode()
        {
            int lastId = _db.Pets.Max(p => (int?)p.PetId) ?? 0;
            int next = lastId + 1;
            return $"PET-{next:0000}";
        }


        // =========================================
        // INDEX – LIST ALL PETS (USER-BASED)
        // =========================================
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Pets";

            var query = _db.Pets.Include(p => p.Owner).AsQueryable();

            if (IsClient && UserId != 0)
            {
                var owner = _db.Owners.FirstOrDefault(o => o.AccountId == UserId);

                if (owner != null)
                    query = query.Where(p => p.OwnerId == owner.OwnerId);
                else
                    return View(new List<Pet>());
            }

            var pets = query.OrderBy(p => p.Name).ToList();
            return View(pets);
        }


      
        // CREATE PET (GET)
        [HttpGet]
        public IActionResult Create()
        {
            // By default include all owners for admin/staff
            var owners = _db.Owners.OrderBy(o => o.FullName).ToList();

            // If current user is a client/owner, limit to their owner record
            if (IsClient && UserId != 0)
            {
                var owner = _db.Owners.FirstOrDefault(o => o.AccountId == UserId);
                if (owner != null)
                {
                    owners = new List<Owner> { owner };
                    ViewBag.OwnerLocked = true;   // view will render read-only owner
                }
                else
                {
                    // No owner record exists for logged-in client (should not normally happen)
                    ViewBag.OwnerLocked = false;
                }
            }
            else
            {
                ViewBag.OwnerLocked = false;
            }

            ViewBag.Owners = owners;
            var model = new Pet { BirthDate = DateTime.Today };
            return PartialView("_Modal_CreatePet", model);
        }

        // CREATE PET (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Pet model)
        {
            // Server-side: If client, enforce owner = logged in owner
            if (IsClient && UserId != 0)
            {
                var owner = _db.Owners.FirstOrDefault(o => o.AccountId == UserId);
                if (owner == null)
                {
                    TempData["Error"] = "Owner profile not found for the current user.";
                    return RedirectToAction("Index");
                }
                model.OwnerId = owner.OwnerId;
            }

            if (!ModelState.IsValid)
            {
                // Rebuild owners list exactly as in GET for re-render
                var owners = _db.Owners.OrderBy(o => o.FullName).ToList();
                if (IsClient && UserId != 0)
                {
                    var owner = _db.Owners.FirstOrDefault(o => o.AccountId == UserId);
                    if (owner != null)
                    {
                        owners = new List<Owner> { owner };
                        ViewBag.OwnerLocked = true;
                    }
                    else
                    {
                        ViewBag.OwnerLocked = false;
                    }
                }
                else
                {
                    ViewBag.OwnerLocked = false;
                }

                ViewBag.Owners = owners;
                return PartialView("_Modal_CreatePet", model);
            }

            // Generate pet code and save
            model.PetCode = GeneratePetCode();
            model.Owner = null; // avoid EF attaching Owner nav prop
            _db.Pets.Add(model);
            _db.SaveChanges();

            TempData["Success"] = "Patient added successfully!";
            return RedirectToAction("Index");
        }



        // =========================================
        // EDIT PET (GET)
        // =========================================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var pet = _db.Pets.Include(p => p.Owner).FirstOrDefault(p => p.PetId == id);
            if (pet == null) return NotFound();

            ViewBag.Owners = _db.Owners.OrderBy(o => o.FullName).ToList();
            return PartialView("_Modal_EditPet", pet);
        }


        // =========================================
        // EDIT PET (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Pet model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Owners = _db.Owners.OrderBy(o => o.FullName).ToList();
                return PartialView("_Modal_EditPet", model);
            }

            model.Owner = null;

            _db.Pets.Update(model);
            _db.SaveChanges();

            TempData["Success"] = "Patient updated successfully!";
            return RedirectToAction("Index");
        }


        // =========================================
        // DELETE PET (GET)
        // =========================================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var pet = _db.Pets.Include(p => p.Owner).FirstOrDefault(p => p.PetId == id);
            if (pet == null) return NotFound();

            return PartialView("_Modal_DeletePet", pet);
        }


        // =========================================
        // DELETE PET (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var pet = _db.Pets.Find(id);
            if (pet == null) return NotFound();

            bool hasAppointments = _db.Schedule.Any(s => s.PetId == id);
            bool hasMedical = _db.MedicalRecords.Any(m => m.PetId == id);

            if (hasAppointments || hasMedical)
            {
                TempData["Error"] = "This patient has existing records and cannot be deleted.";
                return RedirectToAction("Index");
            }

            _db.Pets.Remove(pet);
            _db.SaveChanges();

            TempData["Success"] = "Patient deleted successfully!";
            return RedirectToAction("Index");
        }


        // =========================================
        // VIEW PET DETAILS
        // =========================================
        [HttpGet]
        public IActionResult Details(int id)
        {
            var pet = _db.Pets.Include(p => p.Owner).FirstOrDefault(p => p.PetId == id);
            if (pet == null) return NotFound();

            return PartialView("_Modal_ViewPet", pet);
        }
    }
}
