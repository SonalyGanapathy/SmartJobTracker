namespace SmartJobTracker.API.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalApplied { get; set; }
        public int TotalSaved { get; set; }
        public int TotalInterviews { get; set; }
        public int TotalOffers { get; set; }
        public int TotalRejected { get; set; }
        public List<JobApplicationDto> RecentApplications { get; set; } = new();
        public List<JobListingDto> TopMatchedJobs { get; set; } = new();
    }
}
