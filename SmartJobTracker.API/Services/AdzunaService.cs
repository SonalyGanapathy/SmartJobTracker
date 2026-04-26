using System.Text.Json;
using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Fetches real-time jobs from Adzuna API.
    /// Adzuna aggregates LinkedIn, Indeed, Glassdoor, JobStreet, and 100+ job boards globally.
    /// Free tier: 250 requests/day — register at https://developer.adzuna.com/
    /// Configure "Adzuna:AppId" and "Adzuna:AppKey" in appsettings.json.
    /// </summary>
    public class AdzunaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<AdzunaService> _logger;

        private const string BaseUrl = "https://api.adzuna.com/v1/api/jobs";

        // Adzuna country code map
        private static readonly Dictionary<string, string> CountryCodeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Singapore",      "sg" }, { "Canada",        "ca" }, { "United States", "us" },
            { "USA",            "us" }, { "United Kingdom", "gb" }, { "UK",             "gb" },
            { "Australia",      "au" }, { "India",          "in" }, { "Germany",        "de" },
            { "France",         "fr" }, { "Netherlands",    "nl" }, { "UAE",            "ae" },
            { "New Zealand",    "nz" }, { "South Africa",   "za" }, { "Brazil",         "br" },
            { "Mexico",         "mx" }, { "Poland",         "pl" }, { "Italy",          "it" },
            { "Spain",          "es" }, { "Belgium",        "be" }, { "Austria",        "at" },
            { "Switzerland",    "ch" },
        };

        private static readonly Dictionary<string, string> CurrencyMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "sg", "SGD" }, { "ca", "CAD" }, { "us", "USD" }, { "gb", "GBP" },
            { "au", "AUD" }, { "in", "INR" }, { "de", "EUR" }, { "fr", "EUR" },
            { "nl", "EUR" }, { "ae", "AED" }, { "nz", "NZD" }, { "za", "ZAR" },
            { "br", "BRL" }, { "mx", "MXN" }, { "pl", "PLN" }, { "it", "EUR" },
            { "es", "EUR" }, { "be", "EUR" }, { "at", "EUR" }, { "ch", "CHF" },
        };

        public static string GetCountryCode(string country) =>
            CountryCodeMap.TryGetValue(country, out var code) ? code : "gb";

        private static string GetCurrency(string countryCode) =>
            CurrencyMap.TryGetValue(countryCode, out var cur) ? cur : "USD";

        public AdzunaService(HttpClient httpClient, IConfiguration config, ILogger<AdzunaService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        /// <summary>Fetches one page (50 results) from Adzuna for a given keyword.</summary>
        public async Task<List<ExternalJobDto>> SearchAsync(string keyword, int page = 1,
            string searchCountry = "Singapore", string searchLocation = "")
        {
            var jobs = new List<ExternalJobDto>();

            var appId = _config["Adzuna:AppId"];
            var appKey = _config["Adzuna:AppKey"];

            if (string.IsNullOrWhiteSpace(appId) || appId == "YOUR_ADZUNA_APP_ID" ||
                string.IsNullOrWhiteSpace(appKey) || appKey == "YOUR_ADZUNA_APP_KEY")
            {
                _logger.LogDebug("Adzuna not configured — skipping");
                return jobs;
            }

            try
            {
                var countryCode = GetCountryCode(searchCountry);
                var whereValue = string.IsNullOrWhiteSpace(searchLocation)
                    ? Uri.EscapeDataString(searchCountry)
                    : Uri.EscapeDataString(searchLocation);
                var kw = Uri.EscapeDataString(keyword);

                var url = $"{BaseUrl}/{countryCode}/search/{page}" +
                          $"?app_id={appId}&app_key={appKey}" +
                          $"&results_per_page=50&what={kw}&where={whereValue}" +
                          $"&content-type=application/json&sort_by=date";

                // Log the full URL (key masked) so we can inspect it in VS Output
                _logger.LogInformation("[Adzuna] GET {Url}", url.Replace(appKey, "***"));

                var response = await _httpClient.GetAsync(url);
                // ReadAsStringAsync() fails when Adzuna returns charset=utf8 (no hyphen).
                // Bypass .NET charset parsing by reading raw bytes and forcing UTF-8 decode.
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var content = System.Text.Encoding.UTF8.GetString(bytes);

                _logger.LogInformation("[Adzuna] Status={Status} | BodyPreview={Body}",
                    (int)response.StatusCode,
                    content.Length > 300 ? content[..300] : content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[Adzuna] FAILED {Status} for '{Keyword}' — Full body: {Body}",
                        response.StatusCode, keyword, content);
                    return jobs;
                }

                var json = JsonDocument.Parse(content);

                if (!json.RootElement.TryGetProperty("results", out var results) ||
                    results.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("[Adzuna] No 'results' array in response for '{Keyword}'", keyword);
                    return jobs;
                }

                foreach (var item in results.EnumerateArray())
                {
                    var job = ParseJob(item, keyword, searchCountry, searchLocation);
                    if (job != null) jobs.Add(job);
                }

                _logger.LogInformation("[Adzuna] Parsed {Count} jobs for '{Keyword}' (page {Page}, country={Country})",
                    jobs.Count, keyword, page, countryCode);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError("[Adzuna] TIMEOUT after {Timeout}s for '{Keyword}': {Message}",
                    _httpClient.Timeout.TotalSeconds, keyword, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("[Adzuna] NETWORK ERROR for '{Keyword}': {Message} | InnerException: {Inner}",
                    keyword, ex.Message, ex.InnerException?.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Adzuna] UNEXPECTED ERROR for '{Keyword}': {Message}", keyword, ex.Message);
            }

            return jobs;
        }

        /// <summary>Fetches multiple pages in parallel — call this for primary keywords to maximise results.</summary>
        public async Task<List<ExternalJobDto>> SearchMultiPageAsync(string keyword, int pages = 2,
            string searchCountry = "Singapore", string searchLocation = "")
        {
            var pageTasks = Enumerable.Range(1, pages)
                .Select(p => SearchAsync(keyword, p, searchCountry, searchLocation));
            var results = await Task.WhenAll(pageTasks);
            return results.SelectMany(x => x).ToList();
        }

        private ExternalJobDto? ParseJob(JsonElement item, string searchKeyword,
            string searchCountry = "Singapore", string searchLocation = "")
        {
            try
            {
                var title = GetString(item, "title");
                if (string.IsNullOrEmpty(title)) return null;

                // Company
                string? company = null;
                if (item.TryGetProperty("company", out var companyEl))
                    company = GetString(companyEl, "display_name");

                // Location — use what Adzuna returns; fall back to the search location/country
                var fallbackLocation = string.IsNullOrWhiteSpace(searchLocation)
                    ? searchCountry
                    : $"{searchLocation}, {searchCountry}";
                string location = fallbackLocation;
                if (item.TryGetProperty("location", out var locEl))
                {
                    var display = GetString(locEl, "display_name");
                    if (!string.IsNullOrEmpty(display))
                        location = display;
                }

                // Salary
                decimal? salaryMin = GetDecimal(item, "salary_min");
                decimal? salaryMax = GetDecimal(item, "salary_max");

                // Adzuna sometimes gives annual salary — detect and convert to monthly
                if (salaryMin.HasValue && salaryMin > 50000) salaryMin /= 12;
                if (salaryMax.HasValue && salaryMax > 50000) salaryMax /= 12;

                // Apply URL
                var applyUrl = GetString(item, "redirect_url") ?? GetString(item, "url") ?? "";

                // Description
                var description = GetString(item, "description") ?? "";

                // Posted date
                DateTime? postedDate = null;
                var dateStr = GetString(item, "created");
                if (DateTime.TryParse(dateStr, out var dt)) postedDate = dt;

                // Detect source from the redirect URL — determines priority ranking
                var source = DetectSource(applyUrl, company);

                // Extract skills from description
                var techSkills = new[] {
                    "C#", ".NET", "ASP.NET", "Angular", "React", "Vue", "TypeScript", "JavaScript",
                    "Azure", "AWS", "Docker", "Kubernetes", "SQL Server", "PostgreSQL", "MongoDB",
                    "Entity Framework", "Microservices", "REST API", "Python", "Java", "Node.js",
                    "Git", "CI/CD", "DevOps", "Agile", "Scrum"
                };
                var descLower = description.ToLower();
                var skills = techSkills.Where(s => descLower.Contains(s.ToLower())).Take(8).ToList();

                var id = GetString(item, "id") ?? Guid.NewGuid().ToString();

                var countryCode = GetCountryCode(searchCountry);
                var currency = GetCurrency(countryCode);

                return new ExternalJobDto
                {
                    Id = $"adz_{id}",
                    Title = title,
                    Company = company ?? "Unknown Employer",
                    Location = location,
                    JobType = "Full-time",
                    Description = description.Length > 600 ? description[..600] + "…" : description,
                    SalaryMin = salaryMin,
                    SalaryMax = salaryMax,
                    Currency = currency,
                    Source = source,
                    ApplyUrl = applyUrl,
                    PostedDate = postedDate,
                    IsEasyApply = false,
                    Skills = skills,
                    Tags = searchKeyword // tag with which keyword found it
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Adzuna parse error: {Message}", ex.Message);
                return null;
            }
        }

        // ─── Trusted agencies list ────────────────────────────────────────────
        public static readonly HashSet<string> TrustedAgencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "Michael Page", "Robert Walters", "Hays", "JobPlus",
            "Michael Page Singapore", "Robert Walters Singapore",
            "Hays Singapore", "JobPlus Singapore"
        };

        // ─── Known job board domains → source name → priority ─────────────────
        private static readonly (string domain, string source)[] SourceMap = {
            ("linkedin.com",    "LinkedIn"),
            ("indeed.com",      "Indeed"),
            ("glassdoor.com",   "Glassdoor"),
            ("jobstreet.com",   "JobStreet"),
            ("glints.com",      "Glints"),
            ("jobsdb.com",      "JobsDB"),
            ("seek.com",        "SEEK"),
            ("monster.com",     "Monster"),
            ("ziprecruiter",    "ZipRecruiter"),
        };

        private static string DetectSource(string? applyUrl, string? company)
        {
            // Trusted agency = flag as agency regardless of where job is posted
            if (!string.IsNullOrEmpty(company) &&
                TrustedAgencies.Any(a => company.Contains(a, StringComparison.OrdinalIgnoreCase)))
                return $"Agency:{company}";

            if (string.IsNullOrEmpty(applyUrl)) return "Adzuna";

            var url = applyUrl.ToLower();

            // Adzuna redirect URLs (adzuna.com/land/ad/...) — the real destination is unknown,
            // so label as "Adzuna" so the portal filter works correctly.
            if (url.Contains("adzuna.")) return "Adzuna";

            foreach (var (domain, source) in SourceMap)
                if (url.Contains(domain)) return source;

            // URL points to a real job board or company career page not in the map
            return "Company";
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
