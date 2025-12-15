using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;

namespace PetClinicSystem.Controllers
{
    public class VaccinationsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public VaccinationsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ========== LIST FOR TAB ==========
        // GET: /Vaccinations/List
        public IActionResult List()
        {
            var list = _db.Vaccinations
                .Include(v => v.Pet).ThenInclude(p => p.Owner)
                .Include(v => v.Staff)
                .ToList();

            return PartialView("_Vaccinations", list);  // _Vaccinations.cshtml: @model List<Vaccination>
        }

        // ========== CREATE ==========
        public IActionResult Create()
        {
            ViewBag.Pets = _db.Pets.Include(p => p.Owner).ToList();
            ViewBag.Staff = _db.Accounts.Where(a => a.IsAdmin == 1 || a.IsAdmin == 2).ToList();
            return PartialView("_Modal_CreateVaccination", new Vaccination());
        }

        [HttpPost]
        public IActionResult Create(Vaccination model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid form data.");

            _db.Vaccinations.Add(model);
            _db.SaveChanges();

            return Ok();
        }

        // ========== EDIT ==========
        public IActionResult Edit(int id)
        {
            var vax = _db.Vaccinations.Find(id);
            if (vax == null) return NotFound();

            ViewBag.Pets = _db.Pets.Include(p => p.Owner).ToList();
            ViewBag.Staff = _db.Accounts.Where(a => a.IsAdmin == 1 || a.IsAdmin == 2).ToList();

            return PartialView("_Modal_EditVaccination", vax);
        }

        [HttpPost]
        public IActionResult Edit(Vaccination model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid form data.");

            _db.Vaccinations.Update(model);
            _db.SaveChanges();

            return Ok();
        }

        // ========== VIEW ==========
        public IActionResult Details(int id)
        {
            var vax = _db.Vaccinations
                .Include(v => v.Pet).ThenInclude(p => p.Owner)
                .Include(v => v.Staff)
                .FirstOrDefault(v => v.VaccinationId == id);

            if (vax == null) return NotFound();
            return PartialView("_Modal_ViewVaccination", vax);
        }

        // ========== DELETE ==========
        public IActionResult Delete(int id)
        {
            var vax = _db.Vaccinations
                .Include(v => v.Pet)
                .Include(v => v.Staff)
                .FirstOrDefault(v => v.VaccinationId == id);

            if (vax == null) return NotFound();
            return PartialView("_Modal_DeleteVaccination", vax);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var vax = _db.Vaccinations.Find(id);
            if (vax != null)
            {
                _db.Vaccinations.Remove(vax);
                _db.SaveChanges();
            }

            return Ok();
        }
    }
}
