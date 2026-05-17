namespace MediConnectAPI.Models
{
    public class DoctorSpecialization
    {
        public int DoctorSpecializationID { get; set; }

        public int DoctorID { get; set; }

        public Doctor Doctor { get; set; }

        public int SpecializationID { get; set; }

        public Specialization Specialization { get; set; }
    }
}