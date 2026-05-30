using System.ComponentModel.DataAnnotations;

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

        [RegularExpression(@"^(3|6)\d{7}$", ErrorMessage = "Phone must be a valid Bahrain number (8 digits starting with 3 or 6)")]
        public int Phone { get; set; }
        public DateTime DOB { get; set; }
    }
}