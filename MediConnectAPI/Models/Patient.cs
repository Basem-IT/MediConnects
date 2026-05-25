using System.ComponentModel.DataAnnotations.Schema;

namespace MediConnectAPI.Models

{
    public class Patient
    {
        public int PatientID { get; set; }

        public string Name { get; set; } = string.Empty;

        public int CPR { get; set; }

        public DateTime DOB { get; set; }

        public string Email { get; set; } = string.Empty;

        public int Phone { get; set; }

        public string ReferenceCode { get; set; } = string.Empty;

        public int? UserID { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        public ICollection<Appointment>? Appointments { get; set; }

        public ICollection<MedicalRecord>? MedicalRecords { get; set; }
    }
}