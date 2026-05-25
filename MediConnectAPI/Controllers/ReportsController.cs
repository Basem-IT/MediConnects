using MediConnectAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediConnectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Clinic Manager")]
    public class ReportsController : ControllerBase
    {
        private readonly MediConnectDbContext _context;

        // get database access
        public ReportsController(MediConnectDbContext context)
        {
            _context = context;
        }

        // total and status counts for appointments
        [HttpGet("appointment-statistics")]
        public async Task<IActionResult> AppointmentStatistics()
        {
            var total = await _context.Appointments.CountAsync();
            var completed = await _context.Appointments.CountAsync(a => a.Status == "Completed");
            var cancelled = await _context.Appointments.CountAsync(a => a.Status == "Cancelled");
            var missed = await _context.Appointments.CountAsync(a => a.Status == "Missed");

            return Ok(new
            {
                totalAppointments = total,
                completedAppointments = completed,
                cancelledAppointments = cancelled,
                missedAppointments = missed
            });
        }

        // shows you how many appointments each doctor have
        [HttpGet("doctor-utilization")]
        public async Task<IActionResult> DoctorUtilization()
        {
            var data = await _context.Doctors
                .Select(d => new
                {
                    doctorName = d.Name,
                    appointmentCount = d.Appointments.Count()
                })
                .ToListAsync();

            return Ok(data);
        }

        // groups appointments by status, completed etc
        [HttpGet("appointments-by-status")]
        public async Task<IActionResult> AppointmentsByStatus()
        {
            var data = await _context.Appointments
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    status = g.Key,
                    count = g.Count()
                })
                .ToListAsync();

            return Ok(data);
        }

        // last 10 appointment in the system
        [HttpGet("recent-appointments")]
        public async Task<IActionResult> RecentAppointments()
        {
            var data = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .OrderByDescending(a => a.AppointmentDate)
                .Take(10)
                .Select(a => new
                {
                    appointmentId = a.AppointmentID,
                    appointmentDate = a.AppointmentDate,
                    status = a.Status,
                    doctorName = a.Doctor != null ? a.Doctor.Name : "",
                    patientName = a.Patient != null ? a.Patient.Name : ""
                })
                .ToListAsync();

            return Ok(data);
        }

        // missed appointments
        [HttpGet("missed-rate")]
        public async Task<IActionResult> MissedRate()
        {
            var total = await _context.Appointments.CountAsync();
            var missed = await _context.Appointments.CountAsync(a => a.Status == "Missed");

            double rate = total == 0 ? 0 : (double)missed / total * 100;

            return Ok(new
            {
                missedAppointments = missed,
                missedRate = Math.Round(rate, 2)
            });
        }
    }
}