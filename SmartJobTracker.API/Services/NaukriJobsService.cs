using System.Text.Json;
using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Fetches real-time Naukri job listings via the Naukri RapidAPI.
    ///
    /// Setup:
    ///   1. Go to https://rapidapi.com/search/naukri and subscribe to "Naukri API"
    ///   2. Add your key to appsettings.json: "Naukri": { "ApiKey": "YOUR_KEY" }
    ///
    /// Only active for India searches (searchCountry = "India").
    ///
    /// Alternatively, configure a second JSearch key as "JSearch:ApiKey2" to use
    /// JSearch (which already indexes Naukri via Google for Jobs) without
    /// burning the primary key's quota.
    /// </summary>
    public class NaukriJobsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<NaukriJobsService> _logger;

        private static readonly string[] TechSkills = {
            "C#", ".NET", "ASP.NET", "Angular", "React", "TypeScript", "JavaScript",
            "Azure", "AWS", "Docker", "Kubernetes", "SQL Server", "PostgreSQL", "MongoDB",
            "Entity Framework", "Microservices", "REST API", "Python", "Java", "Node.js",
            "Spring Boot", "Hibernate", "Git", "CI/CD", "DevOps", "Agile", "Scrum"
        };

        public NaukriJobsService(HttpClient httpClient, IConfiguration config, ILogger<NaukriJobsService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public bool IsConfigured()
        {
            // Enabled if any of the three key options is set
            var naukriKey = _config["Naukri:ApiKey"];
            if (!string.IsNullOrWhiteSpace(naukriKey) && naukriKey != "YOUR_NAUKRI_RAPIDAPI_KEY")
                return true;

            var jsearchKey2 = _config["JSearch:ApiKey2"];
            if (!string.IsNullOrWhiteSpace(jsearchKey2) && jsearchKey2 != "YOUR_SECOND_JSEARCH_KEY")
                return true;

            // Last resort: fall back to primary JSearch key
            var jsearchKey = _config["JSearch:ApiKey"];
            return !string.IsNullOrWhiteSpace(jsearchKey) && jsearchKey != "YOUR_RAPIDAPI_KEY_HERE";
        }

        /// <summary>
        /// Search Naukri/India jobs. Falls back through:
        ///   1. Dedicated Naukri RapidAPI key ("Naukri:ApiKey")
        ///   2. Secondary JSearch key ("JSearch:ApiKey2")
        ///   3. Primary JSearch key ("JSearch:ApiKey") — shares quota with the main search
        /// </summary>
        public async Task<List<ExternalJobDto>> SearchAsync(
            string keyword,
            string searchLocation = "India")
        {
            var jobs = new List<ExternalJobDto>();

            // Try dedicated Naukri API first
            var naukriKey = _config["Naukri:ApiKey"];
            if (!string.IsNullOrWhiteSpace(naukriKey) && naukriKey != "YOUR_NAUKRI_RAPIDAPI_KEY")
            {
                return await SearchViaNaukriApiAsync(keyword, searchLocation, naukriKey);
            }

            // Fall back to secondary JSearch key if configured
            var jsearchKey2 = _config["JSearch:ApiKey2"];
            if (!string.IsNullOrWhiteSpace(jsearchKey2) && jsearchKey2 != "YOUR_SECOND_JSEARCH_KEY")
            {
                return await SearchViaJSearchAsync(keyword, searchLocation, jsearchKey2);
            }

            // Last resort: reuse primary JSearch key (shares quota)
            var jsearchKey = _config["JSearch:ApiKey"];
            if (!string.IsNullOrWhiteSpace(jsearchKey) && jsearchKey != "YOUR_RAPIDAPI_KEY_HERE")
            {
                _logger.LogInformation("Naukri: using primary JSearch key as fallback for India search");
                return await SearchViaJSearchAsync(keyword, searchLocation, jsearchKey);
            }

            _logger.LogDebug("Naukri: no API key configured (Naukri:ApiKey, JSearch:ApiKey2, or JSearch:ApiKey) — skipping");
            return jobs;
        }

        private async Task<List<ExternalJobDto>> SearchViaNaukriApiAsync(
            string keyword, string location, string apiKey)
        {
            var jobs = new List<ExternalJobDto>();
            try
            {
                var kw = Uri.EscapeDataString(keyword);
                var loc = Uri.EscapeDataString(location);
                var url = $"https://naukri.p.rapidapi.com/v1/jobs/search?q={kw}&location={loc}&noOfResults=25&start=1";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-RapidAPI-Key", apiKey);
                request.Headers.Add("X-RapidAPI-Host", "naukri.p.rapidapi.com");

                _logger.LogInformation("[Naukri] GET {Url}", url);

                var response = await _httpClient.SendAsync(request);
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var content = System.Text.Encoding.UTF8.GetString(bytes);

                _logger.LogInformation("[Naukri] Status={Status} | Preview={Body}",
                    (int)response.StatusCode,
                    content.Length > 300 ? content[..300] : content);

                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode == 429)
                        _logger.LogWarning("[Naukri] Rate limit hit for '{Keyword}'", keyword);
                    else
                        _logger.LogError("[Naukri] FAILED {Status} for '{Keyword}': {Body}",
                            response.StatusCode, keyword, content);
                    return jobs;
                }

                using var json = JsonDocument.Parse(content);
                var root = json.RootElement;

                // Try common response shapes
                JsonElement? jobsArray = null;
                if (root.ValueKind == JsonValueKind.Array)
                    jobsArray = root;
                else if (root.TryGetProperty("jobs", out var jEl) && jEl.ValueKind == JsonValueKind.Array)
                    jobsArray = jEl;
                else if (root.TryGetProperty("data", out var dEl) && dEl.ValueKind == JsonValueKind.Array)
                    jobsArray = dEl;
                else if (root.TryGetProperty("jobDetails", out var jdEl) && jdEl.ValueKind == JsonValueKind.Array)
                    jobsArray = jdEl;

                if (jobsArray == null)
                {
                    _logger.LogWarning("[Naukri] Unexpected response shape — Full body: {Body}",
                        content.Length > 500 ? content[..500] : content);
                    return jobs;
                }

                foreach (var item in jobsArray.Value.EnumerateArray())
                {
                    var job = ParseNaukriJob(item, keyword);
                    if (job != null) jobs.Add(job);
                }

                _logger.LogInformation("[Naukri] Parsed {Count} jobs for '{Keyword}'", jobs.Count, keyword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Naukri] ERROR for '{Keyword}'", keyword);
            }
            return jobs;
        }

        /// <summary>
        /// Uses JSearch (Google for Jobs) with a second API key — JSearch indexes Naukri,
        /// so results will include Naukri, LinkedIn, Indeed, Glassdoor, Shine, etc.
        /// Jobs are tagged with their actual publisher name.
        /// </summary>
        private async Task<List<ExternalJobDto>> SearchViaJSearchAsync(
            string keyword, string location, string apiKey)
        {
            var jobs = new List<ExternalJobDto>();
            try
            {
                var query = Uri.EscapeDataString($"{keyword} jobs in {location}");
                var url = $"https://jsearch.p.rapidapi.com/search?query={query}&page=1&num_pages=1&date_posted=week";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-RapidAPI-Key", apiKey);
                request.Headers.Add("X-RapidAPI-Host", "jsearch.p.rapidapi.com");

                _logger.LogInformation("[NaukriJSearch] GET {Url}", url);

                var response = await _httpClient.SendAsync(request);
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var content = System.Text.Encoding.UTF8.GetString(bytes);

                _logger.LogInformation("[NaukriJSearch] Status={Status} | Preview={Body}",
                    (int)response.StatusCode,
                    content.Length > 300 ? content[..300] : content);

                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode == 429)
                        _logger.LogWarning("[NaukriJSearch] Quota exceeded (429) for '{Keyword}'", keyword);
                    else
                        _logger.LogError("[NaukriJSearch] FAILED {Status} for '{Keyword}'",
                            response.StatusCode, keyword);
                    return jobs;
                }

                using var json = JsonDocument.Parse(content);
                if (json.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        var job = ParseJSearchJobAsIndia(item, location);
                        if (job != null) jobs.Add(job);
                    }
                }

                _logger.LogInformation("[NaukriJSearch] Parsed {Count} jobs for '{Keyword}'", jobs.Count, keyword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NaukriJSearch] ERROR for '{Keyword}'", keyword);
            }
            return jobs;
        }

        private ExternalJobDto? ParseNaukriJob(JsonElement item, string keyword)
        {
            try
            {
                var title = GetString(item, "title") ?? GetString(item, "jobTitle");
                if (string.IsNullOrWhiteSpace(title)) return null;

                var company = GetString(item, "companyName") ?? GetString(item, "company") ?? "Unknown Company";
                var location = GetString(item, "location") ?? GetString(item, "jobLocation") ?? "India";
                var applyUrl = GetString(item, "jdURL") ?? GetString(item, "applyUrl") ?? GetString(item, "url") ?? "";
                if (string.IsNullOrWhiteSpace(applyUrl)) return null;

                var description = GetString(item, "jobDescription") ?? GetString(item, "description") ?? "";

                decimal? salaryMin = null, salaryMax = null;
                var salaryEl = GetString(item, "salary") ?? GetString(item, "salaryDetail") ?? "";
                // Naukri salaries are often "3-5 Lacs PA" format — skip for now

                DateTime? postedDate = null;
                var dateStr = GetString(item, "createdDate") ?? GetString(item, "postDate");
                if (dateStr != null && DateTime.TryParse(dateStr, out var dt)) postedDate = dt;

                var descLower = description.ToLower();
                var skills = TechSkills.Where(s => descLower.Contains(s.ToLower())).Take(8).ToList();

                var id = GetString(item, "jobId") ?? GetString(item, "id") ?? Guid.NewGuid().ToString();

                return new ExternalJobDto
                {
                    Id = $"nk_{id}",
                    Title = title,
                    Company = company,
                    Location = location,
                    JobType = "Full-time",
                    Description = description.Length > 600 ? description[..600] + "…" : description,
                    SalaryMin = salaryMin,
                    SalaryMax = salaryMax,
                    Currency = "INR",
                    Source = "Naukri",
                    ApplyUrl = applyUrl,
                    PostedDate = postedDate,
                    IsEasyApply = false,
                    Skills = skills,
                    Tags = keyword
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug("[Naukri] Parse error: {Message}", ex.Message);
                return null;
            }
        }

        private ExternalJobDto? ParseJSearchJobAsIndia(JsonElement item, string searchLocation)
        {
            try
            {
                var title = GetString(item, "job_title");
                if (string.IsNullOrWhiteSpace(title)) return null;

                var company = GetString(item, "employer_name") ?? "Unknown Company";
                var applyUrl = GetString(item, "job_apply_link") ?? GetString(item, "job_google_link") ?? "";
                if (string.IsNullOrWhiteSpace(applyUrl)) return null;

                var description = GetString(item, "job_description");
                var publisher = GetString(item, "job_publisher") ?? "Company";
                var city = GetString(item, "job_city");
                var country = GetString(item, "job_country");
                var location = !string.IsNullOrEmpty(city)
                    ? (!string.IsNullOrEmpty(country) ? $"{city}, {country}" : city)
                    : searchLocation;

                var id = GetString(item, "job_id") ?? Guid.NewGuid().ToString();

                DateTime? postedDate = null;
                var postedStr = GetString(item, "job_posted_at_datetime_utc");
                if (DateTime.TryParse(postedStr, out var dt)) postedDate = dt;

                var descLower = (description ?? "").ToLower();
                var skills = TechSkills.Where(s => descLower.Contains(s.ToLower())).Take(8).ToList();

                // Map publisher to known source labels
                var source = publisher switch
                {
                    "LinkedIn" => "LinkedIn",
                    "Indeed" => "Indeed",
                    "Naukri" => "Naukri",
                    "Glassdoor" => "Glassdoor",
                    "Shine" => "Shine",
                    "Instahyre" => "Instahyre",
                    _ => "Company"
                };

                return new ExternalJobDto
                {
                    Id = $"nkjs_{id}",
                    Title = title,
                    Company = company,
                    Location = location,
                    JobType = "Full-time",
                    Description = description?.Length > 600 ? description[..600] + "…" : description,
                    SalaryMin = null,
                    SalaryMax = null,
                    Currency = "INR",
                    Source = source,
                    ApplyUrl = applyUrl,
                    PostedDate = postedDate,
                    IsEasyApply = false,
                    Skills = skills
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug("[NaukriJSearch] Parse error: {Message}", ex.Message);
                return null;
            }
        }

        private static string? GetString(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }
    }
}
