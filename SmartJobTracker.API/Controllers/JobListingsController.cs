using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;
using SmartJobTracker.API.Services;

namespace SmartJobTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobListingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJobSearchService _jobSearchService;

        public JobListingsController(AppDbContext context, IJobSearchService jobSearchService)
        {
            _context = context;
            _jobSearchService = jobSearchService;
        }

        /// <summary>
        /// Search job listings with optional filters and pagination.
        /// Optionally applies match scoring against a user profile.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<JobListingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchJobs([FromQuery] JobSearchFilterDto filter, [FromQuery] int? userProfileId = null)
        {
            var result = await _jobSearchService.SearchJobsAsync(filter, userProfileId);
            return Ok(result);
        }

        /// <summary>
        /// Get a specific job listing by ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(JobListingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetJobListing(int id)
        {
            var job = await _context.JobListings.FindAsync(id);
            if (job == null)
                return NotFound("Job listing not found");

            var dto = MapToDto(job);
            return Ok(dto);
        }

        /// <summary>
        /// Create a new job listing. Used for seeding or manual entry.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(JobListingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateJobListing([FromBody] JobListingDto createDto)
        {
            if (string.IsNullOrWhiteSpace(createDto.Title) || string.IsNullOrWhiteSpace(createDto.Company))
                return BadRequest("Title and Company are required");

            var job = new JobListing
            {
                Title = createDto.Title,
                Company = createDto.Company,
                Location = createDto.Location,
                JobType = createDto.JobType,
                Description = createDto.Description,
                Requirements = createDto.Requirements,
                SalaryMin = createDto.SalaryMin,
                SalaryMax = createDto.SalaryMax,
                Currency = createDto.Currency,
                Source = createDto.Source,
                SourceUrl = createDto.SourceUrl,
                PostedDate = createDto.PostedDate == default ? DateTime.UtcNow : createDto.PostedDate,
                IsEasyApply = createDto.IsEasyApply,
                Tags = createDto.Tags,
                CreatedAt = DateTime.UtcNow
            };

            _context.JobListings.Add(job);
            await _context.SaveChangesAsync();

            var dto = MapToDto(job);
            return CreatedAtAction(nameof(GetJobListing), new { id = job.Id }, dto);
        }

        private JobListingDto MapToDto(JobListing job)
        {
            return new JobListingDto
            {
                Id = job.Id,
                Title = job.Title,
                Company = job.Company,
                Location = job.Location,
                JobType = job.JobType,
                Description = job.Description,
                Requirements = job.Requirements,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                Currency = job.Currency,
                Source = job.Source,
                SourceUrl = job.SourceUrl,
                PostedDate = job.PostedDate,
                IsEasyApply = job.IsEasyApply,
                MatchScore = job.MatchScore,
                Tags = job.Tags,
                CreatedAt = job.CreatedAt
            };
        }
    }
}
