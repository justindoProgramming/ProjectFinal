using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using PetClinicSystem.Services.Pdf;


namespace PetClinicSystem.Controllers
{
    public class BillingPharmacyController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BillingPharmacyController(ApplicationDbContext db)
        {
            _db = db;
        }



        private (bool ok, string error, List<string> warnings) ValidateDrugUse(int drugId, int quantityNeeded)
        {
            var d = _db.Drugs.FirstOrDefault(x => x.DrugId == drugId);
            var warnings = new List<string>();

            if (d == null)
                return (false, "Drug not found.", warnings);

            // BLOCKERS
            if (d.ExpiryDate < DateTime.Today)
                return (false, $"{d.DrugName} is EXPIRED and cannot be dispensed.", warnings);

            if (d.Quantity <= 0)
                return (false, $"{d.DrugName} is out of stock.", warnings);

            if (quantityNeeded > d.Quantity)
                return (false, $"Only {d.Quantity} unit(s) of {d.DrugName} remain.", warnings);

            // WARNINGS
            if (d.ExpiryDate <= DateTime.Today.AddDays(30))
                warnings.Add($"{d.DrugName} is expiring soon.");

            if (d.Quantity <= d.MinimumStock)
                warnings.Add($"{d.DrugName} is low on stock.");

            return (true, "", warnings);
        }


        // ============================================================
        // MAIN PAGE (POS + TABS)
        // ============================================================
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Billing";

            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .ToList();

            ViewBag.Prescriptions = _db.Prescriptions
                .Include(p => p.Pet).ThenInclude(o => o.Owner)
                .Include(p => p.Staff)
                .OrderByDescending(p => p.Date)
                .ToList();

            ViewBag.Drugs = _db.Drugs
                .OrderBy(d => d.DrugName)
                .ToList();

            ViewBag.Vaccinations = _db.Vaccinations
                .Include(v => v.Pet).ThenInclude(o => o.Owner)
                .Include(v => v.Staff)
                .OrderByDescending(v => v.DateGiven)
                .ToList();

            return View();
        }

        // ============================================================
        // POS — SAVE TRANSACTION (multi-item + safe rules)
        // ============================================================
        [HttpPost]
        public IActionResult SaveTransaction([FromBody] List<BillingDTO> items)
        {
            if (items == null || items.Count == 0)
                return Json(new { success = false, message = "Cart is empty." });

            int staffId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (staffId <= 0)
                return Json(new { success = false, message = "Unable to identify staff account." });

            string transactionId = Guid.NewGuid().ToString();

            foreach (var dto in items)
            {
                if (dto.PetId <= 0)
                    return Json(new { success = false, message = "Invalid pet selected." });

                if (string.IsNullOrWhiteSpace(dto.ServiceName))
                    return Json(new { success = false, message = "Service name missing." });

                if (dto.ServicePrice <= 0)
                    return Json(new { success = false, message = "Invalid price." });

                if (dto.Quantity <= 0)
                    return Json(new { success = false, message = "Invalid quantity." });

                var b = new Billing
                {
                    PetId = dto.PetId,
                    StaffId = staffId,
                    ServiceName = dto.ServiceName,
                    ServicePrice = dto.ServicePrice,
                    Quantity = dto.Quantity,
                    Total = dto.ServicePrice * dto.Quantity,
                    TransactionId = transactionId,
                    BillingDate = DateTime.Now
                };

                _db.Billing.Add(b);
            }

            _db.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Payment recorded successfully!",
                transactionId = transactionId
            });
        }


        // ============================================================
        // RECEIPT — MULTI ITEM (browser print-friendly)
        // ============================================================
        public IActionResult Receipt(string id)
        {
            var items = _db.Billing
                .Include(b => b.Pet).ThenInclude(p => p.Owner)
                .Include(b => b.Staff)
                .Where(b => b.TransactionId == id)
                .ToList();

            if (!items.Any())
                return NotFound("Receipt not found.");

            return View("_Receipt", items); // Multi-item receipt
        }

        // ============================================================
        // GET SERVICES FOR POS
        // ============================================================
        [HttpGet]
        public IActionResult GetServices()
        {
            var services = _db.Service
                .Select(s => new
                {
                    name = s.Name,
                    price = s.Price,
                    category = s.Category
                })
                .ToList();

            return Json(services);
        }

        // ============================================================
        // ADD SERVICE (POS)
        // ============================================================
        [HttpPost]
        public JsonResult AddService([FromBody] Service model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Invalid data." });

            try
            {
                _db.Service.Add(model);
                _db.SaveChanges();

                return Json(new { success = true, message = "Service added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // BILLING HISTORY LIST
        // ============================================================
        public IActionResult History()
        {
            var history = _db.Billing
                .Include(b => b.Pet).ThenInclude(o => o.Owner)
                .Include(b => b.Staff)
                .OrderByDescending(b => b.BillingDate)
                .ToList();

            return PartialView("_BillingHistory", history);
        }

        [HttpPost]
        public IActionResult FilterHistory(DateTime? startDate, DateTime? endDate, string search)
        {
            var query = _db.Billing
                .Include(b => b.Pet).ThenInclude(o => o.Owner)
                .Include(b => b.Staff)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(b => b.BillingDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(b => b.BillingDate <= endDate.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b =>
                    b.ServiceName.Contains(search) ||
                    b.Pet.Name.Contains(search) ||
                    b.Pet.Owner.FullName.Contains(search));

            var filtered = query
                .OrderByDescending(b => b.BillingDate)
                .ToList();

            return PartialView("_BillingHistoryList", filtered);
        }


        // ============================================================
        // AUTOCOMPLETE SEARCH (history search box)
        // ============================================================
        [HttpGet]
        public JsonResult SearchHistory(string term)
        {
            var results = _db.Billing
                .Where(b =>
                    b.ServiceName.Contains(term) ||
                    b.Pet.Name.Contains(term) ||
                    b.Pet.Owner.FullName.Contains(term))
                .Select(b => b.ServiceName)
                .Distinct()
                .Take(10)
                .ToList();

            return Json(results);
        }

        // ============================================================
        // EXPORT HISTORY TO EXCEL
        // ============================================================
        public IActionResult ExportHistoryExcel()
        {
            var data = _db.Billing
                .Include(b => b.Pet).ThenInclude(p => p.Owner)
                .Include(b => b.Staff)
                .ToList();

            var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Billing History");

            ws.Cell(1, 1).Value = "Service";
            ws.Cell(1, 2).Value = "Pet";
            ws.Cell(1, 3).Value = "Owner";
            ws.Cell(1, 4).Value = "Staff";
            ws.Cell(1, 5).Value = "BillingDate";
            ws.Cell(1, 6).Value = "Total";

            int row = 2;
            foreach (var b in data)
            {
                ws.Cell(row, 1).Value = b.ServiceName;
                ws.Cell(row, 2).Value = b.Pet.Name;
                ws.Cell(row, 3).Value = b.Pet.Owner.FullName;
                ws.Cell(row, 4).Value = b.Staff.FullName;
                ws.Cell(row, 5).Value = b.BillingDate;
                ws.Cell(row, 6).Value = b.Total;
                row++;
            }

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BillingHistory.xlsx"
            );
        }

        // ============================================================
        // PRESCRIPTIONS — LIST (AJAX)
        // ============================================================
        public IActionResult PrescriptionList()
        {
            var list = _db.Prescriptions
                .Include(p => p.Pet).ThenInclude(o => o.Owner)
                .Include(p => p.Staff)
                .OrderByDescending(p => p.Date)
                .ToList();

            return PartialView("_Prescriptions", list);
        }

        // ============================================================
        // PRESCRIPTIONS — CREATE
        // ============================================================
        [HttpGet]
        public IActionResult CreatePrescription()
        {
            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .ToList();

            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 1 || a.IsAdmin == 2)
                .ToList();

            ViewBag.Drugs = _db.Drugs
                .OrderBy(d => d.DrugName)
                .ToList();

            var model = new Prescription
            {
                Date = DateTime.Today
            };

            return PartialView("_Modal_CreatePrescription", model);
        }

        [HttpPost]
        public IActionResult CreatePrescription(Prescription model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid prescription data.");

            using var tx = _db.Database.BeginTransaction();

            try
            {
                Drug drug = null;

                // ==========================
                // DRUG VALIDATION
                // ==========================
                if (model.SelectedDrugId.HasValue && model.DispensedQuantity.HasValue)
                {
                    var result = ValidateDrugUse(model.SelectedDrugId.Value, model.DispensedQuantity.Value);

                    if (!result.ok)
                        return BadRequest(result.error);

                    // Add warnings to headers for UI to read
                    foreach (var w in result.warnings)
                        Response.Headers.Append("X-Drug-Warning", w);

                    // Reduce stock
                    drug = _db.Drugs.Find(model.SelectedDrugId.Value);
                    drug.Quantity -= model.DispensedQuantity.Value;
                    _db.SaveChanges();
                }

                // ==========================
                // SAVE PRESCRIPTION
                // ==========================
                _db.Prescriptions.Add(model);
                _db.SaveChanges();

                // ==========================
                // AUTO MEDICAL RECORD
                // ==========================
                var record = new MedicalRecord
                {
                    PetId = model.PetId,
                    StaffId = model.StaffId,
                    PrescriptionId = model.PrescriptionId,
                    Date = model.Date,
                    Description = $"Prescription for {model.Medication}",
                    Diagnosis = string.IsNullOrWhiteSpace(model.Notes) ? "Medication prescribed" : model.Notes,
                    Treatment = $"{model.Medication} — {model.Dosage}; {model.Frequency}; {model.Duration}"
                };

                _db.MedicalRecords.Add(record);
                _db.SaveChanges();

                tx.Commit();

                return Ok(new { message = "Prescription created successfully." });
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return StatusCode(500, ex.Message);
            }
        }


        // ============================================================
        // PRESCRIPTION — VIEW
        // ============================================================
        [HttpGet]
        public IActionResult ViewPrescription(int id)
        {
            var pres = _db.Prescriptions
                .Include(p => p.Pet).ThenInclude(o => o.Owner)
                .Include(p => p.Staff)
                .FirstOrDefault(p => p.PrescriptionId == id);

            if (pres == null)
                return NotFound();

            return PartialView("_Modal_ViewPrescription", pres);
        }

        // ============================================================
        // PRESCRIPTION — EDIT
        // ============================================================
        [HttpGet]
        public IActionResult EditPrescription(int id)
        {
            var pres = _db.Prescriptions.FirstOrDefault(p => p.PrescriptionId == id);
            if (pres == null) return NotFound();

            ViewBag.Pets = _db.Pets.Include(p => p.Owner).ToList();
            ViewBag.Staff = _db.Accounts.Where(a => a.IsAdmin == 1 || a.IsAdmin == 2).ToList();
            ViewBag.Drugs = _db.Drugs.OrderBy(d => d.DrugName).ToList();

            return PartialView("_Modal_EditPrescription", pres);
        }

        [HttpPost]
        public IActionResult EditPrescription(Prescription model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            using var tx = _db.Database.BeginTransaction();

            try
            {
                var existing = _db.Prescriptions
                    .AsNoTracking()
                    .FirstOrDefault(p => p.PrescriptionId == model.PrescriptionId);

                if (existing == null)
                    return BadRequest("Prescription not found.");

                // ==========================
                // RESTORE OLD STOCK
                // ==========================
                if (existing.SelectedDrugId.HasValue && existing.DispensedQuantity.HasValue)
                {
                    var oldDrug = _db.Drugs.Find(existing.SelectedDrugId.Value);
                    if (oldDrug != null)
                    {
                        oldDrug.Quantity += existing.DispensedQuantity.Value;
                        _db.SaveChanges();
                    }
                }

                // ==========================
                // VALIDATE NEW DRUG CHOICE
                // ==========================
                if (model.SelectedDrugId.HasValue && model.DispensedQuantity.HasValue)
                {
                    var result = ValidateDrugUse(model.SelectedDrugId.Value, model.DispensedQuantity.Value);

                    if (!result.ok)
                        return BadRequest(result.error);

                    // Warning headers
                    foreach (var w in result.warnings)
                        Response.Headers.Append("X-Drug-Warning", w);

                    // Subtract new quantity
                    var drug = _db.Drugs.Find(model.SelectedDrugId.Value);
                    drug.Quantity -= model.DispensedQuantity.Value;
                    _db.SaveChanges();
                }

                // ==========================
                // UPDATE PRESCRIPTION
                // ==========================
                _db.Prescriptions.Update(model);
                _db.SaveChanges();

                // ==========================
                // UPDATE MEDICAL RECORD
                // ==========================
                var record = _db.MedicalRecords.FirstOrDefault(r => r.PrescriptionId == model.PrescriptionId);
                if (record != null)
                {
                    record.PetId = model.PetId;
                    record.StaffId = model.StaffId;
                    record.Date = model.Date;
                    record.Description = $"Prescription for {model.Medication}";
                    record.Diagnosis = string.IsNullOrWhiteSpace(model.Notes) ? "Medication prescribed" : model.Notes;
                    record.Treatment = $"{model.Medication} — {model.Dosage}; {model.Frequency}; {model.Duration}";
                    _db.SaveChanges();
                }

                tx.Commit();

                return Ok(new { message = "Prescription updated successfully." });
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return StatusCode(500, ex.Message);
            }
        }


        // ============================================================
        // PRESCRIPTION — DELETE
        // ============================================================
        [HttpGet]
        public IActionResult DeletePrescription(int id)
        {
            var pres = _db.Prescriptions
                .Include(p => p.Pet).ThenInclude(o => o.Owner)
                .Include(p => p.Staff)
                .FirstOrDefault(p => p.PrescriptionId == id);

            if (pres == null)
                return NotFound();

            return PartialView("_Modal_DeletePrescription", pres);
        }

        [HttpPost]
        public IActionResult DeletePrescriptionConfirmed(int id)
        {
            var pres = _db.Prescriptions.Find(id);

            if (pres != null)
            {
                _db.Prescriptions.Remove(pres);
                _db.SaveChanges();
            }

            return Ok(new { message = "Prescription deleted." });
        }

        // ============================================================
        // DRUG LIST
        // ============================================================
        public IActionResult DrugList()
        {
            var list = _db.Drugs.OrderBy(d => d.DrugName).ToList();
            return PartialView("_Drugs", list);
        }

        [HttpGet]
        public IActionResult CreateDrug()
        {
            return PartialView("_Modal_CreateDrug", new Drug { DateAdded = DateTime.Now });
        }

        [HttpPost]
        public IActionResult CreateDrug(Drug model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            // Ensure DateAdded has a value
            if (!model.DateAdded.HasValue)
                model.DateAdded = DateTime.Now;

            // Minimum stock must never be zero
            if (model.MinimumStock <= 0)
                model.MinimumStock = 10;

            _db.Drugs.Add(model);
            _db.SaveChanges();

            return Ok(new { message = "Drug created successfully." });
        }



        [HttpGet]
        public IActionResult EditDrug(int id)
        {
            var drug = _db.Drugs.Find(id);
            if (drug == null) return NotFound();

            return PartialView("_Modal_EditDrug", drug);
        }

        [HttpPost]
        public IActionResult EditDrug(Drug model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            var drug = _db.Drugs.FirstOrDefault(d => d.DrugId == model.DrugId);
            if (drug == null) return NotFound();

            // Editable fields
            drug.DrugName = model.DrugName;
            drug.Quantity = model.Quantity;
            drug.UnitPrice = model.UnitPrice;
            drug.RestockNotes = model.RestockNotes;

            // Editable minimum stock
            drug.MinimumStock = model.MinimumStock <= 0 ? 10 : model.MinimumStock;

            // Non-editable fields (locked in UI)
            // BasePrice, DosageType, DateAdded, ExpiryDate remain unchanged

            _db.SaveChanges();

            return Ok(new { message = "Drug updated successfully." });
        }


        [HttpGet]
        public IActionResult DeleteDrug(int id)
        {
            var drug = _db.Drugs.Find(id);
            if (drug == null) return NotFound();

            return PartialView("_Modal_DeleteDrug", drug);
        }

        [HttpPost]
        public IActionResult DeleteDrugConfirmed(int id)
        {
            var drug = _db.Drugs.Find(id);
            if (drug != null)
            {
                _db.Drugs.Remove(drug);
                _db.SaveChanges();
            }

            return Ok(new { message = "Drug deleted." });
        }

        // ============================================================
        // VACCINATIONS
        // ============================================================
        public IActionResult VaccinationList()
        {
            var list = _db.Vaccinations
                .Include(v => v.Pet).ThenInclude(o => o.Owner)
                .Include(v => v.Staff)
                .OrderByDescending(v => v.DateGiven)
                .ToList();

            return PartialView("_Vaccinations", list);
        }

        [HttpGet]
        public IActionResult ViewVaccination(int id)
        {
            var vacc = _db.Vaccinations
                .Include(v => v.Pet).ThenInclude(o => o.Owner)
                .Include(v => v.Staff)
                .FirstOrDefault(v => v.VaccinationId == id);

            if (vacc == null)
                return NotFound("Vaccination record not found.");

            return PartialView("_Modal_ViewVaccination", vacc);
        }


        [HttpGet]
        public IActionResult CreateVaccination()
        {
            ViewBag.Pets = _db.Pets.Include(p => p.Owner).ToList();
            ViewBag.Staff = _db.Accounts.Where(a => a.IsAdmin == 1 || a.IsAdmin == 2).ToList();

            ViewBag.VaccinationServices = _db.Service
                .Where(s => s.Category != null && s.Category.ToLower().Contains("vaccination"))
                .OrderBy(s => s.Name)
                .ToList();

            return PartialView("_Modal_CreateVaccination",
                new Vaccination
                {
                    DateGiven = DateTime.Today,
                    NextDueDate = DateTime.Today.AddYears(1)
                });
        }

        [HttpPost]
        public IActionResult CreateVaccination(Vaccination model)
        {
            using var tx = _db.Database.BeginTransaction();
            try
            {
                if (!model.DateGiven.HasValue)
                    model.DateGiven = DateTime.Today;

                if (!model.NextDueDate.HasValue)
                    model.NextDueDate = model.DateGiven.Value.AddYears(1);

                _db.Vaccinations.Add(model);
                _db.SaveChanges();

                var record = new MedicalRecord
                {
                    PetId = model.PetId,
                    StaffId = model.StaffId,
                    VaccinationId = model.VaccinationId,
                    Date = model.DateGiven,
                    Description = $"Vaccination: {model.VaccineName}",
                    Diagnosis = "Preventive vaccination",
                    Treatment = $"{model.VaccineName} administered. Next due: {model.NextDueDate.Value:MMM dd yyyy}"
                };

                _db.MedicalRecords.Add(record);
                _db.SaveChanges();

                tx.Commit();

                return Ok(new { success = true, message = "Vaccination added." });
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public IActionResult EditVaccination(int id)
        {
            var vacc = _db.Vaccinations.FirstOrDefault(v => v.VaccinationId == id);
            if (vacc == null) return NotFound();

            ViewBag.Pets = _db.Pets.Include(p => p.Owner).ToList();
            ViewBag.Staff = _db.Accounts.Where(a => a.IsAdmin == 1 || a.IsAdmin == 2).ToList();
            ViewBag.VaccinationServices = _db.Service
                .Where(s => s.Category.ToLower().Contains("vacc"))
                .OrderBy(s => s.Name)
                .ToList();

            return PartialView("_Modal_EditVaccination", vacc);
        }

        [HttpPost]
        public IActionResult EditVaccination(Vaccination model)
        {
            try
            {
                _db.Vaccinations.Update(model);
                _db.SaveChanges();

                var record = _db.MedicalRecords.FirstOrDefault(r => r.VaccinationId == model.VaccinationId);

                if (record != null)
                {
                    record.PetId = model.PetId;
                    record.StaffId = model.StaffId;
                    record.Date = model.DateGiven ?? record.Date;
                    record.Description = $"Vaccination: {model.VaccineName}";
                    record.Treatment = $"{model.VaccineName} administered. Next due: {model.NextDueDate?.ToString("MMM dd yyyy")}";
                    _db.SaveChanges();
                }

                return Ok(new { success = true, message = "Vaccination updated." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public IActionResult DeleteVaccination(int id)
        {
            var vacc = _db.Vaccinations
                .Include(v => v.Pet).ThenInclude(o => o.Owner)
                .Include(v => v.Staff)
                .FirstOrDefault(v => v.VaccinationId == id);

            if (vacc == null) return NotFound();

            return PartialView("_Modal_DeleteVaccination", vacc);
        }

        [HttpPost]
        public IActionResult DeleteVaccinationConfirmed(int id)
        {
            var vacc = _db.Vaccinations.Find(id);

            if (vacc != null)
            {
                _db.Vaccinations.Remove(vacc);
                _db.SaveChanges();
            }

            return Ok(new { message = "Vaccination deleted." });
        }

        public IActionResult DownloadPdf(string id)
        {
            var items = _db.Billing
                .Include(b => b.Pet).ThenInclude(p => p.Owner)
                .Include(b => b.Staff)
                .Where(b => b.TransactionId == id)
                .ToList();

            if (!items.Any())
                return NotFound("Receipt not found.");

            var document = new BillingReceiptDocument(items);
            var pdf = document.GeneratePdf();   // CLEAN, CORRECT

            return File(pdf, "application/pdf", $"Receipt_{id}.pdf");
        }


    }
}

