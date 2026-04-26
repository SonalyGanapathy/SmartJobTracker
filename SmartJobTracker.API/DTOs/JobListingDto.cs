namespace SmartJobTracker.API.DTOs
{
    public class JobListingDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? JobType { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string? Currency { get; set; }
        public string? Source { get; set; }
        public string? SourceUrl { get; set; }
        public DateTime PostedDate { get; set; }
        public bool IsEasyApply { get; set; }
        public int? MatchScore { get; set; }
        public string? Tags { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
