using System.Text.Json;
using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Fetches real-time LinkedIn job listings via the linkedin-job-search-api RapidAPI.
    ///
    /// API: https://rapidapi.com/search/linkedin-job-search-api
    /// Host: linkedin-job-search-api.p.rapidapi.com
    /// Key config: "LinkedIn:ApiKey" in appsettings.json
    ///
    /// Endpoints used:
    ///   GET /active-jb-7d   — jobs posted in the last 7 days
    ///   GET /active-jb-24h  — fallback (last 24 hours)
    /// </summary>
    public class LinkedInJobsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<LinkedInJobsService> _logger;

        private const string RapidApiHost = "linkedin-job-search-api.p.rapidapi.com";

        private static readonly string[] TechSkills = {
            "C#", ".NET", "ASP.NET", "Angular", "React", "Vue", "TypeScript", "JavaScript",
            "Azure", "AWS", "Docker", "Kubernetes", "SQL Server", "PostgreSQL", "MongoDB",
            "Entity Framework", "Microservices", "REST API", "Python", "Java", "Node.js",
            "Git", "CI/CD", "DevOps", "Agile", "Scrum", "GraphQL", "Redis"
        };

        public LinkedInJobsService(HttpClient httpClient, IConfiguration config,
            ILogger<LinkedInJobsService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public bool IsConfigured()
        {
            var key = _config["LinkedIn:ApiKey"];
            return !string.IsNullOrWhiteSpace(key)
                && key != "YOUR_LINKEDIN_RAPIDAPI_KEY"
                && key != "YOUR_KEY";
        }

        /// <summary>
        /// Search LinkedIn jobs by keyword and country.
        /// Tries /active-jb-7d first; falls back to /active-jb-24h if empty.
        /// </summary>
        public async Task<List<ExternalJobDto>> SearchAsync(
            string keyword,
            string searchCountry = "Singapore",
            string searchLocation = "",
            int offset = 0)
        {
            var jobs = new List<ExternalJobDto>();
            var apiKey = _config["LinkedIn:ApiKey"];
            if (!IsConfigured()) return jobs;

            var location = string.IsNullOrWhiteSpace(searchLocation)
                ? searchCountry
                : $"{searchLocation}, {searchCountry}";

            // Try 7-day endpoint first (more results), then 24-hour fallback
            foreach (var endpoint in new[] { "active-jb-7d", "active-jb-24h" })
            {
                try
                {
                    var title = Uri.EscapeDataString(keyword);
                    var loc   = Uri.EscapeDataString(location);
                    var url   = $"https://{RapidApiHost}/{endpoint}" +
                                $"?limit=50&offset={offset}&description_type=text" +
                                $"&title={title}&location={loc}";

                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("x-rapidapi-key", apiKey);
                    request.Headers.Add("x-rapidapi-host", RapidApiHost);
                    request.Headers.TryAddWithoutValidation("Content-Type", "application/json");

                    _logger.LogInformation("[LinkedIn] GET {Url}", url);

                    var response = await _httpClient.SendAsync(request);
                    var content  = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation("[LinkedIn] Status={Status} | Preview={Body}",
                        (int)response.StatusCode,
                        content.Length > 300 ? content[..300] : content);

                    if (!response.IsSuccessStatusCode)
                    {
                        if ((int)response.StatusCode == 429)
                            _logger.LogWarning("[LinkedIn] Rate limit for '{Keyword}' on {Endpoint}", keyword, endpoint);
                        else
                            _logger.LogError("[LinkedIn] FAILED {Status} for '{Keyword}': {Body}",
                                response.StatusCode, keyword, content);
                        continue;
                    }

                    using var json = JsonDocument.Parse(content);
                    var root = json.RootElement;

                    // API may return array directly or wrapped in { "data": [...] } / { "jobs": [...] }
                    JsonElement arr = default;
                    if (root.ValueKind == JsonValueKind.Array)
                        arr = root;
                    else
                        foreach (var prop in new[] { "data", "jobs", "results" })
                            if (root.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.Array)
                            { arr = el; break; }

                    if (arr.ValueKind != JsonValueKind.Array)
                    {
                        _logger.LogWarning("[LinkedIn] Unexpected response shape for '{Keyword}' ({Endpoint}): {Body}",
                            keyword, endpoint, content.Length > 200 ? content[..200] : content);
                        continue;
                    }

                    foreach (var item in arr.EnumerateArray())
                    {
                        var job = ParseJob(item, searchCountry);
                        if (job != null) jobs.Add(job);
                    }

                    _logger.LogInformation("[LinkedIn] Parsed {Count} jobs for '{Keyword}' via {Endpoint}",
                        jobs.Count, keyword, endpoint);

                    if (jobs.Count > 0) break; // success — skip fallback endpoint
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogError("[LinkedIn] TIMEOUT for '{Keyword}': {Message}", keyword, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[LinkedIn] ERROR for '{Keyword}'", keyword);
                }
            }

            return jobs;
        }

        private ExternalJobDto? ParseJob(JsonElement item, string searchCountry)
        {
            try
            {
                var title = GetString(item, "title")
                         ?? GetString(item, "job_title")
                         ?? GetString(item, "jobTitle");
                if (string.IsNullOrWhiteSpace(title)) return null;

                var company = GetString(item, "company")
                           ?? GetString(item, "company_name")
                           ?? GetString(item, "companyName")
                           ?? "Unknown Company";

                var applyUrl = GetString(item, "url")
                            ?? GetString(item, "job_url")
                            ?? GetString(item, "jobUrl")
                            ?? GetString(item, "linkedin_url")
                            ?? GetString(item, "apply_url")
                            ?? "";
                if (string.IsNullOrWhiteSpace(applyUrl)) return null;

                var description = GetString(item, "description")
                               ?? GetString(item, "job_description")
                               ?? "";

                var location = GetString(item, "location")
                            ?? GetString(item, "job_location")
                            ?? searchCountry;

                // Salary fields
                decimal? salaryMin = GetDecimal(item, "salary_min") ?? GetDecimal(item, "min_salary");
                decimal? salaryMax = GetDecimal(item, "salary_max") ?? GetDecimal(item, "max_salary");
                if (!salaryMin.HasValue)
                {
                    var salStr = GetString(item, "salary") ?? GetString(item, "salary_range") ?? "";
                    if (!string.IsNullOrWhiteSpace(salStr))
                        ParseSalaryString(salStr, out salaryMin, out salaryMax);
                }

                // Posted date
                DateTime? postedDate = null;
                var dateStr = GetString(item, "posted_at")
                           ?? GetString(item, "date_posted")
                           ?? GetString(item, "postedAt")
                           ?? GetString(item, "date");
                if (!string.IsNullOrWhiteSpace(dateStr))
                    postedDate = ParseDate(dateStr);

                // Skills from description
                var descLower = description.ToLower();
                var skills = TechSkills.Where(s => descLower.Contains(s.ToLower())).Take(8).ToList();
                if (skills.Count == 0)
                    skills = TechSkills.Where(s => title.Contains(s, StringComparison.OrdinalIgnoreCase)).Take(4).ToList();

                // Explicit skills array
                if (item.TryGetProperty("skills", out var skillsEl) && skillsEl.ValueKind == JsonValueKind.Array)
                    foreach (var s in skillsEl.EnumerateArray())
                    {
                        var sk = s.ValueKind == JsonValueKind.String ? s.GetString() : GetString(s, "name");
                        if (!string.IsNullOrEmpty(sk) && !skills.Contains(sk)) skills.Add(sk);
                    }

                var id = GetString(item, "id") ?? GetString(item, "job_id") ?? Guid.NewGuid().ToString();
                var currency = searchCountry switch
                {
                    "Singapore"                  => "SGD",
                    "India"                      => "INR",
                    "United Kingdom" or "UK"     => "GBP",
                    "Australia"                  => "AUD",
                    "Canada"                     => "CAD",
                    "Germany" or "France"
                        or "Netherlands"         => "EUR",
                    "UAE"                        => "AED",
                    _                            => "USD"
                };

                return new ExternalJobDto
                {
                    Id          = $"li_{id}",
                    Title       = title,
                    Company     = company,
                    CompanyLogo = GetString(item, "company_logo") ?? GetString(item, "logo"),
                    Location    = location,
                    JobType     = GetString(item, "employment_type")
                               ?? GetString(item, "job_type")
                               ?? "Full-time",
                    Description = description.Length > 600 ? description[..600] + "…" : description,
                    SalaryMin   = salaryMin,
                    SalaryMax   = salaryMax,
                    Currency    = currency,
                    Source      = "LinkedIn",
                    ApplyUrl    = applyUrl,
                    PostedDate  = postedDate,
                    IsEasyApply = applyUrl.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase),
                    Skills      = skills
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug("[LinkedIn] Parse error: {Message}", ex.Message);
                return null;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void ParseSalaryString(string salary, out decimal? min, out decimal? max)
        {
            min = null; max = null;
            var cleaned = System.Text.RegularExpressions.Regex.Replace(salary, @"[^\d\-–\.]", " ").Trim();
            var parts   = System.Text.RegularExpressions.Regex.Split(cleaned, @"[\-–]");
            var numbers = parts.Select(p => p.Trim())
                               .Where(p => !string.IsNullOrWhiteSpace(p))
                               .Select(p => decimal.TryParse(p, out var v) ? v : (decimal?)null)
                               .Where(v => v.HasValue && v > 0)
                               .ToList();
            if (numbers.Count >= 2) { min = numbers[0]; max = numbers[1]; }
            else if (numbers.Count == 1) min = numbers[0];
            if (min.HasValue && min > 50000) min /= 12;
            if (max.HasValue && max > 50000) max /= 12;
        }

        private static DateTime? ParseDate(string dateStr)
        {
            if (DateTime.TryParse(dateStr, out var dt)) return dt;
            var now   = DateTime.UtcNow;
            var lower = dateStr.ToLower();
            if (lower.Contains("hour") || lower.Contains("minute")) return now;
            if (lower.Contains("yesterday")) return now.AddDays(-1);
            var match = System.Text.RegularExpressions.Regex.Match(lower, @"(\d+)\s*(day|week|month)");
            if (match.Success)
            {
                var n = int.Parse(match.Groups[1].Value);
                return match.Groups[2].Value switch
                {
                    "day"   => now.AddDays(-n),
                    "week"  => now.AddDays(-n * 7),
                    "month" => now.AddMonths(-n),
                    _       => null
                };
            }
            return null;
        }

        private static string? GetString(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }

        private static decimal? GetDecimal(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Number
                && prop.TryGetDecimal(out var val))
                return val;
            return null;
        }
    }
}
