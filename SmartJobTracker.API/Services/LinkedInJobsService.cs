using System.Text.Json;
using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Fetches real-time LinkedIn job listings via the linkedin-jobs-api2 RapidAPI.
    ///
    /// API:  https://rapidapi.com/jaypat87/api/linkedin-jobs-api2
    /// Host: linkedin-jobs-api2.p.rapidapi.com
    /// Key config: "LinkedIn:ApiKey" in appsettings.json
    ///
    /// Endpoints tried in order (most results → fewest):
    ///   GET /active-jb-7d   — jobs posted in the last 7 days  (primary)
    ///   GET /active-jb-24h  — last 24 hours  (fallback)
    ///   GET /active-jb-1h   — last 1 hour    (last-resort fallback)
    ///
    /// Query params: title, location, country, limit, offset, description_type
    /// </summary>
    public class LinkedInJobsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<LinkedInJobsService> _logger;

        private const string RapidApiHost = "linkedin-jobs-api2.p.rapidapi.com";

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

            // Try longest window first (most results), fall back to shorter windows
            foreach (var endpoint in new[] { "active-jb-7d", "active-jb-24h", "active-jb-1h" })
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

                // linkedin-jobs-api2 uses "organization" for company name
                var company = GetString(item, "organization")
                           ?? GetString(item, "company")
                           ?? GetString(item, "company_name")
                           ?? GetString(item, "companyName")
                           ?? "Unknown Company";

                var applyUrl = GetString(item, "url")
                            ?? GetString(item, "linkedin_url")
                            ?? GetString(item, "job_url")
                            ?? GetString(item, "jobUrl")
                            ?? GetString(item, "apply_url")
                            ?? "";
                if (string.IsNullOrWhiteSpace(applyUrl)) return null;

                // linkedin-jobs-api2 uses "description_text" or "description_html"
                var description = GetString(item, "description_text")
                               ?? GetString(item, "description")
                               ?? GetString(item, "job_description")
                               ?? "";

                // Location: may be string or array in "locations_raw"
                string location = searchCountry;
                if (item.TryGetProperty("locations_raw", out var locArr) &&
                    locArr.ValueKind == JsonValueKind.Array &&
                    locArr.GetArrayLength() > 0)
                {
                    var firstLoc = locArr.EnumerateArray().FirstOrDefault();
                    if (firstLoc.ValueKind == JsonValueKind.String)
                        location = firstLoc.GetString() ?? searchCountry;
                    else if (firstLoc.TryGetProperty("name", out var locName))
                        location = locName.GetString() ?? searchCountry;
                }
                else
                {
                    location = GetString(item, "location")
                            ?? GetString(item, "job_location")
                            ?? searchCountry;
                }

                // Salary — linkedin-jobs-api2 nests it as { "salary": { "min_value": x, "max_value": y, "currency": "SGD" } }
                decimal? salaryMin = null, salaryMax = null;
                if (item.TryGetProperty("salary", out var salaryEl) && salaryEl.ValueKind == JsonValueKind.Object)
                {
                    salaryMin = GetDecimal(salaryEl, "min_value") ?? GetDecimal(salaryEl, "minimum");
                    salaryMax = GetDecimal(salaryEl, "max_value") ?? GetDecimal(salaryEl, "maximum");
                }
                if (!salaryMin.HasValue)
                {
                    salaryMin = GetDecimal(item, "salary_min") ?? GetDecimal(item, "min_salary");
                    salaryMax = GetDecimal(item, "salary_max") ?? GetDecimal(item, "max_salary");
                }
                if (!salaryMin.HasValue)
                {
                    var salStr = GetString(item, "salary_range") ?? "";
                    if (!string.IsNullOrWhiteSpace(salStr))
                        ParseSalaryString(salStr, out salaryMin, out salaryMax);
                }

                // Posted date — linkedin-jobs-api2 uses "date_posted" (ISO string)
                DateTime? postedDate = null;
                var dateStr = GetString(item, "date_posted")
                           ?? GetString(item, "posted_at")
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

                // employment_type may be a string or ["FULL_TIME"] array
                var jobType = "Full-time";
                if (item.TryGetProperty("employment_type", out var empEl))
                {
                    if (empEl.ValueKind == JsonValueKind.Array && empEl.GetArrayLength() > 0)
                    {
                        var raw = empEl.EnumerateArray().First().GetString() ?? "";
                        jobType = raw switch { "FULL_TIME" => "Full-time", "PART_TIME" => "Part-time",
                                              "CONTRACT" => "Contract", "INTERN" => "Internship", _ => raw };
                    }
                    else if (empEl.ValueKind == JsonValueKind.String)
                        jobType = empEl.GetString() ?? "Full-time";
                }

                return new ExternalJobDto
                {
                    Id          = $"li_{id}",
                    Title       = title,
                    Company     = company,
                    CompanyLogo = GetString(item, "organization_logo") ?? GetString(item, "company_logo") ?? GetString(item, "logo"),
                    Location    = location,
                    JobType     = jobType,
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
