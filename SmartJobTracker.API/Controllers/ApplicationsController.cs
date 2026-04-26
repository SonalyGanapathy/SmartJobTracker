using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;

namespace SmartJobTracker.API.Controllers
{
    [Route("api/[controller]")]
    public class ApplicationsController : ApiControllerBase
    {
        private readonly AppDbContext _context;

        public ApplicationsController(AppDbContext context) => _context = context;

        /// <summary>Get all applications for the current logged-in user.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<JobApplicationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetApplications()
        {
            var userId = GetCurrentUserId();
            var applications = await _context.JobApplications
                .Where(a => a.UserProfileId == userId)
                .Include(a => a.JobListing)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            return Ok(applications.Select(MapToDto).ToList());
        }

        /// <summary>Apply to a job listing.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(JobApplicationDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationDto createDto)
        {
            var userId = GetCurrentUserId();

            var job = await _context.JobListings.FindAsync(createDto.JobListingId);
            if (job == null) return NotFound("Job listing not found");

            var alreadyApplied = await _context.JobApplications
                .AnyAsync(a => a.JobListingId == createDto.JobListingId && a.UserProfileId == userId);
            if (alreadyApplied) return BadRequest("Already applied to this job");

            var application = new JobApplication
            {
                JobListingId  = createDto.JobListingId,
                UserProfileId = userId,
                Status        = "Applied",
                AppliedDate   = DateTime.UtcNow,
                CoverLetter   = createDto.CoverLetter,
                Notes         = createDto.Notes,
                LastUpdatedAt = DateTime.UtcNow
            };

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetApplications), new { id = application.Id }, MapToDto(application));
        }

        /// <summary>Update the status of an application.</summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(JobApplicationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateApplicationStatus(int id, [FromBody] UpdateApplicationStatusDto updateDto)
        {
            var userId = GetCurrentUserId();
            var application = await _context.JobApplications
                .Include(a => a.JobListing)
                .FirstOrDefaultAsync(a => a.Id == id && a.UserProfileId == userId);

            if (application == null) return NotFound("Application not found");

            var validStatuses = new[] { "Applied", "Screening", "Interviewing", "Offered", "Rejected", "Withdrawn" };
            if (!validStatuses.Contains(updateDto.Status))
                return BadRequest($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");

            application.Status        = updateDto.Status;
            if (!string.IsNullOrWhiteSpace(updateDto.Notes)) application.Notes = updateDto.Notes;
            application.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(MapToDto(application));
        }

        /// <summary>Remove (withdraw) an application.</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> WithdrawApplication(int id)
        {
            var userId = GetCurrentUserId();
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id && a.UserProfileId == userId);

            if (application == null) return NotFound("Application not found");

            _context.JobApplications.Remove(application);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static JobApplicationDto MapToDto(JobApplication app) => new()
        {
            Id            = app.Id,
            JobListingId  = app.JobListingId,
            UserProfileId = app.UserProfileId,
            Status        = app.Status,
            AppliedDate   = app.AppliedDate,
            Notes         = app.Notes,
            CoverLetter   = app.CoverLetter,
            LastUpdatedAt = app.LastUpdatedAt,
            JobListing = app.JobListing == null ? null : new JobListingDto
            {
                Id           = app.JobListing.Id,
                Title        = app.JobListing.Title,
                Company      = app.JobListing.Company,
                Location     = app.JobListing.Location,
                JobType      = app.JobListing.JobType,
                Description  = app.JobListing.Description,
                Requirements = app.JobListing.Requirements,
                SalaryMin    = app.JobListing.SalaryMin,
                SalaryMax    = app.JobListing.SalaryMax,
                Currency     = app.JobListing.Currency,
                Source       = app.JobListing.Source,
                SourceUrl    = app.JobListing.SourceUrl,
                PostedDate   = app.JobListing.PostedDate,
                IsEasyApply  = app.JobListing.IsEasyApply,
                MatchScore   = app.JobListing.MatchScore,
                Tags         = app.JobListing.Tags,
                CreatedAt    = app.JobListing.CreatedAt
            }
        };
    }
}
