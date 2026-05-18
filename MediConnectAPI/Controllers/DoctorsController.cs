using MediConnectAPI.Data;
using MediConnectAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediConnectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorsController : ControllerBase
    {
        private readonly MediConnectDbContext _context;

        public DoctorsController(MediConnectDbContext context)
        {
            _context = context;
        }

        // GET /api/doctors
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<DoctorResponseDto>>> GetDoctors(
            [FromQuery] int? specializationID)
        {
            var query = _context.Doctors
                .Include(d => d.DoctorSpecializations)
                    .ThenInclude(ds => ds.Specialization)
                .AsQueryable();

            if (specializationID.HasValue)
                query = query.Where(d =>
                    d.DoctorSpecializations.Any(ds =>
                        ds.SpecializationID == specializationID));

            var doctors = await query.ToListAsync();

            return Ok(doctors.Select(d => new DoctorResponseDto
            {
                DoctorID = d.DoctorID,
                Name = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Qualification = d.Qualification,
                Specializations = d.DoctorSpecializations
                                   .Select(ds => ds.Specialization?.Name ?? string.Empty)
                                   .ToList()
            }));
        }

        // GET /api/doctors/{id}
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<DoctorResponseDto>> GetDoctor(int id)
        {
            var d = await _context.Doctors
                .Include(d => d.DoctorSpecializations)
                    .ThenInclude(ds => ds.Specialization)
                .FirstOrDefaultAsync(d => d.DoctorID == id);

            if (d == null)
                return NotFound(new { message = $"Doctor {id} not found" });

            return Ok(new DoctorResponseDto
            {
                DoctorID = d.DoctorID,
                Name = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Qualification = d.Qualification,
                Specializations = d.DoctorSpecializations
                                   .Select(ds => ds.Specialization?.Name ?? string.Empty)
                                   .ToList()
            });
        }

        // GET /api/doctors/{id}/schedule
        [HttpGet("{id:int}/schedule")]
        [Authorize(Roles = "ClinicManager,Receptionist,Doctor,Patient")]
        public async Task<ActionResult<DoctorScheduleDto>> GetDoctorSchedule(
            int id,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
                return NotFound(new { message = $"Doctor {id} not found" });

            // Load all schedule slots for the specified doctor id
            var schedules = await _context.Schedules
                .Where(s => s.DoctorID == id)
                .ToListAsync();

            var fromDate = from?.Date ?? DateTime.UtcNow.Date;
            var toDate = to?.Date ?? fromDate.AddDays(7);

            // Load upcoming appointments in the date range
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .Where(a =>
                    a.DoctorID == id &&
                    a.AppointmentDate.Date >= fromDate &&
                    a.AppointmentDate.Date <= toDate &&
                    a.Status != "Cancelled" &&
                    a.Status != "Missed")
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return Ok(new DoctorScheduleDto
            {
                DoctorID = doctor.DoctorID,
                DoctorName = doctor.Name,
                Schedules = schedules.Select(s => new ScheduleSlotDto
                {
                    ScheduleID = s.ScheduleID,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime.ToString("HH:mm"),
                    EndTime = s.EndTime,
                    IsAvailable = s.IsAvailable
                }).ToList(),
                UpcomingAppointments = appointments.Select(a => new AppointmentResponseDto
                {
                    AppointmentID = a.AppointmentID,
                    AppointmentDate = a.AppointmentDate,
                    Reason = a.Reason,
                    Status = a.Status,
                    DoctorID = a.DoctorID,
                    DoctorName = doctor.Name,
                    PatientID = a.PatientID,
                    PatientName = a.Patient?.Name ?? string.Empty,
                    ScheduleID = a.ScheduleID,
                    DayOfWeek = a.Schedule?.DayOfWeek ?? string.Empty,
                    StartTime = a.Schedule?.StartTime.ToString("HH:mm") ?? string.Empty,
                    EndTime = a.Schedule?.EndTime ?? string.Empty
                }).ToList()
            });
        }
    }
}