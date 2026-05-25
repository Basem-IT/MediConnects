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

        // handles user registration
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            // checks if username already exists
            var exists = await _context.Users
                .AnyAsync(u => u.UserName == dto.UserName);

            if (exists)
                return Conflict(new { message = "Username already taken" });

            // finds the role from the roles table
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == dto.RoleName);

            if (role == null)
                return BadRequest(new { message = $"Role '{dto.RoleName}' does not exist" });

            // hashes password before saving
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                UserName = dto.UserName,
                Password = hashedPassword,
                RoleID = role.RoleID
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            // stores patient id if user is patient
            int? patientID = null;

            // creates patient record automatically
            if (dto.RoleName == "Patient")
            {
                // checks required fields
                if (string.IsNullOrEmpty(dto.PatientName) || dto.CPR == null)
                {
                    return BadRequest(new
                    {
                        message = "PatientName and CPR are required for Patient registration"
                    });
                }

                // generate patient reference code
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

            // loads role before creating token
            user.Role = role;

            var token = _tokenService.CreateToken(user);

            var expiryMinutes =
                int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

            return Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                UserName = user.UserName,
                Role = role.RoleName,
                PatientID = patientID
            });
        }

        // handles user login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            // gets user and role
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == dto.UserName);

            // checks username
            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid username or password"
                });
            }

            // checks password using bcrypt
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            {
                return Unauthorized(new
                {
                    message = "Invalid username or password"
                });
            }

            int? patientID = null;

            // finds linked patient account
            if (user.Role?.RoleName == "Patient")
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Email == dto.UserName);

                patientID = patient?.PatientID;
            }

            // creates jwt token after login
            var token = _tokenService.CreateToken(user);

            var expiryMinutes =
                int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

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