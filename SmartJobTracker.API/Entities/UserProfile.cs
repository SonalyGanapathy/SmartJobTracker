namespace SmartJobTracker.API.Entities
{
    /// <summary>
    /// Represents a user's job seeker profile with skills, experience, and preferences.
    /// </summary>
    public class UserProfile
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Country { get; set; }
        public string? PreferredLocation { get; set; }
        /// <summary>Remote, Hybrid, or OnSite</summary>
        public string? LocationType { get; set; }
        public int? MinExperienceYears { get; set; }
        public int? MaxExperienceYears { get; set; }
        /// <summary>Comma-separated list of preferred job roles (e.g., "Backend Engineer,Full Stack Developer")</summary>
        public string? PreferredRoles { get; set; }
        /// <summary>Comma-separated list of skills (e.g., "C#,.NET,SQL,Azure")</summary>
        public string? Skills { get; set; }
        public string? Education { get; set; }
        /// <summary>Professional summary or bio</summary>
        public string? Summary { get; set; }
        /// <summary>Name of the uploaded resume file</summary>
        public string? ResumeFileName { get; set; }
        /// <summary>BCrypt hashed password for authentication.</summary>
        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<JobApplication>? JobApplications { get; set; }
        public ICollection<SavedJob>? SavedJobs { get; set; }
    }
}
