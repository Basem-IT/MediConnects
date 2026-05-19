
namespace MediConnectMVC.Models
{
    public class Notification
    {
        public int NotificationID { get; set; }

        public int AppointmentID { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }

}