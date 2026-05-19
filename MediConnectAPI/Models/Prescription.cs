using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediConnectAPI.Models
{
    public class Prescription
    {
        [Key]
        public int PrescriptionID { get; set; }

        public string MedicationName { get; set; } = string.Empty;

        public string Dosage { get; set; } = string.Empty;

        public string Frequency { get; set; } = string.Empty;

        public DateTime Duration { get; set; }
        public string Instructions { get; set; } = string.Empty;

        public int RecordID { get; set; }

        [ForeignKey("RecordID")]
        public MedicalRecord? MedicalRecord { get; set; }
    }
}