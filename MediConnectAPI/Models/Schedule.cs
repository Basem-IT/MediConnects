using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediConnectAPI.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleID { get; set; }

        public string DayOfWeek { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public string EndTime { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }

        public int DoctorID { get; set; }

        [ForeignKey("DoctorID")]
        public Doctor? Doctor { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}