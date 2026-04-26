namespace SmartJobTracker.API.DTOs
{
    public class SavedJobDto
    {
        public int Id { get; set; }
        public int JobListingId { get; set; }
        public int UserProfileId { get; set; }
        public DateTime SavedDate { get; set; }
        public string? Notes { get; set; }
        public JobListingDto? JobListing { get; set; }
    }
}
