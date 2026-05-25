using System.ComponentModel.DataAnnotations;

namespace MediConnectAPI.DTOs
{
    // what the api will sends back for a appointment 
    public class AppointmentResponseDto
    {
        public int AppointmentID { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // doctor
        public int DoctorID { get; set; }
        public string DoctorName { get; set; } = string.Empty;

        // patient
        public int PatientID { get; set; }
        public string PatientName { get; set; } = string.Empty;

        // schedule
        public int ScheduleID { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    // what the person sends to create a appointment 
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

    // update appointment status only confirmd etc 
    public class UpdateStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}