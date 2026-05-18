namespace MediConnectAPI.Models
{
    public class Staff
    {
        public int StaffID { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public int Phone { get; set; }

        public string Position { get; set; }

        public int UserID { get; set; }

        public User User { get; set; }
    }
}