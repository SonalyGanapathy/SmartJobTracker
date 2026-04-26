namespace SmartJobTracker.API.Entities
{
    /// <summary>
    /// Represents a job from AI Job Search that a user has bookmarked for later.
    /// Unlike SavedJob (internal DB jobs only), this stores external job data inline.
    /// </summary>
    public class ExternalSavedJob
    {
        public int Id { get; set; }
        public int UserProfileId { get; set; } = 1;

        /// <summary>The external job's unique ID (from the job portal)</summary>
        public string ExternalJobId { get; set; } = "";

        public string Title { get; set; } = "";
        public string Company { get; set; } = "";
        public string Location { get; set; } = "";
        public string Source { get; set; } = "";
        public string ApplyUrl { get; set; } = "";
        public string? JobType { get; set; }
        public string? Salary { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? Currency { get; set; }

        /// <summary>Comma-separated skill tags</summary>
        public string? Skills { get; set; }

        public int MatchPercent { get; set; }
        public string? VisaSponsorshipChance { get; set; }
        public DateTime? PostedDate { get; set; }
        public string? Description { get; set; }
        public DateTime SavedDate { get; set; } = DateTime.UtcNow;
    }
}
