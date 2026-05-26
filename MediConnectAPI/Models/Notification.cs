using System.ComponentModel.DataAnnotations.Schema;

namespace MediConnectAPI.Models
{
    public class Notification
    {
        public int NotificationID { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        // which appointment this is about 
        public int? AppointmentID { get; set; }

        // who this notification is for
        public int? UserID { get; set; }

        public Appointment? Appointment { get; set; }
    }
}