using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;

namespace PetClinicSystem.Controllers
{
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ClientController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Index", "Home");

            // 1️⃣ Find owner linked to this account
            var owner = _db.Owners.FirstOrDefault(o => o.AccountId == userId);

            if (owner == null)
            {
                // Safety fallback — client has no owner record
                ViewBag.MyPets = new List<Pet>();
                ViewBag.Upcoming = new List<Schedule>();
                ViewBag.Vaccines = new List<Vaccination>();
                return View();
            }

            // 2️⃣ Load only the client’s pets correctly
            var pets = _db.Pets
                .Include(p => p.Owner)
                .Where(p => p.OwnerId == owner.OwnerId)
                .ToList();

            ViewBag.MyPets = pets;

            // 3️⃣ Upcoming appointments
            var upcoming = _db.Schedule
                .Include(s => s.Pet)
                .Where(s =>
                    s.Pet.OwnerId == owner.OwnerId &&
                    s.ScheduleDateOld >= DateTime.Today)
                .OrderBy(s => s.ScheduleDateOld)
                .ToList();

            ViewBag.Upcoming = upcoming;

            // 4️⃣ Vaccination reminders
            var vaccines = _db.Vaccinations
                .Include(v => v.Pet)
                .Where(v => v.Pet.OwnerId == owner.OwnerId)
                .ToList();

            ViewBag.Vaccines = vaccines;

            return View();
        }
    }
}
