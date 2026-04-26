using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;

namespace SmartJobTracker.API.Controllers
{
    [Route("api/saved-jobs")]
    public class SavedJobsController : ApiControllerBase
    {
        private readonly AppDbContext _context;

        public SavedJobsController(AppDbContext context) => _context = context;

        /// <summary>Get all saved jobs for the current logged-in user.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<SavedJobDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSavedJobs()
        {
            var userId = GetCurrentUserId();
            var savedJobs = await _context.SavedJobs
                .Where(s => s.UserProfileId == userId)
                .Include(s => s.JobListing)
                .OrderByDescending(s => s.SavedDate)
                .ToListAsync();

            return Ok(savedJobs.Select(MapToDto).ToList());
        }

        /// <summary>Save a job for later review.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(SavedJobDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveJob([FromBody] CreateSavedJobDto createDto)
        {
            var userId = GetCurrentUserId();

            var job = await _context.JobListings.FindAsync(createDto.JobListingId);
            if (job == null) return NotFound("Job listing not found");

            var alreadySaved = await _context.SavedJobs
                .AnyAsync(s => s.JobListingId == createDto.JobListingId && s.UserProfileId == userId);
            if (alreadySaved) return BadRequest("Job already saved");

            var savedJob = new SavedJob
            {
                JobListingId  = createDto.JobListingId,
                UserProfileId = userId,
                SavedDate     = DateTime.UtcNow,
                Notes         = createDto.Notes
            };

            _context.SavedJobs.Add(savedJob);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSavedJobs), new { id = savedJob.Id }, MapToDto(savedJob));
        }

        /// <summary>Remove a job from saved list.</summary>
        [HttpDelete("{jobListingId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnsaveJob(int jobListingId)
        {
            var userId = GetCurrentUserId();
            var savedJob = await _context.SavedJobs
                .FirstOrDefaultAsync(s => s.JobListingId == jobListingId && s.UserProfileId == userId);

            if (savedJob == null) return NotFound("Saved job not found");

            _context.SavedJobs.Remove(savedJob);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static SavedJobDto MapToDto(SavedJob saved) => new()
        {
            Id            = saved.Id,
            JobListingId  = saved.JobListingId,
            UserProfileId = saved.UserProfileId,
            SavedDate     = saved.SavedDate,
            Notes         = saved.Notes,
            JobListing = saved.JobListing == null ? null : new JobListingDto
            {
                Id           = saved.JobListing.Id,
                Title        = saved.JobListing.Title,
                Company      = saved.JobListing.Company,
                Location     = saved.JobListing.Location,
                JobType      = saved.JobListing.JobType,
                Description  = saved.JobListing.Description,
                Requirements = saved.JobListing.Requirements,
                SalaryMin    = saved.JobListing.SalaryMin,
                SalaryMax    = saved.JobListing.SalaryMax,
                Currency     = saved.JobListing.Currency,
                Source       = saved.JobListing.Source,
                SourceUrl    = saved.JobListing.SourceUrl,
                PostedDate   = saved.JobListing.PostedDate,
                IsEasyApply  = saved.JobListing.IsEasyApply,
                MatchScore   = saved.JobListing.MatchScore,
                Tags         = saved.JobListing.Tags,
                CreatedAt    = saved.JobListing.CreatedAt
            }
        };
    }
}
