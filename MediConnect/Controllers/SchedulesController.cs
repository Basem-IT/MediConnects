using MediConnectAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using MediConnectMVC.Filters;

namespace MediConnectMVC.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize("ClinicManager", "Receptionist", "Doctor")]
    public class SchedulesController : Controller
    {
        private readonly MediConnectDbContext _context;

        // database connection
        public SchedulesController(MediConnectDbContext context)
        {
            _context = context;
        }

        // show schedules - doctors only see their own
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");

            var schedules = _context.Schedules.Include(s => s.Doctor).AsQueryable();

            // filter by doctor if logged in as doctor
            if (role == "Doctor")
            {
                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserID == userId);
                if (doctor != null)
                    schedules = schedules.Where(s => s.DoctorID == doctor.DoctorID);
            }

            return View(await schedules.ToListAsync());
        }

        // open create page
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");

            // only clinic manager picks the doctor
            if (role == "ClinicManager")
                ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name");

            // dropdown for days instead of free text
            ViewBag.Days = new SelectList(new[]
            {
                "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
            });

            return View();
        }

        // save new schedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Schedule schedule)
        {
            // remove nav properties so validation doesnt fail
            ModelState.Remove("Doctor");
            ModelState.Remove("Appointments");

            if (ModelState.IsValid)
            {
                _context.Schedules.Add(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // reload dropdowns if validation fails
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", schedule.DoctorID);
            ViewBag.Days = new SelectList(new[]
            {
                "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
            });
            return View(schedule);
        }

        // open edit page
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null)
                return NotFound();

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", schedule.DoctorID);

            // pass current day so dropdown shows correct selected value
            ViewBag.Days = new SelectList(new[]
            {
                "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
            }, schedule.DayOfWeek);

            return View(schedule);
        }

        // save edited schedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Schedule schedule)
        {
            ModelState.Remove("Doctor");
            ModelState.Remove("Appointments");

            if (ModelState.IsValid)
            {
                _context.Schedules.Update(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", schedule.DoctorID);
            ViewBag.Days = new SelectList(new[]
            {
                "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
            }, schedule.DayOfWeek);
            return View(schedule);
        }

        // lets doctors toggle their availability on/off
        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                // flip the available flag
                schedule.IsAvailable = !schedule.IsAvailable;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
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

        // actually delete the schedule and all linked data
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
                    // remove notifications for this appointment
                    var notifications = _context.Notifications
                        .Where(n => n.AppointmentID == appointment.AppointmentID);
                    _context.Notifications.RemoveRange(notifications);

                    // get medical records linked to this appointment
                    var medicalRecords = _context.MedicalRecords
                        .Where(m => m.AppointmentID == appointment.AppointmentID)
                        .ToList();

                    foreach (var record in medicalRecords)
                    {
                        // remove prescriptions inside each record
                        var prescriptions = _context.Prescriptions
                            .Where(p => p.RecordID == record.RecordID);
                        _context.Prescriptions.RemoveRange(prescriptions);
                    }

                    _context.MedicalRecords.RemoveRange(medicalRecords);
                }

                // delete appointments then the schedule itself
                _context.Appointments.RemoveRange(appointments);
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}