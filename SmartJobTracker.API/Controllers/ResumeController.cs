using Microsoft.AspNetCore.Mvc;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Services;

namespace SmartJobTracker.API.Controllers
{
    [Route("api/[controller]")]
    public class ResumeController : ApiControllerBase
    {
        private readonly IResumeParserService _resumeParserService;
        private readonly AppDbContext _context;

        public ResumeController(IResumeParserService resumeParserService, AppDbContext context)
        {
            _resumeParserService = resumeParserService;
            _context             = context;
        }

        /// <summary>Upload and parse a resume. Returns extracted data without saving.</summary>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(ResumeParseResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            var allowedExtensions = new[] { ".pdf", ".txt", ".doc", ".docx" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
                return BadRequest($"File type not supported. Allowed: {string.Join(", ", allowedExtensions)}");

            try
            {
                using var stream = file.OpenReadStream();
                var parseResult = await _resumeParserService.ParseResumeAsync(stream, file.FileName);
                return Ok(parseResult);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error parsing resume: {ex.Message}");
            }
        }

        /// <summary>Upload resume and auto-update the current user's profile from parsed data.</summary>
        [HttpPost("upload-and-create-profile")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadAndCreateProfile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            var userId = GetCurrentUserId();

            try
            {
                using var stream = file.OpenReadStream();
                var parseResult = await _resumeParserService.ParseResumeAsync(stream, file.FileName);

                var profile = await _context.UserProfiles.FindAsync(userId);
                if (profile == null)
                    return NotFound("User profile not found.");

                // Merge parsed data into the existing profile
                if (!string.IsNullOrEmpty(parseResult.FullName))  profile.FullName = parseResult.FullName;
                if (!string.IsNullOrEmpty(parseResult.Email))     profile.Email    = parseResult.Email;
                if (!string.IsNullOrEmpty(parseResult.Phone))     profile.Phone    = parseResult.Phone;
                if (!string.IsNullOrEmpty(parseResult.Summary))   profile.Summary  = parseResult.Summary;
                if (parseResult.Skills.Count > 0)
                    profile.Skills = string.Join(", ", parseResult.Skills);
                profile.ResumeFileName = file.FileName;
                profile.UpdatedAt      = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new UserProfileDto
                {
                    Id                  = profile.Id,
                    FullName            = profile.FullName,
                    Email               = profile.Email,
                    Phone               = profile.Phone,
                    Country             = profile.Country,
                    PreferredLocation   = profile.PreferredLocation,
                    LocationType        = profile.LocationType,
                    MinExperienceYears  = profile.MinExperienceYears,
                    MaxExperienceYears  = profile.MaxExperienceYears,
                    PreferredRoles      = profile.PreferredRoles,
                    Skills              = profile.Skills,
                    Education           = profile.Education,
                    Summary             = profile.Summary,
                    ResumeFileName      = profile.ResumeFileName,
                    CreatedAt           = profile.CreatedAt,
                    UpdatedAt           = profile.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing resume: {ex.Message}");
            }
        }
    }
}
