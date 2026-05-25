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

        // database access
        public PrescriptionsController(MediConnectDbContext context)
        {
            _context = context;
        }

        // show all prescriptions with filter
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");

            var prescriptions = _context.Prescriptions
                .Include(p => p.MedicalRecord)
                    .ThenInclude(m => m.Patient)
                .AsQueryable();

            // if it is a patient only show their own prescriptions
            if (role == "Patient")
            {
                prescriptions = prescriptions.Where(p =>
                    p.MedicalRecord != null &&
                    p.MedicalRecord.Patient != null &&
                    p.MedicalRecord.Patient.UserID == userId);
            }

            return View(await prescriptions.ToListAsync());
        }

        // open create prescription page
        public IActionResult Create()
        {
            // dropdown list for medical records
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

        // save the new prescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prescription prescription)
        {
            // ignore navigation property validation issue
            ModelState.Remove("MedicalRecord");

            if (ModelState.IsValid)
            {
                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // reload dropdown if validation fails
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

        // open edit page
        public async Task<IActionResult> Edit(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);

            if (prescription == null)
                return NotFound();

            // dropdown for editing record selection
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

        // update prescription data
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

            // reload dropdown if something goes wrong
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

        // confirm delete page
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

        // actually delete prescription
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