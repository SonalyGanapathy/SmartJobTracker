using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Service for searching and filtering job listings.
    /// </summary>
    public interface IJobSearchService
    {
        /// <summary>
        /// Search jobs with filters and optional user profile matching.
        /// </summary>
        /// <param name="filter">Search and filter criteria</param>
        /// <param name="userProfileId">Optional user profile ID for match scoring</param>
        /// <returns>Paged result of job listings</returns>
        Task<PagedResultDto<JobListingDto>> SearchJobsAsync(JobSearchFilterDto filter, int? userProfileId = null);
    }
}
