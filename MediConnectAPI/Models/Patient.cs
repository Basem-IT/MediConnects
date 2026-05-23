using System.ComponentModel.DataAnnotations.Schema;

namespace MediConnectAPI.Models

// Added by Student 2 for the public appointment lookup feature, which is generated automatically when a patient is registered. In the format: "PAT-0001", "PAT-0002", etc.
{
    public class Patient
    {
        public int PatientID { get; set; }

        public string Name { get; set; } = string.Empty;

        public int CPR { get; set; }

        public DateTime DOB { get; set; }

        public string Email { get; set; } = string.Empty;

        public int Phone { get; set; }

        // Added by Student 2 for public appointment lookup
        public string ReferenceCode { get; set; } = string.Empty;

        // LOGIN LINK
        public int? UserID { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        public ICollection<Appointment>? Appointments { get; set; }

        public ICollection<MedicalRecord>? MedicalRecords { get; set; }
    }
}