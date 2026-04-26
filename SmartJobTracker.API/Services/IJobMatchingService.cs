using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Service for matching jobs against user profiles.
    /// </summary>
    public interface IJobMatchingService
    {
        /// <summary>
        /// Calculate a match score (0-100) between a job and a user profile.
        /// </summary>
        /// <param name="job">Job listing to score</param>
        /// <param name="profile">User profile to match against</param>
        /// <returns>Match score from 0 to 100</returns>
        int CalculateMatchScore(JobListing job, UserProfile profile);

        /// <summary>
        /// Rank and score multiple jobs against a user profile.
        /// </summary>
        /// <param name="jobs">List of job listings to rank</param>
        /// <param name="profile">User profile to match against</param>
        /// <returns>Ranked and scored job DTOs</returns>
        List<JobListingDto> RankJobs(List<JobListing> jobs, UserProfile profile);
    }
}
