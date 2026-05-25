using MediConnectAPI.Data;
using MediConnectAPI.DTOs;
using MediConnectAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediConnectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly MediConnectDbContext _context;

        public AppointmentsController(MediConnectDbContext context)
        {
            _context = context;
        }

        // gets all appointments
        // can also filter by doctor, patient, status or date
        [HttpGet]
        [Authorize(Roles = "ClinicManager,Receptionist,Doctor,Patient")]
        public async Task<ActionResult<IEnumerable<AppointmentResponseDto>>> GetAppointments(
            [FromQuery] int? doctorID,
            [FromQuery] int? patientID,
            [FromQuery] string? status,
            [FromQuery] DateTime? date)
        {
            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .AsQueryable();

            // filter by doctor id
            if (doctorID.HasValue)
                query = query.Where(a => a.DoctorID == doctorID);

            // filter by patient id
            if (patientID.HasValue)
                query = query.Where(a => a.PatientID == patientID);

            // filter by appointment date
            if (date.HasValue)
                query = query.Where(a => a.AppointmentDate.Date == date.Value.Date);

            // filter by status
            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            var list = await query
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return Ok(list.Select(MapToDto));
        }

        // gets one appointment using the id
        [HttpGet("{id:int}")]
        [Authorize(Roles = "ClinicManager,Receptionist,Doctor,Patient")]
        public async Task<ActionResult<AppointmentResponseDto>> GetAppointment(int id)
        {
            var a = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            // checks if appointment exists
            if (a == null)
                return NotFound(new { message = $"Appointment {id} not found" });

            return Ok(MapToDto(a));
        }

        // creates a new appointment
        [HttpPost]
        [Authorize(Roles = "ClinicManager,Receptionist,Patient")]
        public async Task<ActionResult<AppointmentResponseDto>> CreateAppointment(
            CreateAppointmentDto dto)
        {
            // checks if the schedule belongs to the doctor
            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(s =>
                    s.ScheduleID == dto.ScheduleID &&
                    s.DoctorID == dto.DoctorID);

            if (schedule == null)
                return NotFound(new { message = "Schedule slot not found for this doctor" });

            // checks if slot is available
            if (!schedule.IsAvailable)
                return Conflict(new { message = "This schedule slot is not available" });

            // prevents booking the same slot twice
            var alreadyBooked = await _context.Appointments.AnyAsync(a =>
                a.DoctorID == dto.DoctorID &&
                a.ScheduleID == dto.ScheduleID &&
                a.AppointmentDate.Date == dto.AppointmentDate.Date &&
                a.Status != "Cancelled" &&
                a.Status != "Missed");

            if (alreadyBooked)
            {
                return Conflict(new
                {
                    message = "This time slot is already booked for the selected date"
                });
            }

            // checks if patient exists
            var patientExists = await _context.Patients
                .AnyAsync(p => p.PatientID == dto.PatientID);

            if (!patientExists)
                return NotFound(new { message = $"Patient {dto.PatientID} not found" });

            var appointment = new Appointment
            {
                PatientID = dto.PatientID,
                DoctorID = dto.DoctorID,
                ScheduleID = dto.ScheduleID,
                AppointmentDate = dto.AppointmentDate,
                Reason = dto.Reason,
                Status = "Requested"
            };

            _context.Appointments.Add(appointment);

            await _context.SaveChangesAsync();

            // loads related data after saving
            await _context.Entry(appointment).Reference(a => a.Doctor).LoadAsync();
            await _context.Entry(appointment).Reference(a => a.Patient).LoadAsync();
            await _context.Entry(appointment).Reference(a => a.Schedule).LoadAsync();

            return CreatedAtAction(
                nameof(GetAppointment),
                new { id = appointment.AppointmentID },
                MapToDto(appointment));
        }

        // updates appointment status
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "ClinicManager,Receptionist,Doctor")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateStatusDto dto)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            // checks if appointment exists
            if (appointment == null)
                return NotFound(new { message = $"Appointment {id} not found" });

            // checks if status change is valid
            if (!IsValidTransition(appointment.Status, dto.Status))
            {
                return BadRequest(new
                {
                    message = $"Cannot change status from '{appointment.Status}' to '{dto.Status}'"
                });
            }

            appointment.Status = dto.Status;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // gets medical records linked to an appointment
        [HttpGet("{id:int}/medical-records")]
        [Authorize(Roles = "ClinicManager,Doctor,Patient")]
        public async Task<ActionResult<IEnumerable<MedicalRecordResponseDto>>> GetMedicalRecords(int id)
        {
            var appointmentExists = await _context.Appointments
                .AnyAsync(a => a.AppointmentID == id);

            if (!appointmentExists)
                return NotFound(new { message = $"Appointment {id} not found" });

            var records = await _context.MedicalRecords
                .Include(m => m.Doctor)
                .Include(m => m.Prescriptions)
                .Where(m => m.AppointmentID == id)
                .ToListAsync();

            return Ok(records.Select(m => new MedicalRecordResponseDto
            {
                RecordID = m.RecordID,
                CreatedDate = m.CreatedDate,
                Diagnosis = m.Diagnosis,
                DoctorNotes = m.DoctorNotes,
                VisitSummary = m.VisitSummary,
                AppointmentID = m.AppointmentID,
                DoctorName = m.Doctor?.Name ?? string.Empty,

                Prescriptions = m.Prescriptions.Select(p => new PrescriptionResponseDto
                {
                    PrescriptionID = p.PrescriptionID,
                    MedicationName = p.MedicationName,
                    Dosage = p.Dosage,
                    Frequency = p.Frequency,
                    Instructions = p.Instructions,
                    Duration = p.Duration
                }).ToList()
            }));
        }

        // converts appointment object into dto
        private static AppointmentResponseDto MapToDto(Appointment a) => new()
        {
            AppointmentID = a.AppointmentID,
            AppointmentDate = a.AppointmentDate,
            Reason = a.Reason,
            Status = a.Status,
            DoctorID = a.DoctorID,
            DoctorName = a.Doctor?.Name ?? string.Empty,
            PatientID = a.PatientID,
            PatientName = a.Patient?.Name ?? string.Empty,
            ScheduleID = a.ScheduleID,
            DayOfWeek = a.Schedule?.DayOfWeek ?? string.Empty,
            StartTime = a.Schedule?.StartTime.ToString("HH:mm") ?? string.Empty,
            EndTime = a.Schedule?.EndTime ?? string.Empty
        };

        // checks if status change is allowed
        private static bool IsValidTransition(string current, string next)
        {
            return (current, next) switch
            {
                ("Requested", "Confirmed") => true,
                ("Requested", "Cancelled") => true,
                ("Confirmed", "CheckedIn") => true,
                ("Confirmed", "Cancelled") => true,
                ("Confirmed", "Missed") => true,
                ("CheckedIn", "InProgress") => true,
                ("InProgress", "Completed") => true,
                ("InProgress", "Cancelled") => true,
                _ => false
            };
        }
    }
}