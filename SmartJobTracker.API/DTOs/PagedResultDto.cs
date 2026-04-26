namespace SmartJobTracker.API.DTOs
{
    /// <summary>
    /// Generic pagination wrapper for API responses.
    /// </summary>
    /// <typeparam name="T">Type of items in the paged result</typeparam>
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
