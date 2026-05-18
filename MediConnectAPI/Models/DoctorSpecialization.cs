using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace MediConnectAPI.Models
{
    public class DoctorSpecialization
    {
        [Key]
        public int DoctorSpecializationID { get; set; }

        public int DoctorID { get; set; }
        public int SpecializationID { get; set; }

        [ForeignKey("DoctorID")]
        public virtual Doctor? Doctor { get; set; }

        [ForeignKey("SpecializationID")]
        public virtual Specialization? Specialization { get; set; }
    }
}