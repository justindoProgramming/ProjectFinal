using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;

namespace PetClinicSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AppointmentsController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int? UserId => HttpContext.Session.GetInt32("UserId");
        private int? UserRole => HttpContext.Session.GetInt32("UserRole");

        private int? GetOwnerId(int? accountId)
        {
            return _db.Owners
                .Where(o => o.AccountId == accountId)
                .Select(o => o.OwnerId)
                .FirstOrDefault();
        }

        // -------------------------------------------------------------
        // LOAD DROPDOWNS
        // -------------------------------------------------------------
        private void LoadDropdowns()
        {
            int? ownerId = GetOwnerId(UserId);

            ViewBag.Pets = (UserRole == 0)
                ? _db.Pets.Where(p => p.OwnerId == ownerId).Include(p => p.Owner).ToList()
                : _db.Pets.Include(p => p.Owner).ToList();

            ViewBag.Staff = _db.Accounts.Where(a => a.IsAdmin == 2).ToList();
            ViewBag.Services = _db.Service.OrderBy(s => s.Name).ToList();
        }

        // -------------------------------------------------------------
        // STATUS RULES
        // -------------------------------------------------------------
        private static readonly Dictionary<string, string[]> AllowedStatusTransitions = new()
        {
            { "pending",   new[] { "confirmed", "urgent", "completed", "cancelled" } },
            { "confirmed", new[] { "urgent", "completed", "cancelled" } },
            { "urgent",    new[] { "confirmed", "completed", "cancelled" } },
            { "completed", Array.Empty<string>() },
            { "cancelled", Array.Empty<string>() }
        };

        private bool CanChangeStatus(string oldStatus, string newStatus)
        {
            oldStatus = oldStatus?.ToLower() ?? "pending";
            newStatus = newStatus?.ToLower() ?? "pending";

            if (oldStatus == newStatus) return true;
            if (AllowedStatusTransitions.TryGetValue(oldStatus, out var allowed))
                return allowed.Contains(newStatus);

            return false;
        }

        // -------------------------------------------------------------
        // CONFLICT CHECKER
        // -------------------------------------------------------------
        private bool HasConflict(DateTime date, int startSlotId, int blocksNeeded, int? excludeScheduleId = null)
        {
            var slots = _db.TimeSlots.OrderBy(t => t.StartTime).ToList();
            int index = slots.FindIndex(s => s.SlotId == startSlotId);
            if (index < 0) return true;

            var booked = _db.Schedule
                .Where(s => s.ScheduleDateOld == date && s.ScheduleId != excludeScheduleId)
                .Select(s => s.SlotId.Value)
                .ToHashSet();

            for (int b = 0; b < blocksNeeded; b++)
            {
                if (index + b >= slots.Count) return true;
                int blockSlotId = slots[index + b].SlotId;

                if (booked.Contains(blockSlotId))
                    return true;
            }

            return false;
        }

        // -------------------------------------------------------------
        // INDEX VIEW
        // -------------------------------------------------------------
        public IActionResult Index(string? search)
        {
            ViewBag.ActiveMenu = "Appointments";

            var query = _db.Schedule
                .Include(s => s.Pet).ThenInclude(p => p.Owner)
                .Include(s => s.Staff)
                .Include(s => s.Service)
                .Include(s => s.Slot)
                .AsQueryable();

            int? ownerId = GetOwnerId(UserId);

            if (UserRole == 0)
                query = query.Where(s => s.Pet.OwnerId == ownerId);

            if (UserRole == 2)
                query = query.Where(s => s.StaffId == UserId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(s =>
                    s.Pet.Name.ToLower().Contains(search) ||
                    s.Service.Name.ToLower().Contains(search) ||
                    s.Status.ToLower().Contains(search)
                );
            }

            return View(query
                .OrderBy(s => s.ScheduleDateOld)
                .ThenBy(s => s.Slot.StartTime)
                .ToList());
        }

        // -------------------------------------------------------------
        // CREATE (GET)
        // -------------------------------------------------------------
        public IActionResult Create()
        {
            LoadDropdowns();
            return PartialView("_Modal_CreateAppointment");
        }

        // -------------------------------------------------------------
        // CREATE (POST)
        // -------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Schedule model)
        {
            if (!model.ScheduleDateOld.HasValue || !model.SlotId.HasValue)
            {
                TempData["Error"] = "Please select a date and time slot.";
                return RedirectToAction("Index");
            }

            if (model.ScheduleDateOld.Value < DateTime.Today)
            {
                TempData["Error"] = "You cannot book a past date.";
                return RedirectToAction("Index");
            }

            if (model.ScheduleDateOld.Value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                TempData["Error"] = "Weekends are not available.";
                return RedirectToAction("Index");
            }

            var service = _db.Service.FirstOrDefault(s => s.ServiceId == model.ServiceId);
            if (service == null)
            {
                TempData["Error"] = "Invalid service selection.";
                return RedirectToAction("Index");
            }

            int blocksNeeded = Math.Max(1, service.DurationMinutes / 30);

            // Ensure selected slot is not in the past (if booking for today)
            var selectedSlot = _db.TimeSlots.FirstOrDefault(t => t.SlotId == model.SlotId);
            if (model.ScheduleDateOld.HasValue && model.ScheduleDateOld.Value.Date == DateTime.Today && selectedSlot != null)
            {
                if (selectedSlot.StartTime <= DateTime.Now.TimeOfDay)
                {
                    TempData["Error"] = "You cannot book a time that is already in the past. Please select a future time.";
                    return RedirectToAction("Index");
                }
            }

            if (HasConflict(model.ScheduleDateOld.Value, model.SlotId.Value, blocksNeeded))
            {
                TempData["Error"] = "This timeslot overlaps with another booking.";
                return RedirectToAction("Index");
            }



            // ⭐ NEW: Allow client to set Pending or Urgent
            if (UserRole == 0)
            {
                if (model.Status != "Pending" && model.Status != "Urgent")
                    model.Status = "Pending";
            }

            model.ServiceName = service.Name;

            _db.Schedule.Add(model);
            _db.SaveChanges();

            TempData["Success"] = "Appointment created successfully.";
            return RedirectToAction("Index");
        }

        // -------------------------------------------------------------
        // EDIT (GET)
        // -------------------------------------------------------------
        public IActionResult Edit(int id)
        {
            int? ownerId = GetOwnerId(UserId);

            var appt = _db.Schedule
                .Include(s => s.Pet).ThenInclude(p => p.Owner)
                .Include(s => s.Service)
                .FirstOrDefault(s => s.ScheduleId == id);

            if (appt == null) return Content("Appointment not found.");

            // ⭐ NEW: Completed cannot be edited
            if (appt.Status?.ToLower() == "completed")
                return Content("Completed appointments cannot be edited.");

            if (UserRole == 0 && appt.Pet.OwnerId != ownerId)
                return Content("Unauthorized.");

            LoadDropdowns();
            return PartialView("_Modal_EditAppointment", appt);
        }

        // -------------------------------------------------------------
        // EDIT (POST)
        // -------------------------------------------------------------
        [HttpPost]
        public IActionResult Edit(Schedule model)
        {
            var appt = _db.Schedule.FirstOrDefault(s => s.ScheduleId == model.ScheduleId);
            if (appt == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("Index");
            }

            // ⭐ NEW: Completed is locked
            if (appt.Status?.ToLower() == "completed")
            {
                TempData["Error"] = "Completed appointments cannot be edited.";
                return RedirectToAction("Index");
            }

            if (!model.SlotId.HasValue)
                model.SlotId = appt.SlotId;

            var service = _db.Service.FirstOrDefault(s => s.ServiceId == model.ServiceId);
            if (service == null)
            {
                TempData["Error"] = "Invalid service.";
                return RedirectToAction("Index");
            }

            int blocksNeeded = Math.Max(1, service.DurationMinutes / 30);

            // --- PAST SLOT CHECK (EDIT) ---
            // If user selected a slot for today, ensure its start time is still in the future
            var selectedSlot = _db.TimeSlots.FirstOrDefault(t => t.SlotId == model.SlotId);
            if (model.ScheduleDateOld.HasValue && model.ScheduleDateOld.Value.Date == DateTime.Today && selectedSlot != null)
            {
                if (selectedSlot.StartTime <= DateTime.Now.TimeOfDay)
                {
                    TempData["Error"] = "You cannot select a timeslot in the past. Please choose a future time.";
                    return RedirectToAction("Index");
                }
            }

            bool scheduleChanged =
                model.SlotId != appt.SlotId ||
                model.ScheduleDateOld != appt.ScheduleDateOld;

            if (scheduleChanged)
            {
                if (!model.ScheduleDateOld.HasValue)
                {
                    TempData["Error"] = "Please select a valid date.";
                    return RedirectToAction("Index");
                }

                if (HasConflict(model.ScheduleDateOld.Value, model.SlotId.Value, blocksNeeded, appt.ScheduleId))
                {
                    TempData["Error"] = "That timeslot is already booked.";
                    return RedirectToAction("Index");
                }
            }

            // Update fields
            appt.PetId = model.PetId;
            appt.StaffId = model.StaffId;
            appt.ServiceId = model.ServiceId;
            appt.ServiceName = service.Name;
            appt.ScheduleDateOld = model.ScheduleDateOld;
            appt.SlotId = model.SlotId;

            // Only staff/admin can change status
            // 
            if (appt.Status?.ToLower() == "confirmed")
            {
                // keep original status, ignore submitted one
                model.Status = appt.Status;

                // If the user tries to change it, reject
                if (model.Status?.ToLower() != appt.Status?.ToLower())
                {
                    TempData["Error"] = "Confirmed appointments cannot change their status.";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                // Allowed transitions for non-confirmed statuses
                if (UserRole != 0)
                {
                    if (CanChangeStatus(appt.Status, model.Status))
                        appt.Status = model.Status;
                    else
                    {
                        TempData["Error"] = $"Invalid status change: {appt.Status} → {model.Status}";
                        return RedirectToAction("Index");
                    }
                }
            }


            _db.SaveChanges();

            TempData["Success"] = "Appointment updated successfully.";
            return RedirectToAction("Index");
        }

        // -------------------------------------------------------------
        // DELETE
        // -------------------------------------------------------------
        public IActionResult Delete(int id)
        {
            var appt = _db.Schedule
                .Include(s => s.Pet)
                .FirstOrDefault(s => s.ScheduleId == id);

            return PartialView("_Modal_DeleteAppointment", appt);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var appt = _db.Schedule.Find(id);
            if (appt != null)
                _db.Schedule.Remove(appt);

            _db.SaveChanges();
            TempData["Success"] = "Appointment deleted.";

            return RedirectToAction("Index");
        }

        // -------------------------------------------------------------
        // VIEW
        // -------------------------------------------------------------
        public IActionResult ViewAppointment(int id)
        {
            var appt = _db.Schedule
                .Include(s => s.Pet).ThenInclude(p => p.Owner)
                .Include(s => s.Service)
                .Include(s => s.Staff)
                .Include(s => s.Slot)
                .FirstOrDefault(s => s.ScheduleId == id);

            return PartialView("_Modal_ViewAppointment", appt);
        }

        // -------------------------------------------------------------
        // AJAX – VALID TIME SLOTS
        // -------------------------------------------------------------
        [HttpGet]
        public IActionResult GetValidStartTimes(DateTime date, int serviceId)
        {
            if (date < DateTime.Today) return Json(new List<object>());
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return Json(new List<object>());

            var service = _db.Service.FirstOrDefault(s => s.ServiceId == serviceId);
            if (service == null) return Json(new List<object>());

            int blocksNeeded = Math.Max(1, service.DurationMinutes / 30);
            var slots = _db.TimeSlots.OrderBy(t => t.StartTime).ToList();

            var booked = _db.Schedule
                .Where(s => s.ScheduleDateOld == date)
                .Select(s => s.SlotId)
                .ToHashSet();

            bool isToday = date.Date == DateTime.Today;
            TimeSpan nowTime = DateTime.Now.TimeOfDay;

            var valid = new List<object>();

            for (int i = 0; i < slots.Count; i++)
            {
                bool ok = true;

                // multi-block check
                for (int b = 0; b < blocksNeeded; b++)
                {
                    if (i + b >= slots.Count) { ok = false; break; }
                    if (booked.Contains(slots[i + b].SlotId)) { ok = false; break; }
                }

                // If this is today, skip start times that are already <= now
                if (ok && isToday)
                {
                    if (slots[i].StartTime <= nowTime)
                    {
                        ok = false;
                    }
                }

                if (ok)
                {
                    valid.Add(new
                    {
                        slotId = slots[i].SlotId,
                        start = $"{slots[i].StartTime:hh\\:mm}"
                    });
                }
            }

            return Json(valid);
        }

    }
}
