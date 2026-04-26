namespace SmartJobTracker.API.DTOs
{
    public class TrackExternalApplicationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Source { get; set; }
        public string? ApplyUrl { get; set; }
        public string? JobType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? Currency { get; set; }
        public string? Skills { get; set; }
        public int? MatchScore { get; set; }
        public int? AiConfidenceScore { get; set; }
        public string? VisaSponsorshipChance { get; set; }
        public DateTime? JobPostedDate { get; set; }
        public string? CoverNote { get; set; }
        public string? RecruiterMessage { get; set; }
    }

    public class ExternalJobApplicationDto
    {
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Source { get; set; }
        public string? ApplyUrl { get; set; }
        public string? JobType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? Currency { get; set; }
        public string? Skills { get; set; }
        public int? MatchScore { get; set; }
        public int? AiConfidenceScore { get; set; }
        public string? VisaSponsorshipChance { get; set; }
        public DateTime? JobPostedDate { get; set; }
        public string Status { get; set; } = "Applied";
        public DateTime AppliedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public string? CoverNote { get; set; }
        public string? RecruiterMessage { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateExternalApplicationStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
