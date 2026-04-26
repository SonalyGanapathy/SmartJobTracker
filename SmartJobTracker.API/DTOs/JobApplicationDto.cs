namespace SmartJobTracker.API.DTOs
{
    public class JobApplicationDto
    {
        public int Id { get; set; }
        public int JobListingId { get; set; }
        public int UserProfileId { get; set; }
        public string Status { get; set; } = "Applied";
        public DateTime AppliedDate { get; set; }
        public string? Notes { get; set; }
        public string? CoverLetter { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public JobListingDto? JobListing { get; set; }
    }
}
