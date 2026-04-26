using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;
using System.Text.RegularExpressions;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Basic job matching service using keyword similarity.
    /// For production, consider machine learning or more sophisticated NLP.
    /// </summary>
    public class JobMatchingService : IJobMatchingService
    {
        public int CalculateMatchScore(JobListing job, UserProfile profile)
        {
            int score = 0;
            int maxScore = 100;

            // Extract keywords
            var jobKeywords = ExtractKeywords(job.Title, job.Description, job.Requirements);
            var profileSkills = (profile.Skills ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLower())
                .ToList();
            var profileRoles = (profile.PreferredRoles ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim().ToLower())
                .ToList();

            // Score skill matches (40 points max)
            int skillMatches = 0;
            foreach (var skill in profileSkills)
            {
                if (jobKeywords.Any(k => k.Contains(skill, StringComparison.OrdinalIgnoreCase)))
                {
                    skillMatches++;
                }
            }
            score += Math.Min((skillMatches * 10), 40);

            // Score role matches (30 points max)
            int roleMatches = 0;
            foreach (var role in profileRoles)
            {
                if (jobKeywords.Any(k => k.Contains(role, StringComparison.OrdinalIgnoreCase)) ||
                    job.Title.Contains(role, StringComparison.OrdinalIgnoreCase))
                {
                    roleMatches++;
                }
            }
            score += Math.Min((roleMatches * 10), 30);

            // Score location match (15 points)
            if (!string.IsNullOrEmpty(profile.PreferredLocation) && !string.IsNullOrEmpty(job.Location))
            {
                if (job.Location.Contains(profile.PreferredLocation, StringComparison.OrdinalIgnoreCase))
                {
                    score += 15;
                }
            }

            // Score location type match (10 points)
            if (!string.IsNullOrEmpty(profile.LocationType) && !string.IsNullOrEmpty(job.JobType))
            {
                if (job.JobType.Contains(profile.LocationType, StringComparison.OrdinalIgnoreCase))
                {
                    score += 10;
                }
            }

            // Score experience match (5 points)
            if (profile.MinExperienceYears.HasValue && profile.MaxExperienceYears.HasValue)
            {
                // Check if job title indicates seniority
                bool isSenior = job.Title.Contains("Senior", StringComparison.OrdinalIgnoreCase) ||
                               job.Title.Contains("Lead", StringComparison.OrdinalIgnoreCase);
                bool isJunior = job.Title.Contains("Junior", StringComparison.OrdinalIgnoreCase);

                if ((isSenior && profile.MaxExperienceYears >= 5) ||
                    (isJunior && profile.MinExperienceYears <= 3) ||
                    (!isSenior && !isJunior))
                {
                    score += 5;
                }
            }

            return Math.Min(score, maxScore);
        }

        public List<JobListingDto> RankJobs(List<JobListing> jobs, UserProfile profile)
        {
            var rankedJobs = new List<(JobListing job, int score)>();

            foreach (var job in jobs)
            {
                var score = CalculateMatchScore(job, profile);
                rankedJobs.Add((job, score));
            }

            // Sort by score descending, then by posted date
            rankedJobs = rankedJobs
                .OrderByDescending(x => x.score)
                .ThenByDescending(x => x.job.PostedDate)
                .ToList();

            // Convert to DTOs with match scores
            return rankedJobs.Select(x => new JobListingDto
            {
                Id = x.job.Id,
                Title = x.job.Title,
                Company = x.job.Company,
                Location = x.job.Location,
                JobType = x.job.JobType,
                Description = x.job.Description,
                Requirements = x.job.Requirements,
                SalaryMin = x.job.SalaryMin,
                SalaryMax = x.job.SalaryMax,
                Currency = x.job.Currency,
                Source = x.job.Source,
                SourceUrl = x.job.SourceUrl,
                PostedDate = x.job.PostedDate,
                IsEasyApply = x.job.IsEasyApply,
                MatchScore = x.score,
                Tags = x.job.Tags,
                CreatedAt = x.job.CreatedAt
            }).ToList();
        }

        private List<string> ExtractKeywords(string? title, string? description, string? requirements)
        {
            var text = $"{title} {description} {requirements}".ToLower();
            // Extract words (4+ chars, excluding common words)
            var keywords = Regex.Matches(text, @"\b[a-z]+\b")
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(w => w.Length >= 3)
                .Distinct()
                .ToList();

            return keywords;
        }
    }
}
