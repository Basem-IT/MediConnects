using MediConnectAPI.Data;
using MediConnectMVC.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediConnectMVC.Controllers
{
    [SessionAuthorize]
    [RoleAuthorize("Clinic Manager", "Receptionist", "Doctor", "Patient")]
    public class NotificationsController : Controller
    {
        private readonly MediConnectDbContext _context;

        public NotificationsController(MediConnectDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserID");

            var notifications = _context.Notifications
                .Include(n => n.Appointment)
                    .ThenInclude(a => a.Patient)
                .AsQueryable();

            if (role == "Patient")
            {
                notifications = notifications.Where(n =>
                    n.Appointment != null &&
                    n.Appointment.Patient != null &&
                    n.Appointment.Patient.UserID == userId);
            }

            return View(await notifications.ToListAsync());
        }

        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}