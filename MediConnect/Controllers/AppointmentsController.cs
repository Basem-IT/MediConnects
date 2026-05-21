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

        public AppointmentsController(MediConnectDbContext context, IHubContext<AppointmentHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");

            var appointments = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .AsQueryable();

            if (role == "Patient")
            {
                appointments = appointments.Where(a => a.Patient != null && a.Patient.UserID == userId);
            }

            return View(await appointments.ToListAsync());
        }

        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");

            ViewBag.Role = role;

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name");

            ViewBag.ScheduleID = new SelectList(
                _context.Schedules
                    .GroupBy(s => s.DayOfWeek)
                    .Select(g => g.First()),
                "ScheduleID",
                "DayOfWeek"
            );

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");
            var userName = HttpContext.Session.GetString("UserName");

            if (role == "Patient")
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserID == userId);

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

            ModelState.Remove("PatientID");
            ModelState.Remove("Status");
            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");
            ModelState.Remove("Schedule");


            if (role == "Patient")
            {
                bool alreadyHasAppointment = await _context.Appointments
                    .AnyAsync(a => a.PatientID == appointment.PatientID);

                if (alreadyHasAppointment)
                {
                    ModelState.AddModelError("", "You already have an appointment booked.");
                }
            }
            if (ModelState.IsValid)
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("AppointmentStatusUpdated", new
                {
                    appointmentId = appointment.AppointmentID,
                    status = appointment.Status
                });

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Role = role;

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", appointment.DoctorID);

            ViewBag.ScheduleID = new SelectList(
                _context.Schedules
                    .GroupBy(s => s.DayOfWeek)
                    .Select(g => g.First()),
                "ScheduleID",
                "DayOfWeek",
                appointment.ScheduleID
            );

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

            ViewBag.ScheduleID = new SelectList(
                _context.Schedules
                    .GroupBy(s => s.DayOfWeek)
                    .Select(g => g.First()),
                "ScheduleID",
                "DayOfWeek",
                appointment.ScheduleID
            );

            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Appointment appointment)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role == "Patient")
                return RedirectToAction(nameof(Index));

            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");
            ModelState.Remove("Schedule");

            if (ModelState.IsValid)
            {
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();

                var notification = new MediConnectAPI.Models.Notification
                {
                    AppointmentID = appointment.AppointmentID,
                    Type = "Appointment Update",
                    Message = $"Your appointment status changed to {appointment.Status}.",
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

            ViewBag.DoctorID = new SelectList(_context.Doctors, "DoctorID", "Name", appointment.DoctorID);

            ViewBag.PatientID = new SelectList(_context.Patients, "PatientID", "Name", appointment.PatientID);

            ViewBag.ScheduleID = new SelectList(
                _context.Schedules
                    .GroupBy(s => s.DayOfWeek)
                    .Select(g => g.First()),
                "ScheduleID",
                "DayOfWeek",
                appointment.ScheduleID
            );

            return View(appointment);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role == "Patient")
                return RedirectToAction(nameof(Index));

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            if (appointment == null)
                return NotFound();

            return View(appointment);
        }

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
                var notifications = _context.Notifications
                    .Where(n => n.AppointmentID == id);

                _context.Notifications.RemoveRange(notifications);

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