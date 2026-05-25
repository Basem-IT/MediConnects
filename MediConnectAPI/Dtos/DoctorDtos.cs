namespace MediConnectAPI.DTOs
{
    // the doctor profile 
    public class DoctorResponseDto
    {
        public int DoctorID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;

        // specialization names into a simplee list
        public List<string> Specializations { get; set; } = new();
    }

    // doctor schedule working slots and booked appointments 

    public class DoctorScheduleDto
    {
        public int DoctorID { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public List<ScheduleSlotDto> Schedules { get; set; } = new();
        public List<AppointmentResponseDto> UpcomingAppointments { get; set; } = new();
    }

    // schedule slot 

    public class ScheduleSlotDto
    {
        public int ScheduleID { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
}