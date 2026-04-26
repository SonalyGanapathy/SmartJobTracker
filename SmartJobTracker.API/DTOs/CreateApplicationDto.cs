namespace SmartJobTracker.API.DTOs
{
    public class CreateApplicationDto
    {
        public int JobListingId { get; set; }
        public int UserProfileId { get; set; }
        public string? CoverLetter { get; set; }
        public string? Notes { get; set; }
    }
}
