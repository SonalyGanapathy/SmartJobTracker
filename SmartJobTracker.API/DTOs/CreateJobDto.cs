namespace SmartJobTracker.API.DTOs
{
    public class CreateJobDto
    {
        public string Company { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
    }
}