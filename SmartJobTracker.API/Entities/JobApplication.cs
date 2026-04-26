namespace SmartJobTracker.API.Entities
{
    /// <summary>
    /// Represents a user's application to a job listing.
    /// </summary>
    public class JobApplication
    {
        public int Id { get; set; }
        public int JobListingId { get; set; }
        public int UserProfileId { get; set; }
        /// <summary>Applied, Screening, Interviewing, Offered, Rejected, Withdrawn</summary>
        public string Status { get; set; } = "Applied";
        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
        /// <summary>Optional internal notes about the application</summary>
        public string? Notes { get; set; }
        /// <summary>Cover letter text submitted with the application</summary>
        public string? CoverLetter { get; set; }
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public JobListing? JobListing { get; set; }
        public UserProfile? UserProfile { get; set; }
    }
}
