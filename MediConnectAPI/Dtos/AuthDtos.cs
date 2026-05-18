using System.ComponentModel.DataAnnotations;

namespace MediConnectAPI.DTOs
{
    // What the clients submits to register
    public class RegisterDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        // Role name: "Patient", "Doctor", "Receptionist", "ClinicManager"
        [Required]
        public string RoleName { get; set; } = "Patient";

        // Patient-specific fields (only needed if RoleName == "Patient")
        public string? PatientName { get; set; }
        public int? CPR { get; set; }
        public DateTime? DOB { get; set; }
        public string? Email { get; set; }
        public int? Phone { get; set; }
    }

    // What the clients submits to login
    public class LoginDto
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // What the API returns after a successful login/register
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // Returned so the MVC app knows which patient record belongs to this user
        public int? PatientID { get; set; }
    }
}