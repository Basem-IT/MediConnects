namespace MediConnectAPI.Models
{
    public class Specialization
    {
        public int SpecializationID { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ICollection<DoctorSpecialization> DoctorSpecializations { get; set; }
    }
}