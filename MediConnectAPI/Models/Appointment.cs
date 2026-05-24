using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediConnectAPI.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentID { get; set; }

        public DateTime AppointmentDate { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int DoctorID { get; set; }

        public int PatientID { get; set; }

        public int ScheduleID { get; set; }

        [ForeignKey("DoctorID")]
        public virtual Doctor? Doctor { get; set; }

        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }

        public int? UserID { get; set; }
        public User? User { get; set; }

        [ForeignKey("ScheduleID")]
        public virtual Schedule? Schedule { get; set; }

        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}