using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;

namespace PetClinicSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.Layout = "_Layout_Admin";
            ViewBag.ActiveMenu = "Dashboard";

            // ---- METRICS ----
            ViewBag.TotalAppointments = _db.Schedule.Count();
            ViewBag.TotalPatients = _db.Pets.Count();
            ViewBag.ActiveStaff = _db.Accounts.Count(a => a.IsAdmin == 2);
            ViewBag.Revenue = _db.Billing.Any() ? _db.Billing.Sum(b => b.Total) : 0;

            // ---- TODAY'S APPOINTMENTS ----
            var today = DateTime.Today;

            ViewBag.TodayAppointments = _db.Schedule
                .Include(s => s.Pet)
                .Include(s => s.Staff)
                .Include(s => s.Slot)                 // ★ NEW: include slot info
                .Where(s => s.ScheduleDateOld.HasValue &&
                            s.ScheduleDateOld.Value.Date == today)
                .OrderBy(s => s.Slot.StartTime)       // ★ NEW: sort by slot time
                .Take(5)
                .ToList();

            // ---- RECENT PATIENTS ----
            ViewBag.RecentPatients = _db.Pets
                .Include(p => p.Owner)
                .OrderByDescending(p => p.PetId)
                .Take(5)
                .ToList();

            return View();
        }
    }
}
