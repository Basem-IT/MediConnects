namespace MediConnectAPI.Models
{
    public class Notification
    {
        public int NotificationID { get; set; }

        public string Message { get; set; }

        public string Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public int AppointmentID { get; set; }

        public Appointment Appointment { get; set; }
    }
}