namespace MediConnectAPI.Models
{
    public class Prescription
    {
        public int PrescriptionID { get; set; }

        public string MedicationName { get; set; }

        public string Dosage { get; set; }

        public string Frequency { get; set; }

        public string Instructions { get; set; }

        public DateTime Duration { get; set; }

        public int RecordID { get; set; }

        public MedicalRecord MedicalRecord { get; set; }
    }
}