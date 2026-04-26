namespace SmartJobTracker.API.DTOs
{
    public class CreateSavedJobDto
    {
        public int JobListingId { get; set; }
        public int UserProfileId { get; set; }
        public string? Notes { get; set; }
    }
}
