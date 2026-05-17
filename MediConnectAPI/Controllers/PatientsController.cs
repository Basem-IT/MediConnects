using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.Models;

namespace MediConnectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly MediConnectDbContext _context;

        public PatientsController(MediConnectDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
        {
            return await _context.Patients.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);

            if (patient == null)
            {
                return NotFound();
            }

            return patient;
        }

        [HttpPost]
        public async Task<ActionResult<Patient>> AddPatient(Patient patient)
        {
            _context.Patients.Add(patient);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPatient), new { id = patient.PatientID }, patient);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, Patient patient)
        {
            if (id != patient.PatientID)
            {
                return BadRequest();
            }

            _context.Entry(patient).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);

            if (patient == null)
            {
                return NotFound();
            }

            _context.Patients.Remove(patient);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}