namespace MediConnectAPI.Models
{
    public class Patient
    {
        public int PatientID { get; set; }

        public string Name { get; set; }

        public int CPR { get; set; }

        public DateTime DOB { get; set; }

        public string Email { get; set; }

        public int Phone { get; set; }

        public ICollection<Appointment>? Appointments { get; set; }

        public ICollection<MedicalRecord>? MedicalRecords { get; set; }
    }
}