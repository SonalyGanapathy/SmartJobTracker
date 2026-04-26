namespace SmartJobTracker.API.DTOs
{
    public class JobSearchFilterDto
    {
        public string? SearchTerm { get; set; }
        public string? Location { get; set; }
        public string? JobType { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string? Source { get; set; }
        public bool? IsEasyApply { get; set; }
        public int? MinMatchScore { get; set; }
        /// <summary>Sort by: newest, salary, matchscore</summary>
        public string? SortBy { get; set; } = "newest";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
