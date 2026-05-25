using MediConnectAPI.Data;
using MediConnectMVC.Filters;
using MediConnectMVC.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MediConnectMVC.Controllers
{
    [SessionAuthorize]
    public class AppointmentTrackingController : Controller
    {
        private readonly MediConnectDbContext _context;
        private readonly IHubContext<AppointmentHub> _hubContext;

        public AppointmentTrackingController(MediConnectDbContext context, IHubContext<AppointmentHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Live()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .ToListAsync();
            return View(appointments);
        }

        public async Task<IActionResult> TestUpdate()
        {
            await _hubContext.Clients.All.SendAsync("AppointmentStatusUpdated", new
            {
                appointmentId = 1,
                status = "In Progress"
            });
            return RedirectToAction("Live");
        }
    }
}