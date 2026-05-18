using MediConnectMVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace MediConnectMVC.Controllers
{
    public class NotificationsController : Controller
    {
        public IActionResult Index()
        {
            var notifications = new List<Notification>
            {
                new Notification
                {
                    NotificationID = 1,
                    AppointmentID = 1,
                    Message = "Your appointment status changed to Checked-In.",
                    Type = "Appointment Update",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                },
                new Notification
                {
                    NotificationID = 2,
                    AppointmentID = 1,
                    Message = "Your appointment is now In Progress.",
                    Type = "Appointment Update",
                    IsRead = true,
                    CreatedAt = DateTime.Now
                }
            };

            return View(notifications);
        }
    }
}