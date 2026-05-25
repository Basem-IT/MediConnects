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

        // database connection
        public SchedulesController(MediConnectDbContext context)
        {
            _context = context;
        }

        // show all the schedules
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Doctor)
                .ToListAsync();

            return View(schedules);
        }

        // open create page
        public IActionResult Create()
        {
            // load doctors for dropdown list
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name");
            return View();
        }

        // save new schedule
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

            // if validation fails, reload dropdown
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", schedule.DoctorID);
            return View(schedule);
        }

        // open the edit page
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null)
                return NotFound();

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", schedule.DoctorID);
            return View(schedule);
        }

        // update schedule data
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

        // show delete confirmation page
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.ScheduleID == id);

            if (schedule == null)
                return NotFound();

            return View(schedule);
        }

        // actually delete schedule and related data
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule != null)
            {
                // get all appointments linked to this schedule
                var appointments = _context.Appointments
                    .Where(a => a.ScheduleID == id)
                    .ToList();

                foreach (var appointment in appointments)
                {
                    // remove notifications related to appointment
                    var notifications = _context.Notifications
                        .Where(n => n.AppointmentID == appointment.AppointmentID);

                    _context.Notifications.RemoveRange(notifications);

                    // get medical records linked to appointment
                    var medicalRecords = _context.MedicalRecords
                        .Where(m => m.AppointmentID == appointment.AppointmentID)
                        .ToList();

                    foreach (var record in medicalRecords)
                    {
                        // remove prescriptions inside medical records
                        var prescriptions = _context.Prescriptions
                            .Where(p => p.RecordID == record.RecordID);

                        _context.Prescriptions.RemoveRange(prescriptions);
                    }

                    _context.MedicalRecords.RemoveRange(medicalRecords);
                }

                // delete appointments then schedule
                _context.Appointments.RemoveRange(appointments);
                _context.Schedules.Remove(schedule);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}