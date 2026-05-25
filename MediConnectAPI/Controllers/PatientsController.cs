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
        // database connection object
        private readonly MediConnectDbContext _context;

        // controller constructor
        public PatientsController(MediConnectDbContext context)
        {
            _context = context;
        }

        // endpoint to get all the patients
        [HttpGet]
        [Authorize(Roles = "ClinicManager,Receptionist")]
        public async Task<ActionResult<IEnumerable<PatientResponseDto>>> GetPatients()
        {
            // getting all the patients from database
            var patients = await _context.Patients.ToListAsync();

            // returning the patient data using dto
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

        // endpoint to get one patient by the id
        [HttpGet("{id:int}")]
        [Authorize(Roles = "ClinicManager,Receptionist,Doctor,Patient")]
        public async Task<ActionResult<PatientResponseDto>> GetPatient(int id)
        {
            // searching patient using pk
            var p = await _context.Patients.FindAsync(id);

            // check if patient exists
            if (p == null)
                return NotFound(new { message = $"Patient {id} not found" });

            // this return patient information
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

        // to get patient history with appointments and medical records
        [HttpGet("{id:int}/history")]
        [Authorize(Roles = "ClinicManager,Receptionist,Doctor,Patient")]
        public async Task<ActionResult<PatientHistoryDto>> GetPatientHistory(int id)
        {
            // finding patient by id
            var patient = await _context.Patients.FindAsync(id);

            // returning the error if patient is not found
            if (patient == null)
                return NotFound(new { message = $"Patient {id} not found" });

            // getting appointments with doctor and schedule details
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Schedule)
                .Where(a => a.PatientID == id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            // getting medical records and prescriptions
            var records = await _context.MedicalRecords
                .Include(m => m.Doctor)
                .Include(m => m.Prescriptions)
                .Where(m => m.PatientID == id)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();

            // returning patient history data
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

                // converting appointment data into dto format
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

                // converting medical records into dto objects
                MedicalRecords = records.Select(m => new MedicalRecordResponseDto
                {
                    RecordID = m.RecordID,
                    CreatedDate = m.CreatedDate,
                    Diagnosis = m.Diagnosis,
                    DoctorNotes = m.DoctorNotes,
                    VisitSummary = m.VisitSummary,
                    AppointmentID = m.AppointmentID,
                    DoctorName = m.Doctor?.Name ?? string.Empty,

                    // get all prescriptions for each record
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