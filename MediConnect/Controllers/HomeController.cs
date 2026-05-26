using System.Diagnostics;
using MediConnect.Models;
using MediConnectAPI.Data;
using MediConnectAPI.Models;
using MediConnectMVC.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediConnectMVC.Controllers
{
    [SessionAuthorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MediConnectDbContext _context;

        public HomeController(ILogger<HomeController> logger, MediConnectDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // get current user info from session
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");
            var userName = HttpContext.Session.GetString("UserName");

            ViewBag.Role = role;
            ViewBag.UserName = userName;
            ViewBag.UnreadNotifications = _context.Notifications.Count(n => !n.IsRead && n.UserID == userId);

            if (role == "Doctor")
            {
                // find the doctor record linked to this user
                var doctorRecord = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserID == userId);

                if (doctorRecord != null)
                {
                    var today = DateTime.Today;

                    // get upcoming appointments for this doctor
                    var upcoming = await _context.Appointments
                        .Include(a => a.Patient)
                        .Where(a => a.DoctorID == doctorRecord.DoctorID
                                 && a.AppointmentDate >= today
                                 && a.Status != "Cancelled"
                                 && a.Status != "Completed")
                        .OrderBy(a => a.AppointmentDate)
                        .Take(5)
                        .ToListAsync();

                    // load patient manually to avoid EF join issues
                    foreach (var a in upcoming)
                        a.Patient = await _context.Patients
                            .FirstOrDefaultAsync(p => p.PatientID == a.PatientID);

                    ViewBag.UpcomingAppointments = upcoming;
                    ViewBag.TotalToday = await _context.Appointments
                        .CountAsync(a => a.DoctorID == doctorRecord.DoctorID
                                      && a.AppointmentDate.Date == today);
                    ViewBag.TotalCompleted = await _context.Appointments
                        .CountAsync(a => a.DoctorID == doctorRecord.DoctorID
                                      && a.Status == "Completed");
                }
            }
            else if (role == "Patient")
            {
                // get patient record linked to this user
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserID == userId);

                if (patient != null)
                {
                    var upcoming = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Where(a => a.PatientID == patient.PatientID
                                 && a.AppointmentDate >= DateTime.Today
                                 && a.Status != "Cancelled")
                        .OrderBy(a => a.AppointmentDate)
                        .Take(3)
                        .ToListAsync();

                    ViewBag.UpcomingAppointments = upcoming;
                    ViewBag.PatientName = patient.Name;
                }
            }
            else if (role == "Clinic Manager")
            {
                // stats for clinic manager dashboard
                ViewBag.TotalDoctors = await _context.Doctors.CountAsync();
                ViewBag.TotalPatients = await _context.Patients.CountAsync();
                ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
                ViewBag.PendingAppointments = await _context.Appointments
                    .CountAsync(a => a.Status == "Requested");
                ViewBag.TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate.Date == DateTime.Today);
            }
            else if (role == "Receptionist")
            {
                // show today's appointments for receptionist
                ViewBag.TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate.Date == DateTime.Today);
                ViewBag.PendingAppointments = await _context.Appointments
                    .CountAsync(a => a.Status == "Requested");

                var upcoming = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Schedule)
                    .Where(a => a.AppointmentDate.Date == DateTime.Today)
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync();

                foreach (var a in upcoming)
                    a.Patient = await _context.Patients
                        .FirstOrDefaultAsync(p => p.PatientID == a.PatientID);

                ViewBag.TodayList = upcoming;
            }

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}