using AI_Resume_Analyzer_API.Domain.Entities;
using AI_Resume_Analyzer_API.Infrastructure.Database;
using AI_Resume_Analyzer_API.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Resume_Analyzer_API.Features.Authentication
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ResumeAnalyzerDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(ResumeAnalyzerDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "A user with this email address already exists." });
            }

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = PasswordHasher.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Token = token
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return BadRequest(new { message = "Invalid email or password." });
            }

            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Token = token
            });
        }
    }
}
