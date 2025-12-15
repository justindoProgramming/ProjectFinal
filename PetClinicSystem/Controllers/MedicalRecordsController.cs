using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;

namespace PetClinicSystem.Controllers
{
    public class MedicalRecordsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MedicalRecordsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===========================
        // MAIN LIST (ACTIVE RECORDS)
        // ===========================
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Records";
            ViewBag.IsArchiveList = false;   // tell partial we're in active mode

            var records = _db.MedicalRecords
                .Include(r => r.Pet).ThenInclude(p => p.Owner)
                .Include(r => r.Staff)
                .Where(r => !r.IsArchived)          // ✅ only active
                .OrderByDescending(r => r.Date)
                .ToList();

            return View(records);
        }

        // ===========================
        // ARCHIVED LIST
        // /MedicalRecords/Archived
        // ===========================
        [HttpGet]
        public IActionResult Archived()
        {
            ViewBag.ActiveMenu = "Records";
            ViewBag.IsArchiveList = true;    // tell partial we're in archive mode

            var records = _db.MedicalRecords
                .Include(r => r.Pet).ThenInclude(p => p.Owner)
                .Include(r => r.Staff)
                .Where(r => r.IsArchived)           // ✅ only archived
                .OrderByDescending(r => r.Date)
                .ToList();

            return View(records); // uses Views/MedicalRecords/Archived.cshtml
        }

        // ===========================
        // CREATE (GET)
        // Called by medicalrecords.js: loadRecordCreate()
        // ===========================
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .OrderBy(p => p.Name)
                .ToList();

            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 1 || a.IsAdmin == 2)
                .OrderBy(a => a.FullName)
                .ToList();

            var model = new MedicalRecord
            {
                Date = DateTime.Today
            };

            return PartialView("_Modal_CreateMedicalRecord", model);
        }

        // ===========================
        // CREATE (POST)
        // Form id: #createRecordForm (AJAX)
        // ===========================
        [HttpPost]
        public IActionResult Create(MedicalRecord model)
        {
            if (model.PetId <= 0 || model.StaffId <= 0)
            {
                return BadRequest("Please select a patient and a staff/doctor.");
            }

            if (!model.Date.HasValue)
                model.Date = DateTime.Today;

            model.IsArchived = false;  // ensure new record is active

            _db.MedicalRecords.Add(model);
            _db.SaveChanges();

            // your JS expects { message: "..." } and does location.reload()
            return Ok(new { message = "Medical record created successfully!" });
        }

        // ===========================
        // EDIT (GET)
        // Called by medicalrecords.js: loadRecordEdit(id)
        // ===========================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var record = _db.MedicalRecords
                .FirstOrDefault(r => r.RecordId == id);

            if (record == null)
                return NotFound();

            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .OrderBy(p => p.Name)
                .ToList();

            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 1 || a.IsAdmin == 2)
                .OrderBy(a => a.FullName)
                .ToList();

            return PartialView("_Modal_EditMedicalRecord", record);
        }

        // ===========================
        // EDIT (POST)
        // Form id: #editRecordForm (AJAX)
        // ===========================
        [HttpPost]
        public IActionResult Edit(MedicalRecord model)
        {
            var record = _db.MedicalRecords
                .FirstOrDefault(r => r.RecordId == model.RecordId);

            if (record == null)
                return NotFound("Medical record not found.");

            if (model.PetId <= 0 || model.StaffId <= 0)
            {
                return BadRequest("Please select a patient and a staff/doctor.");
            }

            if (!model.Date.HasValue)
                model.Date = DateTime.Today;

            // update fields
            record.PetId = model.PetId;
            record.StaffId = model.StaffId;
            record.PrescriptionId = model.PrescriptionId;
            record.VaccinationId = model.VaccinationId;
            record.Description = model.Description;
            record.Diagnosis = model.Diagnosis;
            record.Treatment = model.Treatment;
            record.Date = model.Date;
            // record.IsArchived stays as is

            _db.SaveChanges();

            return Ok(new { message = "Medical record updated successfully!" });
        }

        // ===========================
        // DELETE (GET) → ARCHIVE CONFIRM MODAL
        // Called by medicalrecords.js: loadRecordDelete(id)
        // ===========================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var record = _db.MedicalRecords
                .Include(r => r.Pet).ThenInclude(p => p.Owner)
                .Include(r => r.Staff)
                .FirstOrDefault(r => r.RecordId == id);

            if (record == null)
                return NotFound();

            return PartialView("_Modal_DeleteMedicalRecord", record);
        }

        // ===========================
        // DELETE (POST) → ARCHIVE
        // Form id: #deleteRecordForm (AJAX)
        // ===========================
        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var record = _db.MedicalRecords
                .FirstOrDefault(r => r.RecordId == id);

            if (record != null)
            {
                // ✅ ARCHIVE INSTEAD OF REAL DELETE
                record.IsArchived = true;
                _db.SaveChanges();
            }

            return Ok(new { message = "Medical record archived." });
        }

        // ===========================
        // RESTORE (from archived list)
        // Simple POST (non-AJAX)
        // ===========================
        [HttpPost]
        public IActionResult Restore(int id)
        {
            var record = _db.MedicalRecords
                .FirstOrDefault(r => r.RecordId == id);

            if (record == null)
                return NotFound();

            record.IsArchived = false;
            _db.SaveChanges();

            TempData["Success"] = "Medical record restored.";
            return RedirectToAction("Archived");
        }
    }
}
