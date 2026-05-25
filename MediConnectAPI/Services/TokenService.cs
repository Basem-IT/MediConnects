using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediConnectAPI.Models;
using Microsoft.IdentityModel.Tokens;

namespace MediConnectAPI.Services
{
    public interface ITokenService
    {
        // function to generate jwt token for a user
        string CreateToken(User user);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        // get config values jwt settings from appsettings
        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateToken(User user)
        {
            // the data we put inside the token
            var claims = new List<Claim>
            {
                // user id inside token
                new(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),

                // username for identification
                new(JwtRegisteredClaimNames.UniqueName, user.UserName),

                // random id for token uniqueness
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                // user role used for authorize checks
                new(ClaimTypes.Role, user.Role?.RoleName ?? "Patient")
            };

            // creating a key from appsettings jwt secret
            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // token expiry time the default 60 mins if not set
            var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

            // building the token
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            // convert token to string so it can be sent to client
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}