using MediConnectAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using MediConnectMVC.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MediConnectMVC.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly MediConnectDbContext _context;
        private readonly IHubContext<AppointmentHub> _hubContext;

        public AppointmentsController( MediConnectDbContext context,IHubContext<AppointmentHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .ToListAsync();

            return View(appointments);
        }
        public IActionResult Create()
        {
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name");
            ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name");
            ViewBag.ScheduleID = new SelectList(_context.Schedules, "ScheduleID", "DayOfWeek");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", appointment.DoctorID);
            ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name", appointment.PatientID);
            ViewBag.ScheduleID = new SelectList(_context.Schedules, "ScheduleID", "DayOfWeek", appointment.ScheduleID);

            return View(appointment);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                return NotFound();

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", appointment.DoctorID);
            ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name", appointment.PatientID);
            ViewBag.ScheduleID = new SelectList(_context.Schedules, "ScheduleID", "DayOfWeek", appointment.ScheduleID);

            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("AppointmentStatusUpdated", new
                {
                    appointmentId = appointment.AppointmentID,
                    status = appointment.Status
                });

                return RedirectToAction(nameof(Index));
            }

            return View(appointment);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            if (appointment == null)
                return NotFound();

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}