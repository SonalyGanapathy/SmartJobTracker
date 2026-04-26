namespace SmartJobTracker.API.DTOs
{
    public class ExternalJobDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Company { get; set; } = "";
        public string? CompanyLogo { get; set; }
        public string Location { get; set; } = "Singapore";
        public string? JobType { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string Currency { get; set; } = "SGD";

        /// <summary>Source portal: LinkedIn, Indeed, MyCareersFuture, Glassdoor, etc.</summary>
        public string Source { get; set; } = "";

        /// <summary>Direct apply URL — clicking Apply takes user here.</summary>
        public string ApplyUrl { get; set; } = "";

        public DateTime? PostedDate { get; set; }
        public bool IsEasyApply { get; set; }
        public int MatchScore { get; set; }

        /// <summary>Source priority: 1=Company direct, 2=LinkedIn, 3=Indeed, 4=Glassdoor/JobStreet, 5=Glints/JobsDB, 6=Agency</summary>
        public int SourcePriority { get; set; }

        /// <summary>True if posted by a trusted recruitment agency (Michael Page, Robert Walters, Hays, JobPlus)</summary>
        public bool IsTrustedAgency { get; set; }

        public List<string> Skills { get; set; } = new();
        public string? Tags { get; set; }
    }

    public class ExternalJobSearchResultDto
    {
        public List<ExternalJobDto> Jobs { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public bool HasMore { get; set; }
        public List<string> SourcesUsed { get; set; } = new();
    }
}
