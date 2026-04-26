namespace SmartJobTracker.API.DTOs
{
    public class UpdateApplicationStatusDto
    {
        /// <summary>Applied, Screening, Interviewing, Offered, Rejected, Withdrawn</summary>
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
