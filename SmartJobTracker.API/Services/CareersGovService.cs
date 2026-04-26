using System.Text.Json;
using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Fetches jobs from Careers@Gov (careers.gov.sg) — the official Singapore
    /// Public Service job portal covering GovTech, IMDA, MOM, EDB, HDB, CPF,
    /// NUS, NTU, A*STAR and 150+ government agencies.
    /// No API key required — publicly accessible.
    /// </summary>
    public class CareersGovService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CareersGovService> _logger;

        private const string BaseUrl = "https://careers.gov.sg";

        public CareersGovService(HttpClient httpClient, ILogger<CareersGovService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<ExternalJobDto>> SearchAsync(string? keyword, int page = 1)
        {
            var jobs = new List<ExternalJobDto>();

            // Try the Careers@Gov search API
            try
            {
                var kw = Uri.EscapeDataString(keyword ?? "software engineer");
                var pageIndex = Math.Max(0, page - 1);

                // Careers@Gov uses a REST API behind their search UI
                var url = $"{BaseUrl}/api/listingController/search.action" +
                          $"?keyword={kw}&category=&agency=&employmentType=&page={pageIndex}&pageSize=20";

                _httpClient.DefaultRequestHeaders.Remove("Referer");
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://careers.gov.sg/");
                _httpClient.DefaultRequestHeaders.Remove("User-Agent");
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36");

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Careers@Gov returned {Status}", response.StatusCode);
                    // Fallback to v2 endpoint
                    jobs = await TryFallbackAsync(keyword, pageIndex);
                    return jobs;
                }

                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);

                // Try parsing results array
                JsonElement results = default;
                if (json.RootElement.TryGetProperty("data", out var data))
                    results = data;
                else if (json.RootElement.TryGetProperty("results", out var res))
                    results = res;
                else if (json.RootElement.TryGetProperty("listings", out var listings))
                    results = listings;

                if (results.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in results.EnumerateArray())
                    {
                        var job = ParseJob(item);
                        if (job != null) jobs.Add(job);
                    }
                }

                _logger.LogInformation("Careers@Gov returned {Count} jobs", jobs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Careers@Gov API error: {Message}", ex.Message);
                jobs = await TryFallbackAsync(keyword, page - 1);
            }

            return jobs;
        }

        private async Task<List<ExternalJobDto>> TryFallbackAsync(string? keyword, int page)
        {
            var jobs = new List<ExternalJobDto>();
            try
            {
                // Alternate endpoint pattern
                var kw = Uri.EscapeDataString(keyword ?? "software engineer");
                var url = $"{BaseUrl}/api/search?keyword={kw}&page={page}&pageSize=20&sortBy=relevance";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return jobs;

                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);

                JsonElement arr = default;
                foreach (var prop in new[] { "data", "results", "jobs", "listings" })
                {
                    if (json.RootElement.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.Array)
                    {
                        arr = el;
                        break;
                    }
                }

                if (arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        var job = ParseJob(item);
                        if (job != null) jobs.Add(job);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Careers@Gov fallback error: {Message}", ex.Message);
            }
            return jobs;
        }

        private ExternalJobDto? ParseJob(JsonElement item)
        {
            try
            {
                // Try various field name conventions used by gov portals
                var title = GetString(item, "jobTitle") ?? GetString(item, "title") ?? GetString(item, "position");
                if (string.IsNullOrEmpty(title)) return null;

                var agency = GetString(item, "agencyName") ?? GetString(item, "agency") ??
                             GetString(item, "ministry") ?? GetString(item, "organisation") ?? "Singapore Government";

                var jobId = GetString(item, "jobId") ?? GetString(item, "id") ??
                            GetString(item, "listingId") ?? Guid.NewGuid().ToString();

                var description = GetString(item, "description") ?? GetString(item, "jobDescription") ??
                                  GetString(item, "responsibilities") ?? "";

                var jobType = GetString(item, "employmentType") ?? GetString(item, "type") ?? "Full-time";

                // Salary
                decimal? salaryMin = GetDecimal(item, "salaryMin") ?? GetDecimal(item, "minimumSalary");
                decimal? salaryMax = GetDecimal(item, "salaryMax") ?? GetDecimal(item, "maximumSalary");

                // Skills
                var skills = new List<string>();
                foreach (var skillKey in new[] { "skills", "requirements", "keySkills" })
                {
                    if (item.TryGetProperty(skillKey, out var skillsEl))
                    {
                        if (skillsEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var s in skillsEl.EnumerateArray())
                            {
                                var sk = s.ValueKind == JsonValueKind.String
                                    ? s.GetString()
                                    : GetString(s, "name") ?? GetString(s, "skill");
                                if (!string.IsNullOrEmpty(sk)) skills.Add(sk);
                            }
                        }
                        break;
                    }
                }

                // Posted date
                DateTime? postedDate = null;
                var dateStr = GetString(item, "postedDate") ?? GetString(item, "createdDate") ??
                              GetString(item, "publishDate") ?? GetString(item, "datePosted");
                if (DateTime.TryParse(dateStr, out var dt)) postedDate = dt;

                // Apply URL — Careers@Gov listing page (no Singpass needed to VIEW, but to apply need Singpass)
                // We build a direct listing URL + also store jobId for LinkedIn alternative search
                var applyUrl = $"https://careers.gov.sg/careers/{jobId}";

                // Location within Singapore
                var location = GetString(item, "location") ?? GetString(item, "workLocation") ?? "Singapore";
                if (!location.Contains("Singapore", StringComparison.OrdinalIgnoreCase))
                    location = $"{location}, Singapore";

                return new ExternalJobDto
                {
                    Id = $"cgov_{jobId}",
                    Title = title,
                    Company = agency,
                    Location = location,
                    JobType = jobType,
                    Description = description.Length > 600 ? description[..600] + "…" : description,
                    SalaryMin = salaryMin,
                    SalaryMax = salaryMax,
                    Currency = "SGD",
                    Source = "Careers@Gov",
                    ApplyUrl = applyUrl,
                    PostedDate = postedDate,
                    IsEasyApply = false, // requires Singpass — handled in frontend
                    Skills = skills,
                    Tags = "government"
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Careers@Gov parse error: {Message}", ex.Message);
                return null;
            }
        }

        private static string? GetString(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }

        private static decimal? GetDecimal(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Number &&
                prop.TryGetDecimal(out var val))
                return val;
            return null;
        }
    }
}
