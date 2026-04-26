namespace SmartJobTracker.API.Entities
{
    /// <summary>
    /// Represents a job listing saved by a user for later review.
    /// </summary>
    public class SavedJob
    {
        public int Id { get; set; }
        public int JobListingId { get; set; }
        public int UserProfileId { get; set; }
        public DateTime SavedDate { get; set; } = DateTime.UtcNow;
        /// <summary>Optional notes about why this job was saved</summary>
        public string? Notes { get; set; }

        // Navigation properties
        public JobListing? JobListing { get; set; }
        public UserProfile? UserProfile { get; set; }
    }
}
