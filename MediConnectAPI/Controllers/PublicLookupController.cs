using MediConnectAPI.Data;
using MediConnectAPI.DTOs;
using MediConnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediConnectAPI.Controllers
{
    [ApiController]
    [Route("api/public")]
    [AllowAnonymous] // Entire controller is public, no token required
    public class PublicLookupController : ControllerBase
    {
        private readonly MediConnectDbContext _context;

        public PublicLookupController(MediConnectDbContext context)
        {
            _context = context;
        }

        // POST /api/public/lookup
        [HttpPost("lookup")]
        public async Task<ActionResult<PublicLookupResponseDto>> Lookup(
            PublicLookupRequestDto dto)
        {
            // Both the CPR and ReferenceCode must match the same patient record
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.CPR == dto.CPR &&
                    p.ReferenceCode == dto.ReferenceCode);

            // 404 avoids leaking whether the CPR exists
            if (patient == null)
                return NotFound(new
                {
                    message = "No record found. Please check your CPR number and reference code."
                });

            var today = DateTime.UtcNow.Date;

            // Upcoming: today or future, doesn't want cancelled or missed appointments
            var upcoming = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Schedule)
                .Where(a =>
                    a.PatientID == patient.PatientID &&
                    a.AppointmentDate.Date >= today &&
                    a.Status != "Cancelled" &&
                    a.Status != "Missed")
                .OrderBy(a => a.AppointmentDate)
                .Take(5) // Show next 5 only
                .ToListAsync();

            // Recent visits: completed appointments, filtered by most recent first
            var recentVisits = await _context.MedicalRecords
                .Include(m => m.Doctor)
                .Include(m => m.Appointment)
                .Where(m => m.PatientID == patient.PatientID)
                .OrderByDescending(m => m.CreatedDate)
                .Take(5) // Show last 5 only
                .ToListAsync();

            return Ok(new PublicLookupResponseDto
            {
                // First name only — full name made redundant and reduces exposure
                PatientFirstName = patient.Name.Split(' ').First(),

                UpcomingAppointments = upcoming.Select(a => new PublicAppointmentDto
                {
                    AppointmentID = a.AppointmentID,
                    AppointmentDate = a.AppointmentDate,
                    StartTime = a.Schedule?.StartTime.ToString("HH:mm") ?? string.Empty,
                    DoctorName = a.Doctor?.Name ?? string.Empty,
                    Status = a.Status
                }).ToList(),

                RecentVisits = recentVisits.Select(m => new PublicVisitSummaryDto
                {
                    VisitDate = m.CreatedDate,
                    DoctorName = m.Doctor?.Name ?? string.Empty,
                    // Diagnosis is shown; DoctorNotes are not because too sensitive
                    Diagnosis = m.Diagnosis
                }).ToList()
            });
        }

        // GET /api/public/lookup
        [HttpGet("lookup")]
        public async Task<ActionResult<PublicLookupResponseDto>> LookupGet(
            [FromQuery] int cpr,
            [FromQuery] string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return BadRequest(new { message = "Both cpr and reference are required." });

            return await Lookup(new PublicLookupRequestDto
            {
                CPR = cpr,
                ReferenceCode = reference
            });
        }

        // GET /api/public/specializations
        [HttpGet("specializations")]
        public async Task<IActionResult> GetSpecializations()
        {
            var specializations = await _context.Specializations
                .Select(s => new { s.SpecializationID, s.Name, s.Description })
                .ToListAsync();

            return Ok(specializations);
        }

        // GET /api/public/doctors
        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors([FromQuery] int? specializationID)
        {
            var query = _context.Doctors
                .Include(d => d.DoctorSpecializations)
                    .ThenInclude(ds => ds.Specialization)
                .Include(d => d.Schedules)
                .AsQueryable();

            if (specializationID.HasValue)
                query = query.Where(d =>
                    d.DoctorSpecializations.Any(ds =>
                        ds.SpecializationID == specializationID));

            var doctors = await query.Select(d => new
            {
                d.DoctorID,
                d.Name,
                d.Qualification,
                Specializations = d.DoctorSpecializations
                                   .Select(ds => ds.Specialization!.Name),
                AvailableSlots = d.Schedules
                                   .Where(s => s.IsAvailable)
                                   .Select(s => new
                                   {
                                       s.ScheduleID,
                                       s.DayOfWeek,
                                       StartTime = s.StartTime.ToString("HH:mm"),
                                       s.EndTime
                                   })
            }).ToListAsync();

            return Ok(doctors);
        }
    }
}