using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediConnectAPI.Models
{
    public class MedicalRecord
    {
        [Key]
        public int RecordID { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Diagnosis { get; set; } = string.Empty;

        public string DoctorNotes { get; set; } = string.Empty;

        public string VisitSummary { get; set; } = string.Empty;

        public int AppointmentID { get; set; }

        public int DoctorID { get; set; }

        public int PatientID { get; set; }

        [ForeignKey("AppointmentID")]
        public virtual Appointment? Appointment { get; set; }

        [ForeignKey("DoctorID")]
        public virtual Doctor? Doctor { get; set; }

        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }

        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}