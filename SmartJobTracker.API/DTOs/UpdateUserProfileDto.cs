namespace SmartJobTracker.API.DTOs
{
    public class UpdateUserProfileDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
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
    }
}
