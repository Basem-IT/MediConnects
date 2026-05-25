using MediConnectAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using MediConnectMVC.Hubs;
using Microsoft.AspNetCore.SignalR;
using MediConnectMVC.Filters;

namespace MediConnectMVC.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize("Clinic Manager", "Receptionist", "Doctor", "Patient")]
    public class AppointmentsController : Controller
    {
        private readonly MediConnectDbContext _context;
        private readonly IHubContext<AppointmentHub> _hubContext;

        // setup database and signalR hub
        public AppointmentsController(MediConnectDbContext context, IHubContext<AppointmentHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // show all appointments
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Schedule)
                .ToListAsync();

            // load patient manually for each appointment 
            foreach (var a in appointments)
            {
                a.Patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.PatientID == a.PatientID);
            }

            // if it is  patient only show their own appointments
            if (role == "Patient")
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserID == userId);

                if (patient != null)
                    appointments = appointments.Where(a => a.PatientID == patient.PatientID).ToList();
                else
                    appointments = new List<Appointment>();
            }

            return View(appointments);
        }

        // open create appointment page
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");

            ViewBag.Role = role;
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name");

            // if patient, auto fill their info
            if (role == "Patient")
            {
                var patient = _context.Patients.FirstOrDefault(p => p.UserID == userId);
                ViewBag.PatientName = patient?.Name ?? HttpContext.Session.GetString("UserName");
                ViewBag.PatientID = patient?.PatientID;
            }
            else
            {
                ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name");
            }

            return View();
        }

        // create appointment logic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");
            var userName = HttpContext.Session.GetString("UserName");

            // if patient is booking, auto assign patient id
            if (role == "Patient")
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == userId);

                // if thepatient not found, create one
                if (patient == null)
                {
                    patient = new Patient
                    {
                        Name = userName ?? "Patient",
                        Email = "",
                        CPR = 0,
                        Phone = 0,
                        DOB = DateTime.Today,
                        UserID = userId
                    };

                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                }

                appointment.PatientID = patient.PatientID;
                appointment.Status = "Requested";
            }

            // check doctor schedule based on day of appointment
            var dayOfWeek = appointment.AppointmentDate.DayOfWeek.ToString();

            var matchingSchedule = await _context.Schedules.FirstOrDefaultAsync(s =>
                s.DoctorID == appointment.DoctorID &&
                s.DayOfWeek == dayOfWeek &&
                s.IsAvailable);

            if (matchingSchedule == null)
            {
                ModelState.AddModelError("", $"Doctor is not available on {dayOfWeek}");
            }
            else
            {
                appointment.ScheduleID = matchingSchedule.ScheduleID;
            }

            // ignore some fields for validation issues
            ModelState.Remove("PatientID");
            ModelState.Remove("Status");
            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");
            ModelState.Remove("Schedule");
            ModelState.Remove("ScheduleID");

            // check status flow (can't jump steps randomly)
            var existing = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AppointmentID == appointment.AppointmentID);

            if (existing != null)
            {
                var validTransitions = new Dictionary<string, List<string>>
                {
                    { "Requested", new() { "Confirmed", "Cancelled" } },
                    { "Confirmed", new() { "Checked-In", "Cancelled" } },
                    { "Checked-In", new() { "In Progress", "Cancelled" } },
                    { "In Progress", new() { "Completed", "Missed" } },
                    { "Completed", new() },
                    { "Cancelled", new() },
                    { "Missed", new() }
                };

                if (existing.Status != appointment.Status &&
                    !validTransitions[existing.Status].Contains(appointment.Status))
                {
                    TempData["StatusError"] =
                        $"Invalid status change from {existing.Status} to {appointment.Status}";

                    return RedirectToAction("Edit", new { id = appointment.AppointmentID });
                }
            }

            // prevent multiple bookings for patient
            if (role == "Patient")
            {
                bool alreadyBooked = await _context.Appointments
                    .AnyAsync(a => a.PatientID == appointment.PatientID);

                if (alreadyBooked)
                    ModelState.AddModelError("", "You already have an appointment.");
            }

            if (ModelState.IsValid)
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // send update to all clients using signalR
                await _hubContext.Clients.All.SendAsync("AppointmentStatusUpdated", new
                {
                    appointmentId = appointment.AppointmentID,
                    status = appointment.Status
                });

                return RedirectToAction(nameof(Index));
            }

            return View(appointment);
        }

        // edit appointment page
        public async Task<IActionResult> Edit(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role == "Patient")
                return RedirectToAction(nameof(Index));

            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
                return NotFound();

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", appointment.DoctorID);
            ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name", appointment.PatientID);

            ViewBag.StatusOptions = new SelectList(new[]
            {
                "Requested", "Confirmed", "Checked-In", "In Progress", "Completed", "Cancelled", "Missed"
            }, appointment.Status);

            return View(appointment);
        }

        // update appointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Appointment appointment)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role == "Patient")
                return RedirectToAction(nameof(Index));

            var dayOfWeek = appointment.AppointmentDate.DayOfWeek.ToString();

            var schedule = await _context.Schedules.FirstOrDefaultAsync(s =>
                s.DoctorID == appointment.DoctorID &&
                s.DayOfWeek == dayOfWeek &&
                s.IsAvailable);

            if (schedule != null)
                appointment.ScheduleID = schedule.ScheduleID;

            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");
            ModelState.Remove("Schedule");
            ModelState.Remove("ScheduleID");

            if (ModelState.IsValid)
            {
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();

                // create notification for update
                var notification = new Notification
                {
                    AppointmentID = appointment.AppointmentID,
                    Type = "Appointment Update",
                    Message = $"Status changed to {appointment.Status}",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
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

        // delete confirmation page
        public async Task<IActionResult> Delete(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role == "Patient")
                return RedirectToAction(nameof(Index));

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            if (appointment == null)
                return NotFound();

            return View(appointment);
        }

        // delete appointment and cleanup related data
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role == "Patient")
                return RedirectToAction(nameof(Index));

            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment != null)
            {
                // remove notifications first
                var notifications = _context.Notifications.Where(n => n.AppointmentID == id);
                _context.Notifications.RemoveRange(notifications);

                // remove medical records andprescriptions
                var medicalRecords = _context.MedicalRecords
                    .Where(m => m.AppointmentID == id)
                    .ToList();

                foreach (var record in medicalRecords)
                {
                    var prescriptions = _context.Prescriptions
                        .Where(p => p.RecordID == record.RecordID);

                    _context.Prescriptions.RemoveRange(prescriptions);
                }

                _context.MedicalRecords.RemoveRange(medicalRecords);

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}