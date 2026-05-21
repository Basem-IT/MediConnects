using MediConnectAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using MediConnectMVC.Filters;

namespace MediConnectMVC.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize("Clinic Manager", "Receptionist", "Doctor")]
    public class SchedulesController : Controller
    {
        private readonly MediConnectDbContext _context;

        public SchedulesController(MediConnectDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Doctor)
                .ToListAsync();

            return View(schedules);
        }
        public IActionResult Create()
        {
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Schedule schedule)
        {
            if (ModelState.IsValid)
            {
                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", schedule.DoctorID);
            return View(schedule);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null)
                return NotFound();

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", schedule.DoctorID);
            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Schedule schedule)
        {
            if (ModelState.IsValid)
            {
                _context.Schedules.Update(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", schedule.DoctorID);
            return View(schedule);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.ScheduleID == id);

            if (schedule == null)
                return NotFound();

            return View(schedule);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule != null)
            {
                var appointments = _context.Appointments
                    .Where(a => a.ScheduleID == id)
                    .ToList();

                foreach (var appointment in appointments)
                {
                    var notifications = _context.Notifications
                        .Where(n => n.AppointmentID == appointment.AppointmentID);

                    _context.Notifications.RemoveRange(notifications);

                    var medicalRecords = _context.MedicalRecords
                        .Where(m => m.AppointmentID == appointment.AppointmentID)
                        .ToList();

                    foreach (var record in medicalRecords)
                    {
                        var prescriptions = _context.Prescriptions
                            .Where(p => p.RecordID == record.RecordID);

                        _context.Prescriptions.RemoveRange(prescriptions);
                    }

                    _context.MedicalRecords.RemoveRange(medicalRecords);
                }

                _context.Appointments.RemoveRange(appointments);
                _context.Schedules.Remove(schedule);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}