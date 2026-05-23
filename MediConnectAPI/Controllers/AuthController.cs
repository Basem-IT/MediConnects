using MediConnectAPI.Data;
using MediConnectAPI.DTOs;
using MediConnectAPI.Models;
using MediConnectAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediConnectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MediConnectDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public AuthController(
            MediConnectDbContext context,
            ITokenService tokenService,
            IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _config = config;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            // Check if username is not already taken
            var exists = await _context.Users
                .AnyAsync(u => u.UserName == dto.UserName);
            if (exists)
                return Conflict(new { message = "Username already taken" });

            // Find the role by name, the roles are seeded inside the database
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == dto.RoleName);
            if (role == null)
                return BadRequest(new { message = $"Role '{dto.RoleName}' does not exist" });

            // Hash the password using BCrypt which never stores plain text
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                UserName = dto.UserName,
                Password = hashedPassword,
                RoleID = role.RoleID
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // If registring as a Patient, create the Patient record to
            int? patientID = null;
            if (dto.RoleName == "Patient")
            {
                // Ensure patient fields are provided
                if (string.IsNullOrEmpty(dto.PatientName) || dto.CPR == null)
                    return BadRequest(new { message = "PatientName and CPR are required for Patient registration" });

                // Count existing patients to generate a unique reference code
                var patientCount = await _context.Patients.CountAsync();
                var referenceCode = $"PAT-{(patientCount + 1):D4}"; 

                var patient = new Patient
                {
                    Name = dto.PatientName,
                    CPR = dto.CPR.Value,
                    DOB = dto.DOB ?? DateTime.UtcNow,
                    Email = dto.Email ?? string.Empty,
                    Phone = dto.Phone ?? 0,
                    ReferenceCode = referenceCode
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
                patientID = patient.PatientID;
            }

            // Load the Role navigation property before  generating the token
            user.Role = role;
            var token = _tokenService.CreateToken(user);
            var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

            return Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                UserName = user.UserName,
                Role = role.RoleName,
                PatientID = patientID
            });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            // Load user with Role so we can put the role name in the token
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == dto.UserName);

            // Use a generic message — don't reveal whether username or password was wrong
            if (user == null)
                return Unauthorized(new { message = "Invalid username or password" });

            if (user.Password != dto.Password)
                return Unauthorized(new { message = "Invalid username or password" });

            // Check if this user has a linked Patient record
            int? patientID = null;
            if (user.Role?.RoleName == "Patient")
            {
                // Match patient by username (email is used as username for patients)
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Email == dto.UserName);
                patientID = patient?.PatientID;
            }

            var token = _tokenService.CreateToken(user);
            var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

            return Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                UserName = user.UserName,
                Role = user.Role?.RoleName ?? string.Empty,
                PatientID = patientID
            });
        }
    }
}