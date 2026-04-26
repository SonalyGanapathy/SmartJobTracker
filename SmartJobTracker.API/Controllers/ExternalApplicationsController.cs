using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;

namespace SmartJobTracker.API.Controllers
{
    /// <summary>
    /// Tracks applications to external jobs (LinkedIn, Indeed, etc.)
    /// that don't have an internal JobListing ID.
    /// </summary>
    [Route("api/external-applications")]
    public class ExternalApplicationsController : ApiControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExternalApplicationsController> _logger;

        public ExternalApplicationsController(AppDbContext context, ILogger<ExternalApplicationsController> logger)
        {
            _context = context;
            _logger  = logger;
        }

        /// <summary>Get all tracked external applications for the current user.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ExternalJobApplicationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            var apps = await _context.ExternalJobApplications
                .Where(a => a.UserProfileId == userId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return Ok(apps.Select(MapToDto));
        }

        /// <summary>
        /// Record a new application to an external job.
        /// Idempotent — same title+company returns the existing record (200).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ExternalJobApplicationDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Track([FromBody] TrackExternalApplicationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Company))
                return BadRequest("Title and Company are required.");

            var userId = GetCurrentUserId();

            var existing = await _context.ExternalJobApplications
                .FirstOrDefaultAsync(a => a.UserProfileId == userId &&
                                          a.Company == dto.Company &&
                                          a.Title   == dto.Title);
            if (existing != null)
                return Ok(MapToDto(existing));

            var app = new ExternalJobApplication
            {
                UserProfileId         = userId,
                Title                 = dto.Title,
                Company               = dto.Company,
                Location              = dto.Location,
                Source                = dto.Source,
                ApplyUrl              = dto.ApplyUrl,
                JobType               = dto.JobType,
                SalaryMin             = dto.SalaryMin,
                SalaryMax             = dto.SalaryMax,
                Currency              = dto.Currency,
                Skills                = dto.Skills,
                MatchScore            = dto.MatchScore,
                AiConfidenceScore     = dto.AiConfidenceScore,
                VisaSponsorshipChance = dto.VisaSponsorshipChance,
                JobPostedDate         = dto.JobPostedDate,
                CoverNote             = dto.CoverNote,
                RecruiterMessage      = dto.RecruiterMessage,
                Status                = "Applied",
                AppliedAt             = DateTime.UtcNow,
                LastUpdatedAt         = DateTime.UtcNow
            };

            _context.ExternalJobApplications.Add(app);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} tracked external application: {Title} @ {Company}",
                userId, app.Title, app.Company);

            return CreatedAtAction(nameof(GetAll), new { id = app.Id }, MapToDto(app));
        }

        /// <summary>Update status of an external application.</summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ExternalJobApplicationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateExternalApplicationStatusDto dto)
        {
            var userId = GetCurrentUserId();
            var app = await _context.ExternalJobApplications
                .FirstOrDefaultAsync(a => a.Id == id && a.UserProfileId == userId);

            if (app == null) return NotFound();

            var validStatuses = new[] { "Applied", "Screening", "Interviewing", "Offered", "Rejected", "Withdrawn" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest($"Invalid status. Valid: {string.Join(", ", validStatuses)}");

            app.Status        = dto.Status;
            if (!string.IsNullOrWhiteSpace(dto.Notes)) app.Notes = dto.Notes;
            app.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(MapToDto(app));
        }

        /// <summary>Delete a tracked external application.</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var app = await _context.ExternalJobApplications
                .FirstOrDefaultAsync(a => a.Id == id && a.UserProfileId == userId);

            if (app == null) return NotFound();

            _context.ExternalJobApplications.Remove(app);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Check if the current user has already applied to a specific job.</summary>
        [HttpGet("check")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> Check([FromQuery] string title, [FromQuery] string company)
        {
            var userId = GetCurrentUserId();
            var exists = await _context.ExternalJobApplications
                .AnyAsync(a => a.UserProfileId == userId &&
                               a.Company == company &&
                               a.Title   == title);
            return Ok(new { applied = exists });
        }

        private static ExternalJobApplicationDto MapToDto(ExternalJobApplication a) => new()
        {
            Id                    = a.Id,
            UserProfileId         = a.UserProfileId,
            Title                 = a.Title,
            Company               = a.Company,
            Location              = a.Location,
            Source                = a.Source,
            ApplyUrl              = a.ApplyUrl,
            JobType               = a.JobType,
            SalaryMin             = a.SalaryMin,
            SalaryMax             = a.SalaryMax,
            Currency              = a.Currency,
            Skills                = a.Skills,
            MatchScore            = a.MatchScore,
            AiConfidenceScore     = a.AiConfidenceScore,
            VisaSponsorshipChance = a.VisaSponsorshipChance,
            JobPostedDate         = a.JobPostedDate,
            Status                = a.Status,
            AppliedAt             = a.AppliedAt,
            LastUpdatedAt         = a.LastUpdatedAt,
            CoverNote             = a.CoverNote,
            RecruiterMessage      = a.RecruiterMessage,
            Notes                 = a.Notes
        };
    }
}
