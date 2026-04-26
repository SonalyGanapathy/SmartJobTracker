using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartJobTracker.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, IConfiguration config, ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        /// <summary>Register a new user account.</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var emailNormalised = dto.Email.Trim().ToLowerInvariant();

            // Check for duplicate email
            var exists = await _context.UserProfiles
                .AnyAsync(u => u.Email.ToLower() == emailNormalised);
            if (exists)
                return Conflict(new { message = "An account with this email already exists." });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var profile = new UserProfile
            {
                FullName = dto.FullName.Trim(),
                Email = emailNormalised,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: {Email} (Id={Id})", emailNormalised, profile.Id);

            var response = BuildAuthResponse(profile);
            return CreatedAtAction(nameof(Register), response);
        }

        /// <summary>Log in with email and password. Returns a JWT.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var emailNormalised = dto.Email.Trim().ToLowerInvariant();

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalised);

            if (profile == null || string.IsNullOrEmpty(profile.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, profile.PasswordHash);
            if (!passwordValid)
                return Unauthorized(new { message = "Invalid email or password." });

            _logger.LogInformation("User logged in: {Email} (Id={Id})", emailNormalised, profile.Id);

            var response = BuildAuthResponse(profile);
            return Ok(response);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private AuthResponseDto BuildAuthResponse(UserProfile profile)
        {
            var jwtKey = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            var issuer  = _config["Jwt:Issuer"]  ?? "SmartJobTracker";
            var audience = _config["Jwt:Audience"] ?? "SmartJobTrackerUI";
            var expiryHours = int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 72;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, profile.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, profile.Email),
                new Claim(JwtRegisteredClaimNames.Name, profile.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                UserId = profile.Id,
                FullName = profile.FullName,
                Email = profile.Email,
                ExpiresAt = expiresAt
            };
        }
    }
}
