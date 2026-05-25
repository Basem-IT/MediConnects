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
    [AllowAnonymous]
    public class PublicLookupController : ControllerBase
    {
        // database context variable
        private readonly MediConnectDbContext _context;

        // constructor for initializing db context
        public PublicLookupController(MediConnectDbContext context)
        {
            _context = context;
        }

        // endpoint used for public patient lookup
        [HttpPost("lookup")]
        public async Task<ActionResult<PublicLookupResponseDto>> Lookup(
            PublicLookupRequestDto dto)
        {
            // checking if cpr and reference code belong to same patient
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p =>
                    p.CPR == dto.CPR &&
                    p.ReferenceCode == dto.ReferenceCode);

            // return error if patient was not found
            if (patient == null)
                return NotFound(new
                {
                    message = "No record found. Please check your CPR number and reference code."
                });

            // getting today's date
            var today = DateTime.UtcNow.Date;

            // loading upcoming appointments
            var upcoming = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Schedule)
                .Where(a =>
                    a.PatientID == patient.PatientID &&
                    a.AppointmentDate.Date >= today &&
                    a.Status != "Cancelled" &&
                    a.Status != "Missed")
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .ToListAsync();

            // loading latest medical visits
            var recentVisits = await _context.MedicalRecords
                .Include(m => m.Doctor)
                .Include(m => m.Appointment)
                .Where(m => m.PatientID == patient.PatientID)
                .OrderByDescending(m => m.CreatedDate)
                .Take(5)
                .ToListAsync();

            // returning public lookup information
            return Ok(new PublicLookupResponseDto
            {
                // only displaying first name for privacy reasons
                PatientFirstName = patient.Name.Split(' ').First(),

                // converting appointments into dto list
                UpcomingAppointments = upcoming.Select(a => new PublicAppointmentDto
                {
                    AppointmentID = a.AppointmentID,
                    AppointmentDate = a.AppointmentDate,
                    StartTime = a.Schedule?.StartTime.ToString("HH:mm") ?? string.Empty,
                    DoctorName = a.Doctor?.Name ?? string.Empty,
                    Status = a.Status
                }).ToList(),

                // converting recent visits into dto list
                RecentVisits = recentVisits.Select(m => new PublicVisitSummaryDto
                {
                    VisitDate = m.CreatedDate,
                    DoctorName = m.Doctor?.Name ?? string.Empty,

                    // only diagnosis is shown here
                    Diagnosis = m.Diagnosis
                }).ToList()
            });
        }

        // same lookup endpoint but using get request
        [HttpGet("lookup")]
        public async Task<ActionResult<PublicLookupResponseDto>> LookupGet(
            [FromQuery] int cpr,
            [FromQuery] string reference)
        {
            // checking if reference code is empty
            if (string.IsNullOrWhiteSpace(reference))
                return BadRequest(new { message = "Both cpr and reference are required." });

            // calling main lookup method
            return await Lookup(new PublicLookupRequestDto
            {
                CPR = cpr,
                ReferenceCode = reference
            });
        }

        // endpoint to get all specializations
        [HttpGet("specializations")]
        public async Task<IActionResult> GetSpecializations()
        {
            // selecting specialization details from database
            var specializations = await _context.Specializations
                .Select(s => new { s.SpecializationID, s.Name, s.Description })
                .ToListAsync();

            return Ok(specializations);
        }

        // endpoint to get doctors and available schedule slots
        [HttpGet("doctors")]
        public async Task<IActionResult> GetDoctors([FromQuery] int? specializationID)
        {
            // getting doctors with specialization and schedule info
            var query = _context.Doctors
                .Include(d => d.DoctorSpecializations)
                    .ThenInclude(ds => ds.Specialization)
                .Include(d => d.Schedules)
                .AsQueryable();

            // filtering doctors if specialization id exists
            if (specializationID.HasValue)
                query = query.Where(d =>
                    d.DoctorSpecializations.Any(ds =>
                        ds.SpecializationID == specializationID));

            // converting result into anonymous object
            var doctors = await query.Select(d => new
            {
                d.DoctorID,
                d.Name,
                d.Qualification,

                // getting specialization names only
                Specializations = d.DoctorSpecializations
                                   .Select(ds => ds.Specialization!.Name),

                // getting only available slots
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