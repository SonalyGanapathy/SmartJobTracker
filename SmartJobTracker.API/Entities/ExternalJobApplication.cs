namespace SmartJobTracker.API.Entities
{
    /// <summary>
    /// Tracks applications to external jobs (LinkedIn, Indeed, MyCareersFuture, etc.)
    /// that don't exist as internal JobListing rows. Each row = one click-to-apply.
    /// </summary>
    public class ExternalJobApplication
    {
        public int Id { get; set; }

        /// <summary>Fixed to userId=1 for MVP; extend for multi-user auth later.</summary>
        public int UserProfileId { get; set; } = 1;

        // ── Job snapshot at time of apply ───────────────────────────────────
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Source { get; set; }          // LinkedIn, Indeed, MyCareersFuture, etc.
        public string? ApplyUrl { get; set; }
        public string? JobType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? Currency { get; set; }
        public string? Skills { get; set; }          // comma-separated
        public int? MatchScore { get; set; }
        public int? AiConfidenceScore { get; set; }
        public string? VisaSponsorshipChance { get; set; }
        public DateTime? JobPostedDate { get; set; }

        // ── Application tracking ────────────────────────────────────────────
        /// <summary>Applied, Screening, Interviewing, Offered, Rejected, Withdrawn</summary>
        public string Status { get; set; } = "Applied";
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CoverNote { get; set; }
        public string? RecruiterMessage { get; set; }
        public string? Notes { get; set; }
    }
}
