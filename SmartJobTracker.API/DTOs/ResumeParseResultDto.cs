namespace SmartJobTracker.API.DTOs
{
    public class ResumeParseResultDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public List<string> Skills { get; set; } = new();
        public List<ExperienceDto> Experience { get; set; } = new();
        public List<EducationDto> Education { get; set; } = new();
        public string? Summary { get; set; }
        public List<string> Keywords { get; set; } = new();
    }

    public class ExperienceDto
    {
        public string? Company { get; set; }
        public string? Role { get; set; }
        public string? Duration { get; set; }
        public string? Description { get; set; }
    }

    public class EducationDto
    {
        public string? Degree { get; set; }
        public string? Institution { get; set; }
        public string? Field { get; set; }
        public string? Year { get; set; }
    }
}
