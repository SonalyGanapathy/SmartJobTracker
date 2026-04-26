namespace SmartJobTracker.API.DTOs
{
    // ── Request ───────────────────────────────────────────────────────────────────

    public class ClaudeJobSearchRequestDto
    {
        /// <summary>Candidate's current location (e.g. "India")</summary>
        public string CandidateLocation { get; set; } = "India";

        /// <summary>Years of experience</summary>
        public int ExperienceYears { get; set; } = 3;

        /// <summary>Target job roles</summary>
        public List<string> TargetRoles { get; set; } = new();

        /// <summary>Core skills</summary>
        public List<string> CoreSkills { get; set; } = new();

        /// <summary>Certifications</summary>
        public List<string> Certifications { get; set; } = new();

        /// <summary>Country to search jobs in (e.g. "Singapore")</summary>
        public string SearchCountry { get; set; } = "Singapore";

        /// <summary>City / region within that country (optional)</summary>
        public string SearchLocation { get; set; } = "";

        /// <summary>Max jobs to return (1–20)</summary>
        public int MaxJobs { get; set; } = 15;

        /// <summary>Only return jobs posted within this many days</summary>
        public int PostedWithinDays { get; set; } = 14;

        /// <summary>Portal filter — empty = all</summary>
        public List<string> JobPortals { get; set; } = new();
    }

    // ── Per-Job Result ────────────────────────────────────────────────────────────

    public class ClaudeJobResultDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Company { get; set; } = "";
        public string Location { get; set; } = "";
        public string? CompanyLogo { get; set; }
        public string Experience { get; set; } = "";
        public string? Salary { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string Currency { get; set; } = "SGD";

        /// <summary>Claude-determined match percentage (0–100)</summary>
        public int MatchPercent { get; set; }

        /// <summary>Claude's analysis of why this job fits the candidate</summary>
        public string MatchAnalysis { get; set; } = "";

        /// <summary>EP/S-Pass sponsorship likelihood: "High" | "Medium" | "Low"</summary>
        public string VisaSponsorshipChance { get; set; } = "Medium";
        public int SponsorshipScore { get; set; }

        public string ApplyUrl { get; set; } = "";
        public string Source { get; set; } = "";
        public int SourcePriority { get; set; } = 5;
        public bool IsTrustedAgency { get; set; }
        public bool IsEasyApply { get; set; }
        public DateTime? PostedDate { get; set; }
        public List<string> Skills { get; set; } = new();
        public string? JobType { get; set; }
        public string? Description { get; set; }

        // ── Claude-Generated Content ─────────────────────────────────────────────

        /// <summary>Tailored resume summary written by Claude</summary>
        public string TailoredResumeSummary { get; set; } = "";

        /// <summary>LinkedIn recruiter outreach message written by Claude</summary>
        public string RecruiterMessage { get; set; } = "";

        /// <summary>Personalised cover note written by Claude</summary>
        public string CoverNote { get; set; } = "";
    }

    // ── Top-Level Response ────────────────────────────────────────────────────────

    public class ClaudeJobSearchResponseDto
    {
        public List<ClaudeJobResultDto> Jobs { get; set; } = new();
        public int TotalFound { get; set; }
        public int TotalSearched { get; set; }
        public List<string> SourcesUsed { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string SearchSummary { get; set; } = "";

        /// <summary>Claude model used for analysis</summary>
        public string Model { get; set; } = "";

        /// <summary>The search queries Claude generated</summary>
        public List<string> GeneratedQueries { get; set; } = new();
    }
}
