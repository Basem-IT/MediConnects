using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediConnectAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace MediConnectAPI.Services
{
    public interface ITokenService
    {
        // Takes a User (with Role navigation property loaded) and returns a signed JWT string
        string CreateToken(User user);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateToken(User user)
        {
            // Build claims which are checked by [Authorize] 
            var claims = new List<Claim>
            {
                // sub: the user's unique ID
                new(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),

                // unique_name: the username (used for display)
                new(JwtRegisteredClaimNames.UniqueName, user.UserName),

                // jti: unique token ID (prevents token reuse attacks)
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                // Role: what [Authorize(Roles = "Doctor")] checks against
                // Role navigation property must be loaded before calling this
                new(ClaimTypes.Role, user.Role?.RoleName ?? "Patient")
            };

            // Builds the signing keys from appsettings.json Jwt:Key
            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            // Serializes strings to the compact "header.payload.signature" 
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}