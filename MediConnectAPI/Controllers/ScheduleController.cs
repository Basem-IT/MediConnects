using MediConnectAPI.Data;
using MediConnectAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediConnectAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchedulesController : ControllerBase
    {
        private readonly MediConnectDbContext _context;

        // connect to database
        public SchedulesController(MediConnectDbContext context)
        {
            _context = context;
        }

        // get all schedules from the db
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Schedule>>> GetSchedules()
        {
            return await _context.Schedules.ToListAsync();
        }

        // get one schedule by id
        [HttpGet("{id}")]
        public async Task<ActionResult<Schedule>> GetSchedule(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);

            // return error
            if (schedule == null)
                return NotFound();

            return schedule;
        }

        // add a new schedule to database
        [HttpPost]
        public async Task<ActionResult<Schedule>> AddSchedule(Schedule schedule)
        {
            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            // return the created schedule with id
            return CreatedAtAction(nameof(GetSchedule), new { id = schedule.ScheduleID }, schedule);
        }
    }
}