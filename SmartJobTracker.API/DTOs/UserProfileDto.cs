namespace SmartJobTracker.API.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Country { get; set; }
        public string? PreferredLocation { get; set; }
        public string? LocationType { get; set; }
        public int? MinExperienceYears { get; set; }
        public int? MaxExperienceYears { get; set; }
        public string? PreferredRoles { get; set; }
        public string? Skills { get; set; }
        public string? Education { get; set; }
        public string? Summary { get; set; }
        public string? ResumeFileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
