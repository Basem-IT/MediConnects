using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediConnectAPI.Models
{
    public class Patient
    {
        [Key]
        public int PatientID { get; set; }

        public int CPR { get; set; }

        public DateTime DOB { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Phone { get; set; }

        public int? UserID { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    }
}