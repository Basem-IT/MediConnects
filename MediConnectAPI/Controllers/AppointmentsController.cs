using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.Models;

namespace MediConnectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly MediConnectDbContext _context;

        public AppointmentsController(MediConnectDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
        {
            return await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Appointment>> GetAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return appointment;
        }

        [HttpPost]
        public async Task<ActionResult<Appointment>> AddAppointment(Appointment appointment)
        {
            _context.Appointments.Add(appointment);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAppointment),
                new { id = appointment.AppointmentID },
                appointment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, Appointment appointment)
        {
            if (id != appointment.AppointmentID)
            {
                return BadRequest();
            }

            _context.Entry(appointment).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            _context.Appointments.Remove(appointment);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}