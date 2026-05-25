using System.ComponentModel.DataAnnotations;

namespace MediConnectAPI.DTOs
{
    // the clients submits to register
    public class RegisterDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        // the role name patient etc
        [Required]
        public string RoleName { get; set; } = "Patient";

        // only for patients
        public string? PatientName { get; set; }
        public int? CPR { get; set; }
        public DateTime? DOB { get; set; }
        public string? Email { get; set; }
        public int? Phone { get; set; }
    }

    // person submits to login
    public class LoginDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // the api returns after a successful login or register
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // returned so the mvc app knows to which patient record belongs to this user
        public int? PatientID { get; set; }
    }
}