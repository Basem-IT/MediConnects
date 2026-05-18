using MediConnectMVC.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MediConnectMVC.Controllers
{
    public class AppointmentTrackingController : Controller
    {
        private readonly IHubContext<AppointmentHub> _hubContext;

        public AppointmentTrackingController(IHubContext<AppointmentHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public IActionResult Live()
        {
            return View();
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