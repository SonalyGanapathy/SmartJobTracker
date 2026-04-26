namespace SmartJobTracker.API.Entities
{
    public class Job
    {
        public int Id { get; set; }
        public string Company { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime AppliedDate { get; set; }
    }
}