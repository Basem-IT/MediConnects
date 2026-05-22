using MediConnectAPI.Data;
using MediConnectAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediConnectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly MediConnectDbContext _context;

        public PatientsController(MediConnectDbContext context)
        {
            _context = context;
        }

        // GET /api/patients
        [HttpGet]
        [Authorize(Roles = "ClinicManager,Receptionist")]
        public async Task<ActionResult<IEnumerable<PatientResponseDto>>> GetPatients()
        {
            var patients = await _context.Patients.ToListAsync();

            return Ok(patients.Select(p => new PatientResponseDto
            {
                PatientID = p.PatientID,
                Name = p.Name,
                CPR = p.CPR,
                DOB = p.DOB,
                Email = p.Email,
                Phone = p.Phone,
                ReferenceCode = p.ReferenceCode
            }));
        }

        // GET /api/patients/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "ClinicManager,Receptionist,Doctor,Patient")]
        public async Task<ActionResult<PatientResponseDto>> GetPatient(int id)
        {
            var p = await _context.Patients.FindAsync(id);
            if (p == null)
                return NotFound(new { message = $"Patient {id} not found" });

            return Ok(new PatientResponseDto
            {
                PatientID = p.PatientID,
                Name = p.Name,
                CPR = p.CPR,
                DOB = p.DOB,
                Email = p.Email,
                Phone = p.Phone,
                ReferenceCode = p.ReferenceCode
            });
        }

        // ENDPOINT 5: GET /api/patients/{id}/history
        [HttpGet("{id:int}/history")]
        [Authorize(Roles = "ClinicManager,Receptionist,Doctor,Patient")]
        public async Task<ActionResult<PatientHistoryDto>> GetPatientHistory(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
                return NotFound(new { message = $"Patient {id} not found" });

            // Load appointments with doctor and schedule info
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Schedule)
                .Where(a => a.PatientID == id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            // Load medical records with doctor and prescriptions
            var records = await _context.MedicalRecords
                .Include(m => m.Doctor)
                .Include(m => m.Prescriptions)
                .Where(m => m.PatientID == id)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();

            return Ok(new PatientHistoryDto
            {
                Patient = new PatientResponseDto
                {
                    PatientID = patient.PatientID,
                    Name = patient.Name,
                    CPR = patient.CPR,
                    DOB = patient.DOB,
                    Email = patient.Email,
                    Phone = patient.Phone,
                    ReferenceCode = patient.ReferenceCode
                },
                Appointments = appointments.Select(a => new AppointmentResponseDto
                {
                    AppointmentID = a.AppointmentID,
                    AppointmentDate = a.AppointmentDate,
                    Reason = a.Reason,
                    Status = a.Status,
                    DoctorID = a.DoctorID,
                    DoctorName = a.Doctor?.Name ?? string.Empty,
                    PatientID = a.PatientID,
                    PatientName = patient.Name,
                    ScheduleID = a.ScheduleID,
                    DayOfWeek = a.Schedule?.DayOfWeek ?? string.Empty,
                    StartTime = a.Schedule?.StartTime.ToString("HH:mm") ?? string.Empty,
                    EndTime = a.Schedule?.EndTime ?? string.Empty
                }).ToList(),
                MedicalRecords = records.Select(m => new MedicalRecordResponseDto
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
                }).ToList()
            });
        }
    }
}