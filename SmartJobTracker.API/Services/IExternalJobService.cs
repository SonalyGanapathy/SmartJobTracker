using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    public interface IExternalJobService
    {
        /// <summary>
        /// Searches real-time jobs from external portals (JSearch + MyCareersFuture).
        /// Aggregates LinkedIn, Indeed, Glassdoor, company portals, and Singapore govt jobs.
        /// </summary>
        Task<ExternalJobSearchResultDto> SearchExternalJobsAsync(
            string searchCountry,
            string searchLocation,
            string? keyword = null,
            string? jobType = null,
            int page = 1,
            string? userSkills = null);
    }
}
