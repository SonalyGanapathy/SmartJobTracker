namespace SmartJobTracker.API.DTOs
{
    // ── Request ──────────────────────────────────────────────────────────────────

    public class AIJobSearchRequestDto
    {
        /// <summary>Candidate's current location (e.g. "India")</summary>
        public string CandidateLocation { get; set; } = "India";

        /// <summary>Years of experience (e.g. 3)</summary>
        public int ExperienceYears { get; set; } = 3;

        /// <summary>Target job roles (e.g. [".NET Developer", "Backend Engineer"])</summary>
        public List<string> TargetRoles { get; set; } = new()
        {
            ".NET Developer", "Backend Engineer", "Full Stack Developer", "Software Engineer"
        };

        /// <summary>Core skills (e.g. ["ASP.NET Core", "C#", "SQL Server", "Azure"])</summary>
        public List<string> CoreSkills { get; set; } = new()
        {
            "ASP.NET Core", "C#", "SQL Server", "Angular", "Azure", "Microservices"
        };

        /// <summary>Certifications (e.g. ["AZ-400", "AZ-305", "AZ-104"])</summary>
        public List<string> Certifications { get; set; } = new();

        /// <summary>Target country to search jobs in (e.g. "Canada", "Singapore", "United States")</summary>
        public string SearchCountry { get; set; } = "Singapore";

        /// <summary>City or region within the target country (e.g. "Toronto", "Ontario", "Remote")</summary>
        public string SearchLocation { get; set; } = "";

        /// <summary>Max jobs to return (20–40)</summary>
        public int MaxJobs { get; set; } = 30;

        /// <summary>Only return jobs posted within this many days</summary>
        public int PostedWithinDays { get; set; } = 14;

        /// <summary>Primary search keyword override (optional)</summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Filter by job portals. Empty list = all portals.
        /// Accepted values: "LinkedIn", "Indeed", "Glassdoor", "Adzuna", "NodeFlair", "CareersGov"
        /// </summary>
        public List<string> JobPortals { get; set; } = new();

        /// <summary>Only return jobs with match percentage >= this value (0 = no filter)</summary>
        public int MinMatchPercent { get; set; } = 0;
    }

    // ── Per-Job Result ────────────────────────────────────────────────────────────

    public class AIJobResultDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Company { get; set; } = "";
        public string Location { get; set; } = "";
        public string? CompanyLogo { get; set; }

        /// <summary>Experience range shown on the listing (e.g. "2–5 years")</summary>
        public string Experience { get; set; } = "2–5 years";

        /// <summary>Formatted salary string (e.g. "SGD 7,000 – 10,000/mo")</summary>
        public string? Salary { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string Currency { get; set; } = "SGD";

        /// <summary>Profile match percentage (0–100)</summary>
        public int MatchPercent { get; set; }

        /// <summary>EP sponsorship likelihood: "High", "Medium", "Low"</summary>
        public string VisaSponsorshipChance { get; set; } = "Medium";

        /// <summary>Sponsorship chance score 0–100 used for badge color</summary>
        public int SponsorshipScore { get; set; }

        /// <summary>Direct apply link (never a search page)</summary>
        public string ApplyUrl { get; set; } = "";

        public string Source { get; set; } = "";
        public int SourcePriority { get; set; } = 5;
        public bool IsTrustedAgency { get; set; }
        public bool IsEasyApply { get; set; }
        public DateTime? PostedDate { get; set; }

        public List<string> Skills { get; set; } = new();
        public string? JobType { get; set; }
        public string? Description { get; set; }

        // ── AI-Generated Content ───────────────────────────────────────────────

        /// <summary>3–4 line resume summary tailored to this specific job</summary>
        public string TailoredResumeSummary { get; set; } = "";

        /// <summary>Short LinkedIn-style recruiter outreach message</summary>
        public string RecruiterMessage { get; set; } = "";

        /// <summary>3–5 line cover note for the application</summary>
        public string CoverNote { get; set; } = "";
    }

    // ── Companies Section ─────────────────────────────────────────────────────────

    public class CompanyHiringFromIndiaDto
    {
        public string Company { get; set; } = "";
        public string Industry { get; set; } = "";
        public string LogoInitial { get; set; } = "";

        /// <summary>True if company is known to actively hire Indian tech talent</summary>
        public bool HiresFromIndia { get; set; } = true;

        /// <summary>True if EP sponsorship is commonly offered</summary>
        public bool SponsorEP { get; set; } = true;

        /// <summary>Notes on EP sponsorship culture at this company</summary>
        public string EpNotes { get; set; } = "";

        /// <summary>Live job titles matching the user profile (from search results)</summary>
        public List<string> MatchingJobTitles { get; set; } = new();

        /// <summary>Direct apply links corresponding to MatchingJobTitles</summary>
        public List<string> MatchingJobLinks { get; set; } = new();

        /// <summary>Company careers page URL</summary>
        public string CareersUrl { get; set; } = "";
    }

    // ── Top-Level Result ──────────────────────────────────────────────────────────

    public class AIJobSearchResultDto
    {
        public List<AIJobResultDto> Jobs { get; set; } = new();
        public List<CompanyHiringFromIndiaDto> CompaniesHiringFromIndia { get; set; } = new();
        public int TotalFound { get; set; }
        public int TotalSearched { get; set; }
        public List<string> SourcesUsed { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string SearchSummary { get; set; } = "";
    }
}
