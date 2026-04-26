namespace SmartJobTracker.API.Entities
{
    /// <summary>
    /// Represents a job listing from external sources (LinkedIn, Indeed, Glassdoor, etc.).
    /// </summary>
    public class JobListing
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? Location { get; set; }
        /// <summary>Remote, Hybrid, OnSite, FullTime, PartTime, Contract, etc.</summary>
        public string? JobType { get; set; }
        public string? Description { get; set; }
        /// <summary>Job requirements as text</summary>
        public string? Requirements { get; set; }
        /// <summary>Minimum salary, if available</summary>
        public decimal? SalaryMin { get; set; }
        /// <summary>Maximum salary, if available</summary>
        public decimal? SalaryMax { get; set; }
        /// <summary>Salary currency (USD, GBP, INR, SGD, etc.)</summary>
        public string? Currency { get; set; }
        /// <summary>LinkedIn, Indeed, Glassdoor, Naukri, Other</summary>
        public string? Source { get; set; }
        /// <summary>URL to the job listing</summary>
        public string? SourceUrl { get; set; }
        public DateTime PostedDate { get; set; } = DateTime.UtcNow;
        /// <summary>Whether this job supports easy apply (one-click)</summary>
        public bool IsEasyApply { get; set; }
        /// <summary>Match score (0-100) calculated against user profile</summary>
        public int? MatchScore { get; set; }
        /// <summary>Comma-separated tags (e.g., "Backend,.NET,Senior,Remote")</summary>
        public string? Tags { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<JobApplication>? JobApplications { get; set; }
        public ICollection<SavedJob>? SavedJobs { get; set; }
    }
}
