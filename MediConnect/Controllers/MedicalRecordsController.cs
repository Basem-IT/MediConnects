using MediConnectAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using MediConnectMVC.Filters;

namespace MediConnectMVC.Controllers
{
    [SessionAuthorize]
    public class MedicalRecordsController : Controller
    {
        private readonly MediConnectDbContext _context;

        public MedicalRecordsController(MediConnectDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var records = await _context.MedicalRecords
                .Include(m => m.Appointment)
                .Include(m => m.Doctor)
                .Include(m => m.Patient)
                .ToListAsync();

            return View(records);
        }
        public IActionResult Create()
        {
            ViewBag.AppointmentID = new SelectList(_context.Appointments, "AppointmentID", "AppointmentID");
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name");
            ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalRecord record)
        {
            if (ModelState.IsValid)
            {
                _context.MedicalRecords.Add(record);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AppointmentID = new SelectList(_context.Appointments, "AppointmentID", "AppointmentID", record.AppointmentID);
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", record.DoctorID);
            ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name", record.PatientID);

            return View(record);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var record = await _context.MedicalRecords.FindAsync(id);

            if (record == null)
                return NotFound();

            ViewBag.AppointmentID = new SelectList(_context.Appointments, "AppointmentID", "AppointmentID", record.AppointmentID);
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", record.DoctorID);
            ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name", record.PatientID);

            return View(record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MedicalRecord record)
        {
            if (ModelState.IsValid)
            {
                _context.MedicalRecords.Update(record);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AppointmentID = new SelectList(_context.Appointments, "AppointmentID", "AppointmentID", record.AppointmentID);
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", record.DoctorID);
            ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name", record.PatientID);

            return View(record);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.MedicalRecords
                .Include(m => m.Doctor)
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.RecordID == id);

            if (record == null)
                return NotFound();

            return View(record);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var record = await _context.MedicalRecords.FindAsync(id);

            if (record != null)
            {
                _context.MedicalRecords.Remove(record);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}