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
    [RoleAuthorize("ClinicManager", "Receptionist", "Doctor", "Patient")]
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

            // if patient only show their own appointments
            if (role == "Patient")
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserID == userId);

                if (patient != null)
                    appointments = appointments.Where(a => a.PatientID == patient.PatientID).ToList();
                else
                    appointments = new List<Appointment>();
            }

            // doctors only see their own appointments
            if (role == "Doctor")
            {
                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserID == userId);

                if (doctor != null)
                    appointments = appointments.Where(a => a.DoctorID == doctor.DoctorID).ToList();
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

            // if patient auto fill their info
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

            // if patient is booking auto assign patient id
            if (role == "Patient")
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == userId);

                // if patient record not found create one
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

            // ignore nav properties so validation doesnt fail
            ModelState.Remove("PatientID");
            ModelState.Remove("Status");
            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");
            ModelState.Remove("Schedule");
            ModelState.Remove("ScheduleID");

            // prevent multiple bookings for same patient
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

                // notify the doctor about the new appointment
                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.DoctorID == appointment.DoctorID);

                if (doctor?.UserID != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        AppointmentID = appointment.AppointmentID,
                        Type = "New Appointment",
                        Message = $"A new appointment has been booked with you on {appointment.AppointmentDate:MMM dd, yyyy}.",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        UserID = doctor.UserID
                    });
                    await _context.SaveChangesAsync();
                }

                // send live update via signalR
                await _hubContext.Clients.All.SendAsync("AppointmentStatusUpdated", new
                {
                    appointmentId = appointment.AppointmentID,
                    status = appointment.Status
                });

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Role = role;
            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", appointment.DoctorID);

            if (role == "Patient")
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == userId);
                ViewBag.PatientName = patient?.Name ?? userName;
                ViewBag.PatientID = patient?.PatientID;
            }
            else
            {
                ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name", appointment.PatientID);
            }

            return View(appointment);
        }

        // open edit appointment page
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

        // save appointment changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Appointment appointment)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role == "Patient")
                return RedirectToAction(nameof(Index));

            // check status transition is valid
            var existing = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AppointmentID == appointment.AppointmentID);

            if (existing != null && existing.Status != appointment.Status)
            {
                var validTransitions = new Dictionary<string, List<string>>
                {
                    { "Requested",   new() { "Confirmed", "Cancelled" } },
                    { "Confirmed",   new() { "Checked-In", "Cancelled" } },
                    { "Checked-In",  new() { "In Progress", "Cancelled" } },
                    { "In Progress", new() { "Completed", "Missed" } },
                    { "Completed",   new() },
                    { "Cancelled",   new() },
                    { "Missed",      new() }
                };

                if (!validTransitions[existing.Status].Contains(appointment.Status))
                {
                    TempData["StatusError"] = $"Cannot change status from '{existing.Status}' to '{appointment.Status}'. " +
                        $"Allowed: {string.Join(", ", validTransitions[existing.Status])}";
                    return RedirectToAction("Edit", new { id = appointment.AppointmentID });
                }
            }

            // auto assign schedule based on appointment date
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

                // notify the patient about status change
                var appointmentPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.PatientID == appointment.PatientID);

                if (appointmentPatient?.UserID != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        AppointmentID = appointment.AppointmentID,
                        Type = "Appointment Update",
                        Message = $"Your appointment status has been changed to '{appointment.Status}'.",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        UserID = appointmentPatient.UserID
                    });
                }

                // also notify the doctor
                var appointmentDoctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.DoctorID == appointment.DoctorID);

                if (appointmentDoctor?.UserID != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        AppointmentID = appointment.AppointmentID,
                        Type = "Appointment Update",
                        Message = $"Appointment status updated to '{appointment.Status}'.",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        UserID = appointmentDoctor.UserID
                    });
                }

                await _context.SaveChangesAsync();

                // send live update via signalR
                await _hubContext.Clients.All.SendAsync("AppointmentStatusUpdated", new
                {
                    appointmentId = appointment.AppointmentID,
                    status = appointment.Status
                });

                return RedirectToAction(nameof(Index));
            }

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", appointment.DoctorID);
            ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name", appointment.PatientID);
            ViewBag.StatusOptions = new SelectList(new[]
            {
                "Requested", "Confirmed", "Checked-In", "In Progress", "Completed", "Cancelled", "Missed"
            }, appointment.Status);

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

        // actually delete the appointment and all linked data
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

                // remove medical records and prescriptions
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