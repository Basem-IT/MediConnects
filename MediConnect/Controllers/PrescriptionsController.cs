using MediConnectAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using MediConnectMVC.Filters;

namespace MediConnectMVC.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize("Clinic Manager", "Doctor", "Patient")]
    public class PrescriptionsController : Controller
    {
        private readonly MediConnectDbContext _context;

        public PrescriptionsController(MediConnectDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");

            var prescriptions = _context.Prescriptions
                .Include(p => p.MedicalRecord)
                    .ThenInclude(m => m.Patient)
                .AsQueryable();

            if (role == "Patient")
            {
                prescriptions = prescriptions.Where(p =>
                    p.MedicalRecord != null &&
                    p.MedicalRecord.Patient != null &&
                    p.MedicalRecord.Patient.UserID == userId);
            }

            return View(await prescriptions.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewBag.RecordID = new SelectList(
                _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new
                    {
                        m.RecordID,
                        Display = m.Patient.Name + " - Record " + m.RecordID
                    }),
                "RecordID",
                "Display"
            );

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prescription prescription)
        {
            ModelState.Remove("MedicalRecord");

            if (ModelState.IsValid)
            {
                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.RecordID = new SelectList(
                _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new
                    {
                        m.RecordID,
                        Display = m.Patient.Name + " - Record " + m.RecordID
                    }),
                "RecordID",
                "Display",
                prescription.RecordID
            );

            return View(prescription);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);

            if (prescription == null)
                return NotFound();

            ViewBag.RecordID = new SelectList(
                _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new
                    {
                        m.RecordID,
                        Display = m.Patient.Name + " - Record " + m.RecordID
                    }),
                "RecordID",
                "Display",
                prescription.RecordID
            );

            return View(prescription);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Prescription prescription)
        {
            ModelState.Remove("MedicalRecord");

            if (ModelState.IsValid)
            {
                _context.Prescriptions.Update(prescription);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.RecordID = new SelectList(
                _context.MedicalRecords
                    .Include(m => m.Patient)
                    .Select(m => new
                    {
                        m.RecordID,
                        Display = m.Patient.Name + " - Record " + m.RecordID
                    }),
                "RecordID",
                "Display",
                prescription.RecordID
            );

            return View(prescription);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.MedicalRecord)
                    .ThenInclude(m => m.Patient)
                .FirstOrDefaultAsync(p => p.PrescriptionID == id);

            if (prescription == null)
                return NotFound();

            return View(prescription);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);

            if (prescription != null)
            {
                _context.Prescriptions.Remove(prescription);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}