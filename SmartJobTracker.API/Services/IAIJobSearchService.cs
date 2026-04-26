using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    public interface IAIJobSearchService
    {
        /// <summary>
        /// Runs an AI-powered job search for Singapore roles based on the user profile.
        /// Returns real job listings enriched with tailored AI content per job,
        /// visa sponsorship scores, and a curated list of companies that hire from India.
        /// </summary>
        Task<AIJobSearchResultDto> SearchAsync(AIJobSearchRequestDto request);
    }
}
