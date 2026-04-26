using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;

namespace SmartJobTracker.API.Controllers
{
    [Route("api/[controller]")]
    public class ProfileController : ApiControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Get the current logged-in user's profile.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var profile = await _context.UserProfiles.FindAsync(userId);
            if (profile == null)
                return NotFound("User profile not found.");

            return Ok(MapToDto(profile));
        }

        /// <summary>Update the current logged-in user's profile.</summary>
        [HttpPut]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            var userId = GetCurrentUserId();
            var profile = await _context.UserProfiles.FindAsync(userId);
            if (profile == null)
                return NotFound("User profile not found.");

            if (!string.IsNullOrWhiteSpace(updateDto.FullName))
                profile.FullName = updateDto.FullName;
            if (!string.IsNullOrWhiteSpace(updateDto.Email))
                profile.Email = updateDto.Email;
            if (updateDto.Phone != null)
                profile.Phone = updateDto.Phone;
            if (updateDto.Country != null)
                profile.Country = updateDto.Country;
            if (updateDto.PreferredLocation != null)
                profile.PreferredLocation = updateDto.PreferredLocation;
            if (updateDto.LocationType != null)
                profile.LocationType = updateDto.LocationType;
            if (updateDto.MinExperienceYears.HasValue)
                profile.MinExperienceYears = updateDto.MinExperienceYears;
            if (updateDto.MaxExperienceYears.HasValue)
                profile.MaxExperienceYears = updateDto.MaxExperienceYears;
            if (updateDto.PreferredRoles != null)
                profile.PreferredRoles = updateDto.PreferredRoles;
            if (updateDto.Skills != null)
                profile.Skills = updateDto.Skills;
            if (updateDto.Education != null)
                profile.Education = updateDto.Education;
            if (updateDto.Summary != null)
                profile.Summary = updateDto.Summary;

            profile.UpdatedAt = DateTime.UtcNow;
            _context.UserProfiles.Update(profile);
            await _context.SaveChangesAsync();

            return Ok(MapToDto(profile));
        }

        private static UserProfileDto MapToDto(UserProfile profile) => new()
        {
            Id = profile.Id,
            FullName = profile.FullName,
            Email = profile.Email,
            Phone = profile.Phone,
            Country = profile.Country,
            PreferredLocation = profile.PreferredLocation,
            LocationType = profile.LocationType,
            MinExperienceYears = profile.MinExperienceYears,
            MaxExperienceYears = profile.MaxExperienceYears,
            PreferredRoles = profile.PreferredRoles,
            Skills = profile.Skills,
            Education = profile.Education,
            Summary = profile.Summary,
            ResumeFileName = profile.ResumeFileName,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
