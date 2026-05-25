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
        // database context object
        private readonly MediConnectDbContext _context;

        // constructor to initialize database context
        public DoctorsController(MediConnectDbContext context)
        {
            _context = context;
        }

        // this endpoint gets all doctors
        // can also filter doctors by specialization id
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<DoctorResponseDto>>> GetDoctors(
            [FromQuery] int? specializationID)
        {
            // getting doctors with their specializations
            var query = _context.Doctors
                .Include(d => d.DoctorSpecializations)
                    .ThenInclude(ds => ds.Specialization)
                .AsQueryable();

            // filter doctors if specialization id is provided
            if (specializationID.HasValue)
                query = query.Where(d =>
                    d.DoctorSpecializations.Any(ds =>
                        ds.SpecializationID == specializationID));

            // execute query and store doctors list
            var doctors = await query.ToListAsync();

            // return doctors as dto objects
            return Ok(doctors.Select(d => new DoctorResponseDto
            {
                DoctorID = d.DoctorID,
                Name = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Qualification = d.Qualification,

                // getting specialization names only
                Specializations = d.DoctorSpecializations
                                   .Select(ds => ds.Specialization?.Name ?? string.Empty)
                                   .ToList()
            }));
        }

        // this endpoint gets one doctor using doctor id.
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<DoctorResponseDto>> GetDoctor(int id)
        {
            // searching for doctor with specialization details
            var d = await _context.Doctors
                .Include(d => d.DoctorSpecializations)
                    .ThenInclude(ds => ds.Specialization)
                .FirstOrDefaultAsync(d => d.DoctorID == id);

            // return the error if doctor does not exist
            if (d == null)
                return NotFound(new { message = $"Doctor {id} not found" });

            // return doctordetails
            return Ok(new DoctorResponseDto
            {
                DoctorID = d.DoctorID,
                Name = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Qualification = d.Qualification,

                // converting specialization objects into names
                Specializations = d.DoctorSpecializations
                                   .Select(ds => ds.Specialization?.Name ?? string.Empty)
                                   .ToList()
            });
        }

        // this endpoint gets doctor schedule and appointments
        [HttpGet("{id:int}/schedule")]
        [Authorize(Roles = "ClinicManager,Receptionist,Doctor,Patient")]
        public async Task<ActionResult<DoctorScheduleDto>> GetDoctorSchedule(
            int id,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            // finding doctor by id
            var doctor = await _context.Doctors.FindAsync(id);

            // checking if doctor exists
            if (doctor == null)
                return NotFound(new { message = $"Doctor {id} not found" });

            // getting all schedule slots for this doctor
            var schedules = await _context.Schedules
                .Where(s => s.DoctorID == id)
                .ToListAsync();

            // setting default dates if user does not enter them
            var fromDate = from?.Date ?? DateTime.UtcNow.Date;
            var toDate = to?.Date ?? fromDate.AddDays(7);

            // getting appointments in selected date range
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

            // returning doctor schedule info
            return Ok(new DoctorScheduleDto
            {
                DoctorID = doctor.DoctorID,
                DoctorName = doctor.Name,

                // converting schedules into dto format
                Schedules = schedules.Select(s => new ScheduleSlotDto
                {
                    ScheduleID = s.ScheduleID,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime.ToString("HH:mm"),
                    EndTime = s.EndTime,
                    IsAvailable = s.IsAvailable
                }).ToList(),

                // converting appointments into dto format
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