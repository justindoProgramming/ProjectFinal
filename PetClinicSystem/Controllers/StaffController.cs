using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;

namespace PetClinicSystem.Controllers
{
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _db;

        public StaffController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // SESSION
            var staffId = HttpContext.Session.GetInt32("UserId");
            if (staffId == null)
                return RedirectToAction("Index", "Home");

            ViewBag.Layout = "_Layout_Staff";
            ViewBag.ActiveMenu = "Dashboard";

            // ===========================
            // QUICK METRICS
            // ===========================

            ViewBag.TodayAppointments = _db.Schedule
                .Where(s => s.StaffId == staffId &&
                            s.ScheduleDateOld.HasValue &&
                            s.ScheduleDateOld.Value.Date == DateTime.Today)
                .Count();

            ViewBag.PendingAppointments = _db.Schedule
                .Where(s => s.StaffId == staffId && s.Status == "Pending")
                .Count();

            ViewBag.TotalPatients = _db.MedicalRecords
                .Where(r => r.StaffId == staffId)
                .Select(r => r.PetId)
                .Distinct()
                .Count();

            // ===========================
            // TODAY'S APPOINTMENTS
            // (Slot-based)
            // ===========================

            ViewBag.TodayList = _db.Schedule
                .Include(s => s.Pet)
                .Include(s => s.Slot)          // ✔ include slot model
                .Where(s => s.StaffId == staffId &&
                            s.ScheduleDateOld.HasValue &&
                            s.ScheduleDateOld.Value.Date == DateTime.Today)
                .OrderBy(s => s.Slot.StartTime) // ✔ NEW: sort by slot time
                .Take(5)
                .ToList();

            // ===========================
            // RECENT PATIENTS THEY HANDLED
            // ===========================

            ViewBag.RecentPatients = _db.MedicalRecords
                .Include(r => r.Pet)
                .Where(r => r.StaffId == staffId)
                .OrderByDescending(r => r.Date)
                .Take(5)
                .ToList();

            return View();
        }
    }
}
