using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using PetClinicSystem.Models.ViewModels;
using System.Linq;

namespace PetClinicSystem.Controllers
{
    public class ClientRecordsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ClientRecordsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Index", "Home");

            // Get the REAL OwnerId linked to this logged-in Account
            int? ownerId = _db.Owners
                .Where(o => o.AccountId == userId)
                .Select(o => o.OwnerId)
                .FirstOrDefault();

            if (ownerId == null)
                return View(new ClientRecordsViewModel());

            // PETS
            var pets = _db.Pets
                .Include(p => p.Owner)
                .Where(p => p.OwnerId == ownerId)
                .ToList();

            // MEDICAL
            var medical = _db.MedicalRecords
                .Include(m => m.Pet)
                .Include(m => m.Staff)
                .Include(m => m.Prescription)
                .Include(m => m.Vaccination)
                .Where(m => m.Pet.OwnerId == ownerId)
                .OrderByDescending(m => m.Date)
                .ToList();

            // PRESCRIPTIONS
            var prescriptions = _db.Prescriptions
                .Include(p => p.Pet)
                .Include(p => p.Staff)
                .Where(p => p.Pet.OwnerId == ownerId)
                .OrderByDescending(p => p.Date)
                .ToList();

            // VACCINATIONS
            var vaccinations = _db.Vaccinations
                .Include(v => v.Pet)
                .Include(v => v.Staff)
                .Where(v => v.Pet.OwnerId == ownerId)
                .OrderByDescending(v => v.DateGiven)
                .ToList();

            var model = new ClientRecordsViewModel
            {
                Pets = pets,
                MedicalRecords = medical,
                Prescriptions = prescriptions,
                Vaccinations = vaccinations
            };

            ViewBag.ActiveMenu = "Records";
            return View(model);
        }

    }
}
