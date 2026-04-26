using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;

namespace SmartJobTracker.API.Controllers
{
    [Route("api/[controller]")]
    public class DashboardController : ApiControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context) => _context = context;

        /// <summary>Dashboard statistics for the current logged-in user.</summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userId = GetCurrentUserId();
            var stats = new DashboardStatsDto();

            var applications = await _context.JobApplications
                .Where(a => a.UserProfileId == userId)
                .Include(a => a.JobListing)
                .ToListAsync();

            stats.TotalApplied    = applications.Count;
            stats.TotalInterviews = applications.Count(a => a.Status == "Interviewing");
            stats.TotalOffers     = applications.Count(a => a.Status == "Offered");
            stats.TotalRejected   = applications.Count(a => a.Status == "Rejected");

            stats.TotalSaved = await _context.SavedJobs
                .Where(s => s.UserProfileId == userId)
                .CountAsync();

            stats.RecentApplications = applications
                .OrderByDescending(a => a.AppliedDate)
                .Take(5)
                .Select(MapAppToDto)
                .ToList();

            stats.TopMatchedJobs = await _context.JobListings
                .Where(j => j.MatchScore.HasValue)
                .OrderByDescending(j => j.MatchScore)
                .Take(5)
                .Select(j => new JobListingDto
                {
                    Id = j.Id, Title = j.Title, Company = j.Company,
                    Location = j.Location, JobType = j.JobType,
                    Description = j.Description, Requirements = j.Requirements,
                    SalaryMin = j.SalaryMin, SalaryMax = j.SalaryMax,
                    Currency = j.Currency, Source = j.Source, SourceUrl = j.SourceUrl,
                    PostedDate = j.PostedDate, IsEasyApply = j.IsEasyApply,
                    MatchScore = j.MatchScore, Tags = j.Tags, CreatedAt = j.CreatedAt
                })
                .ToListAsync();

            return Ok(stats);
        }

        private static JobApplicationDto MapAppToDto(JobApplication app) => new()
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
                Id = app.JobListing.Id, Title = app.JobListing.Title,
                Company = app.JobListing.Company, Location = app.JobListing.Location,
                JobType = app.JobListing.JobType, Description = app.JobListing.Description,
                Requirements = app.JobListing.Requirements,
                SalaryMin = app.JobListing.SalaryMin, SalaryMax = app.JobListing.SalaryMax,
                Currency = app.JobListing.Currency, Source = app.JobListing.Source,
                SourceUrl = app.JobListing.SourceUrl, PostedDate = app.JobListing.PostedDate,
                IsEasyApply = app.JobListing.IsEasyApply, MatchScore = app.JobListing.MatchScore,
                Tags = app.JobListing.Tags, CreatedAt = app.JobListing.CreatedAt
            }
        };
    }
}
