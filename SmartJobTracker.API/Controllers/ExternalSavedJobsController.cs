using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.Entities;

namespace SmartJobTracker.API.Controllers
{
    /// <summary>
    /// Save / unsave external jobs found via AI Job Search.
    /// Stores the full job snapshot so saved jobs can be viewed without re-searching.
    /// </summary>
    [Route("api/external-saved-jobs")]
    public class ExternalSavedJobsController : ApiControllerBase
    {
        private readonly AppDbContext _context;

        public ExternalSavedJobsController(AppDbContext context) => _context = context;

        /// <summary>Get all saved external jobs for the current user.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ExternalSavedJob>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            var jobs = await _context.ExternalSavedJobs
                .Where(j => j.UserProfileId == userId)
                .OrderByDescending(j => j.SavedDate)
                .ToListAsync();
            return Ok(jobs);
        }

        /// <summary>Check if a specific external job is already saved.</summary>
        [HttpGet("check")]
        public async Task<IActionResult> Check([FromQuery] string externalJobId)
        {
            var userId = GetCurrentUserId();
            var exists = await _context.ExternalSavedJobs
                .AnyAsync(j => j.UserProfileId == userId && j.ExternalJobId == externalJobId);
            return Ok(new { isSaved = exists });
        }

        /// <summary>Save an external job.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ExternalSavedJob), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Save([FromBody] ExternalSavedJob job)
        {
            if (string.IsNullOrWhiteSpace(job.Title) || string.IsNullOrWhiteSpace(job.Company))
                return BadRequest("Title and Company are required.");

            var userId = GetCurrentUserId();

            var existing = await _context.ExternalSavedJobs
                .FirstOrDefaultAsync(j => j.UserProfileId == userId &&
                    (j.ExternalJobId == job.ExternalJobId ||
                     (j.Title == job.Title && j.Company == job.Company)));
            if (existing != null)
                return BadRequest("Job already saved.");

            job.UserProfileId = userId;
            job.SavedDate     = DateTime.UtcNow;

            _context.ExternalSavedJobs.Add(job);
            await _context.SaveChangesAsync();
            return Ok(job);
        }

        /// <summary>Remove a saved external job by its DB ID.</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = GetCurrentUserId();
            var job = await _context.ExternalSavedJobs.FindAsync(id);
            if (job == null || job.UserProfileId != userId)
                return NotFound();

            _context.ExternalSavedJobs.Remove(job);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
