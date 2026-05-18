using System.ComponentModel.DataAnnotations;

namespace MediConnectAPI.DTOs
{
    // Response: what the API sends back for an appointment 
    // Uses flat strings instead of nested objects that avoids circular issues and keeps the JSON clean for the MVC aswell as reporting apps.
    public class AppointmentResponseDto
    {
        public int AppointmentID { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Doctor info flattened
        public int DoctorID { get; set; }
        public string DoctorName { get; set; } = string.Empty;

        // Patient info flattened
        public int PatientID { get; set; }
        public string PatientName { get; set; } = string.Empty;

        // Schedule info flattened
        public int ScheduleID { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    // Request: what the client sends to create an appointment 
    public class CreateAppointmentDto
    {
        [Required]
        public int PatientID { get; set; }

        [Required]
        public int DoctorID { get; set; }

        [Required]
        public int ScheduleID { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    // Request: update appointment status only
    // Used by receptionists (Confirmed, CheckedIn) and doctor (InProgress, Completed)
    public class UpdateStatusDto
    {
        [Required]
        // Valid values: Requested, Confirmed, CheckedIn, InProgress, Completed, Cancelled, Missed
        public string Status { get; set; } = string.Empty;
    }
}