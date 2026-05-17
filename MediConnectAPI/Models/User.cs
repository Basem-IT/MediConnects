using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MediConnectAPI.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public int RoleID { get; set; }

        [ForeignKey("RoleID")]
        public Role Role { get; set; }

        public ICollection<Staff> Staffs { get; set; } = new List<Staff>();
    }
}