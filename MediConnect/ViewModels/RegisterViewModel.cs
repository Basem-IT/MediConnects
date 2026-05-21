namespace MediConnectMVC.ViewModels
{
    public class RegisterViewModel
    {
        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CPR { get; set; }
        public int Phone { get; set; }
        public DateTime DOB { get; set; }
    }
}