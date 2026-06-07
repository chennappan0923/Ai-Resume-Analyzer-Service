using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AI_Resume_Analyzer_API.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AI_Resume_Analyzer_API.Infrastructure.Security
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var keyStr = _configuration["Jwt:Secret"] ?? "SuperSecretKeyForResumeAnalyzerSolutionSecretKey123!";
            var issuer = _configuration["Jwt:Issuer"] ?? "ResumeAnalyzerApi";
            var audience = _configuration["Jwt:Audience"] ?? "ResumeAnalyzerUi";
            var expiryMinutes = double.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440"); // 1 day default

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
