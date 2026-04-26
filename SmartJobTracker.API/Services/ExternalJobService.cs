using System.Text.Json;
using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Aggregates real-time job listings from:
    ///   1. JSearch (RapidAPI) — covers LinkedIn, Indeed, Glassdoor, ZipRecruiter, company portals, agency portals
    ///   2. Adzuna — broad global coverage, free tier 250 req/day
    ///   3. NodeFlair — Singapore tech jobs, no API key required
    /// Configure "JSearch:ApiKey" in appsettings.json to enable JSearch.
    /// Results are sorted by profile match score (skills + role + salary + recency).
    /// Note: MyCareersFuture excluded — restricted to Singapore citizens/PRs only.
    /// </summary>
    public class ExternalJobService : IExternalJobService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<ExternalJobService> _logger;
        private readonly CareersGovService _careersGovService;
        private readonly AdzunaService _adzunaService;
        private readonly LinkedInJobsService _linkedInService;
        private readonly NaukriJobsService _naukriService;

        // Tech keywords to extract from user skills for multi-keyword search
        private static readonly string[] SearchableRoleKeywords = {
            "C#", ".NET Developer", "ASP.NET", "Angular Developer", "Full Stack Developer",
            "Software Engineer", "Backend Developer", "Cloud Engineer", "Azure Developer"
        };

        // Common tech skills for match scoring
        private static readonly string[] TechSkills = {
            "c#", ".net", "asp.net", "angular", "react", "vue", "typescript", "javascript",
            "python", "java", "node.js", "sql", "azure", "aws", "docker", "kubernetes",
            "microservices", "rest api", "git", "agile", "scrum", "devops", "ci/cd",
            "entity framework", "postgresql", "mongodb", "redis", "graphql", "html", "css"
        };

        public ExternalJobService(HttpClient httpClient, IConfiguration config,
            ILogger<ExternalJobService> logger, CareersGovService careersGovService,
            AdzunaService adzunaService, LinkedInJobsService linkedInService, NaukriJobsService naukriService)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _careersGovService = careersGovService;
            _adzunaService = adzunaService;
            _linkedInService = linkedInService;
            _naukriService = naukriService;
        }

        public async Task<ExternalJobSearchResultDto> SearchExternalJobsAsync(
            string searchCountry,
            string searchLocation,
            string? keyword = null,
            string? jobType = null,
            int page = 1,
            string? userSkills = null)
        {
            var allJobs = new List<ExternalJobDto>();
            var sourcesUsed = new List<string>();

            // Build the JSearch location string: "city, country" or just "country"
            var jSearchLocation = string.IsNullOrWhiteSpace(searchLocation)
                ? searchCountry
                : $"{searchLocation}, {searchCountry}";

            // Build multi-keyword list from user skills + the explicit keyword
            var keywords = BuildKeywordList(keyword, userSkills);
            _logger.LogInformation("Searching with keywords: {Keywords} in {Country}/{Location}",
                string.Join(", ", keywords), searchCountry, searchLocation);

            var tasks = new List<Task<(List<ExternalJobDto> jobs, string source)>>();

            // ── Adzuna ─────────────────────────────────────────────────────────
            // Free tier: 250 req/day. Fire keywords sequentially with a small delay
            // to avoid hitting Adzuna's per-second rate limit when all 4 fire at once.
            var adzunaKeywords = keywords.Take(4).ToList();
            async Task<(List<ExternalJobDto>, string)> FetchAdzunaSequential()
            {
                var combined = new List<ExternalJobDto>();
                foreach (var kw in adzunaKeywords)
                {
                    var jobs = await _adzunaService.SearchAsync(kw, page, searchCountry, searchLocation);
                    combined.AddRange(jobs);
                    if (adzunaKeywords.IndexOf(kw) < adzunaKeywords.Count - 1)
                        await Task.Delay(300); // 300ms between requests
                }
                return (combined, "Adzuna");
            }
            tasks.Add(FetchAdzunaSequential());

            // ── LinkedIn (dedicated) ────────────────────────────────────────────
            // Configure "LinkedIn:ApiKey" in appsettings.json to enable.
            // Sign up at: https://rapidapi.com/search/linkedin-job-search
            if (_linkedInService.IsConfigured())
            {
                foreach (var kw in keywords.Take(3))
                {
                    var captured = kw;
                    tasks.Add(_linkedInService.SearchAsync(captured, searchCountry, searchLocation)
                        .ContinueWith(t => (t.Result, "LinkedIn")));
                }
            }

            // ── Naukri / India-specific (JSearch secondary key) ─────────────────
            // Configure "Naukri:ApiKey" for Naukri direct, OR
            //           "JSearch:ApiKey2" for a second JSearch key (covers Naukri via Google Jobs).
            // India-only — Naukri is irrelevant outside India.
            var isIndiaSearch = searchCountry.Contains("India", StringComparison.OrdinalIgnoreCase);
            if (isIndiaSearch && _naukriService.IsConfigured())
            {
                var naukriLocation = string.IsNullOrWhiteSpace(searchLocation) ? "India" : searchLocation;
                foreach (var kw in keywords.Take(2))
                {
                    var captured = kw;
                    tasks.Add(_naukriService.SearchAsync(captured, naukriLocation)
                        .ContinueWith(t => (t.Result, "Naukri")));
                }
            }

            // ── JSearch (sequential, primary key + ApiKey2 fallback) ────────────
            // Covers Google for Jobs → LinkedIn, Indeed, Glassdoor, JobStreet, etc.
            // BASIC plan = 200 req/month. Sequential + 1.2s gap avoids the 1 req/sec
            // per-key rate limit that causes 429 when both queries fire concurrently.
            var jSearchKey  = _config["JSearch:ApiKey"];
            var jSearchKey2 = _config["JSearch:ApiKey2"];
            if (!string.IsNullOrWhiteSpace(jSearchKey) && jSearchKey != "YOUR_RAPIDAPI_KEY_HERE")
            {
                var jSearchKeywords = keywords.Take(2).ToList();
                async Task<(List<ExternalJobDto>, string)> FetchJSearchSequential()
                {
                    var combined = new List<ExternalJobDto>();
                    foreach (var kw in jSearchKeywords)
                    {
                        var jobs = await FetchJSearchAsync(kw, jSearchLocation, jobType,
                                                           jSearchKey, page,
                                                           fallbackApiKey: jSearchKey2);
                        combined.AddRange(jobs);
                        if (jSearchKeywords.IndexOf(kw) < jSearchKeywords.Count - 1)
                            await Task.Delay(1200); // 1.2s gap — stays within 1 req/sec limit
                    }
                    return (combined, "JSearch");
                }
                tasks.Add(FetchJSearchSequential());
            }

            // ── NodeFlair (free, Singapore tech jobs) ─────────────────────────
            // Singapore's dedicated tech job platform — no API key required.
            // Note: MyCareersFuture excluded — restricted to Singapore citizens/PRs only.
            var isSingaporeSearch = searchCountry.Contains("Singapore", StringComparison.OrdinalIgnoreCase)
                                 || searchLocation.Contains("Singapore", StringComparison.OrdinalIgnoreCase);
            if (isSingaporeSearch)
            {
                tasks.Add(FetchNodeFlairAsync(BuildShortKeyword(keyword, userSkills))
                    .ContinueWith(t => (t.Result, "NodeFlair")));
            }

            var results = await Task.WhenAll(tasks);
            foreach (var (jobs, source) in results)
            {
                if (jobs.Count > 0)
                {
                    allJobs.AddRange(jobs);
                    sourcesUsed.Add(source);
                }
            }

            // ── Keep ONLY approved portals ──────────────────────────────────────
            // Allowed: Company (direct), LinkedIn, Indeed, Glassdoor, JobStreet,
            //          Glints, JobsDB, NodeFlair + Trusted Agencies
            // Rejected: "BLOCKED" (BeBee, Jooble, etc.), unknown publishers with no URL
            var approvedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Direct / aggregated
                "Company", "Adzuna",
                // Global portals
                "LinkedIn", "Indeed", "Glassdoor",
                // SG / SEA portals (MyCareersFuture excluded — Singapore citizens/PRs only)
                "JobStreet", "Glints", "JobsDB", "NodeFlair", "Careers@Gov",
                // India portals
                "Naukri", "Shine", "Instahyre", "Monster India", "TimesJobs",
                // AU / NZ
                "SEEK",
                // US / CA
                "ZipRecruiter", "CareerBuilder", "Dice",
                // UK
                "Reed", "Totaljobs",
                // Other
                "Monster"
            };

            allJobs = allJobs.Where(j =>
                !string.IsNullOrEmpty(j.Source) &&
                !j.Source.Equals("BLOCKED", StringComparison.OrdinalIgnoreCase) &&
                (approvedSources.Contains(j.Source) ||
                 j.Source.StartsWith("Agency:", StringComparison.OrdinalIgnoreCase))
            ).ToList();

            // Deduplicate by normalized title + company
            var unique = allJobs
                .GroupBy(j => $"{Normalize(j.Title)}_{Normalize(j.Company)}")
                .Select(g => g.OrderByDescending(j => j.PostedDate).First())
                .ToList();

            // Set source priority and trusted agency flags
            foreach (var job in unique)
            {
                (job.SourcePriority, job.IsTrustedAgency) = GetSourceMeta(job.Source);
            }

            // Apply match scoring against user skills
            var skillList = string.IsNullOrWhiteSpace(userSkills)
                ? new List<string>()
                : userSkills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToLower()).ToList();

            foreach (var job in unique)
                job.MatchScore = CalculateMatchScore(job, skillList);

            // Sort: match score desc → source priority asc → newest
            var sorted = unique
                .OrderByDescending(j => j.MatchScore)
                .ThenBy(j => j.SourcePriority)
                .ThenByDescending(j => j.PostedDate)
                .ToList();

            return new ExternalJobSearchResultDto
            {
                Jobs = sorted,
                TotalCount = sorted.Count,
                Page = page,
                HasMore = sorted.Count >= 20,
                SourcesUsed = sourcesUsed.Distinct().ToList()
            };
        }

        // ─── NodeFlair (Singapore tech jobs, no API key) ─────────────────────────

        private async Task<List<ExternalJobDto>> FetchNodeFlairAsync(string? keyword)
        {
            var jobs = new List<ExternalJobDto>();
            try
            {
                var search = Uri.EscapeDataString(keyword ?? "software engineer");
                // NodeFlair public job listing endpoint
                var url = $"https://nodeflair.com/api/jobs?query={search}&country=Singapore&page=1&per_page=40";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36");
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Headers.TryAddWithoutValidation("Referer", "https://nodeflair.com/jobs");

                _logger.LogInformation("[NodeFlair] GET {Url}", url);
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("[NodeFlair] Status={Status} | Preview={Body}",
                    (int)response.StatusCode, content.Length > 200 ? content[..200] : content);

                if (!response.IsSuccessStatusCode) return jobs;

                var json = JsonDocument.Parse(content);

                // Try common array wrappers
                JsonElement arr = default;
                foreach (var prop in new[] { "jobs", "data", "results", "listings" })
                {
                    if (json.RootElement.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.Array)
                    { arr = el; break; }
                }
                if (arr.ValueKind == JsonValueKind.Undefined && json.RootElement.ValueKind == JsonValueKind.Array)
                    arr = json.RootElement;

                if (arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        var job = ParseNodeFlairJob(item);
                        if (job != null) jobs.Add(job);
                    }
                }

                _logger.LogInformation("[NodeFlair] Parsed {Count} jobs for '{Keyword}'", jobs.Count, keyword);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[NodeFlair] Error: {Message}", ex.Message);
            }
            return jobs;
        }

        private ExternalJobDto? ParseNodeFlairJob(JsonElement item)
        {
            try
            {
                var title   = GetString(item, "title") ?? GetString(item, "job_title");
                if (string.IsNullOrWhiteSpace(title)) return null;

                var company = GetString(item, "company") ?? GetString(item, "company_name") ?? "Unknown Company";
                var applyUrl = GetString(item, "url") ?? GetString(item, "apply_url") ?? GetString(item, "link") ?? "";
                if (string.IsNullOrWhiteSpace(applyUrl)) return null;

                var description = GetString(item, "description") ?? GetString(item, "job_description") ?? "";
                var id = GetString(item, "id") ?? GetString(item, "slug") ?? Guid.NewGuid().ToString();

                // Salary
                decimal? salaryMin = GetDecimal(item, "salary_min") ?? GetDecimal(item, "min_salary");
                decimal? salaryMax = GetDecimal(item, "salary_max") ?? GetDecimal(item, "max_salary");

                // Posted date
                DateTime? postedDate = null;
                var dateStr = GetString(item, "posted_at") ?? GetString(item, "created_at") ?? GetString(item, "date");
                if (DateTime.TryParse(dateStr, out var dt)) postedDate = dt;

                // Skills
                var skills = new List<string>();
                if (item.TryGetProperty("skills", out var skillsEl) && skillsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in skillsEl.EnumerateArray())
                    {
                        var sk = s.ValueKind == JsonValueKind.String ? s.GetString()
                               : GetString(s, "name") ?? GetString(s, "skill");
                        if (!string.IsNullOrEmpty(sk)) skills.Add(sk);
                    }
                }
                if (skills.Count == 0 && !string.IsNullOrEmpty(description))
                    skills = TechSkills.Where(s => description.Contains(s, StringComparison.OrdinalIgnoreCase))
                                       .Select(s => ToTitleCase(s)).Take(8).ToList();

                return new ExternalJobDto
                {
                    Id = $"nf_{id}",
                    Title = title,
                    Company = company,
                    Location = GetString(item, "location") ?? "Singapore",
                    JobType = GetString(item, "job_type") ?? GetString(item, "employment_type") ?? "Full-time",
                    Description = description.Length > 600 ? description[..600] + "…" : description,
                    SalaryMin = salaryMin,
                    SalaryMax = salaryMax,
                    Currency = "SGD",
                    Source = "NodeFlair",
                    ApplyUrl = applyUrl,
                    PostedDate = postedDate,
                    IsEasyApply = false,
                    Skills = skills
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug("[NodeFlair] Parse error: {Message}", ex.Message);
                return null;
            }
        }

        // ─── JSearch (RapidAPI) ───────────────────────────────────────────────

        private async Task<List<ExternalJobDto>> FetchJSearchAsync(
            string? keyword, string location, string? jobType, string apiKey, int page,
            string? fallbackApiKey = null)
        {
            var jobs = new List<ExternalJobDto>();
            try
            {
                var queryStr = string.IsNullOrWhiteSpace(keyword)
                    ? $"software engineer jobs in {location}"
                    : $"{keyword} jobs in {location}";
                var query = Uri.EscapeDataString(queryStr);

                var url = $"https://jsearch.p.rapidapi.com/search?query={query}&page={page}&num_pages=1&date_posted=week";
                if (!string.IsNullOrWhiteSpace(jobType))
                {
                    var empType = jobType.ToLower() switch
                    {
                        "full-time" => "FULLTIME",
                        "part-time" => "PARTTIME",
                        "contract" => "CONTRACTOR",
                        "internship" => "INTERN",
                        _ => "FULLTIME"
                    };
                    url += $"&employment_types={empType}";
                }

                // Try primary key; on 429 automatically retry with fallback key
                var keysToTry = new List<string> { apiKey };
                if (!string.IsNullOrWhiteSpace(fallbackApiKey) && fallbackApiKey != "YOUR_RAPIDAPI_KEY_HERE")
                    keysToTry.Add(fallbackApiKey);

                foreach (var key in keysToTry)
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("X-RapidAPI-Key", key);
                    request.Headers.Add("X-RapidAPI-Host", "jsearch.p.rapidapi.com");

                    _logger.LogInformation("[JSearch] GET {Url} (key suffix: ...{Suffix})",
                        url, key.Length > 6 ? key[^6..] : key);

                    var response = await _httpClient.SendAsync(request);
                    var content = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation("[JSearch] Status={Status} | BodyPreview={Body}",
                        (int)response.StatusCode,
                        content.Length > 300 ? content[..300] : content);

                    if (!response.IsSuccessStatusCode)
                    {
                        if ((int)response.StatusCode == 429)
                        {
                            _logger.LogWarning("[JSearch] Quota exceeded (429) for '{Keyword}' on key ...{Suffix} — trying next key",
                                keyword, key.Length > 6 ? key[^6..] : key);
                            continue; // try fallback key
                        }
                        _logger.LogError("[JSearch] FAILED {Status} for '{Keyword}' — Full body: {Body}",
                            response.StatusCode, keyword, content);
                        return jobs;
                    }

                    var json = JsonDocument.Parse(content);
                    if (json.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in data.EnumerateArray())
                        {
                            var job = ParseJSearchJob(item, location);
                            if (job != null) jobs.Add(job);
                        }
                    }

                    _logger.LogInformation("[JSearch] Parsed {Count} jobs for '{Keyword}' in '{Location}'",
                        jobs.Count, keyword, location);
                    break; // success — no need to try fallback
                }

                if (jobs.Count == 0 && keysToTry.Count > 1)
                    _logger.LogWarning("[JSearch] All keys quota-exceeded for '{Keyword}' — no JSearch results", keyword);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError("[JSearch] TIMEOUT for '{Keyword}': {Message}", keyword, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("[JSearch] NETWORK ERROR for '{Keyword}': {Message} | Inner: {Inner}",
                    keyword, ex.Message, ex.InnerException?.Message);
            }
            return jobs;
        }

        private ExternalJobDto? ParseJSearchJob(JsonElement item, string searchLocation = "")
        {
            try
            {
                var title = GetString(item, "job_title");
                if (string.IsNullOrEmpty(title)) return null;

                var company = GetString(item, "employer_name") ?? "Unknown Company";
                var applyUrl = GetString(item, "job_apply_link") ?? GetString(item, "job_google_link") ?? "";
                var description = GetString(item, "job_description");
                var rawPublisher = GetString(item, "job_publisher") ?? "";
                var id = GetString(item, "job_id") ?? Guid.NewGuid().ToString();
                var logo = GetString(item, "employer_logo");
                var city = GetString(item, "job_city");
                var country = GetString(item, "job_country");
                // Build location from what JSearch returns; fall back to search location
                var location = !string.IsNullOrEmpty(city)
                    ? (!string.IsNullOrEmpty(country) ? $"{city}, {country}" : city)
                    : (!string.IsNullOrEmpty(searchLocation) ? searchLocation : "Unknown Location");

                var isDirect = item.TryGetProperty("job_apply_is_direct", out var directEl) &&
                               directEl.ValueKind == JsonValueKind.True;

                // Map JSearch publisher → our source labels.
                // ATS/career-page platforms = direct company posts ("Company").
                // Known job boards → keep their name so the approved-source filter passes them.
                var source = NormaliseJSearchPublisher(rawPublisher, isDirect, applyUrl);

                decimal? salaryMin = GetDecimal(item, "job_min_salary");
                decimal? salaryMax = GetDecimal(item, "job_max_salary");
                var currency = GetString(item, "job_salary_currency") ?? "SGD";

                // Normalize salary period — convert annual to monthly for SGD display
                if (item.TryGetProperty("job_salary_period", out var period) &&
                    period.GetString()?.ToUpper() == "YEAR")
                {
                    salaryMin = salaryMin.HasValue ? salaryMin / 12 : null;
                    salaryMax = salaryMax.HasValue ? salaryMax / 12 : null;
                }

                DateTime? postedDate = null;
                var postedStr = GetString(item, "job_posted_at_datetime_utc");
                if (DateTime.TryParse(postedStr, out var dt)) postedDate = dt;

                // Skills from required_skills or highlights qualifications
                var skills = new List<string>();
                if (item.TryGetProperty("job_required_skills", out var skillsEl) && skillsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in skillsEl.EnumerateArray())
                    {
                        var sk = s.GetString();
                        if (!string.IsNullOrEmpty(sk)) skills.Add(sk);
                    }
                }

                // If no explicit skills, extract from description
                if (skills.Count == 0 && !string.IsNullOrEmpty(description))
                {
                    var descLower = description.ToLower();
                    skills = TechSkills.Where(s => descLower.Contains(s))
                        .Select(s => ToTitleCase(s))
                        .Take(8)
                        .ToList();
                }

                var empType = GetString(item, "job_employment_type");
                var jobType = empType switch
                {
                    "FULLTIME" => "Full-time",
                    "PARTTIME" => "Part-time",
                    "CONTRACTOR" => "Contract",
                    "INTERN" => "Internship",
                    _ => "Full-time"
                };

                return new ExternalJobDto
                {
                    Id = $"js_{id}",
                    Title = title,
                    Company = company,
                    CompanyLogo = logo,
                    Location = location,
                    JobType = jobType,
                    Description = description?.Length > 800
                        ? description[..800] + "…"
                        : description,
                    SalaryMin = salaryMin,
                    SalaryMax = salaryMax,
                    Currency = currency,
                    Source = source,
                    ApplyUrl = applyUrl,
                    PostedDate = postedDate,
                    IsEasyApply = isDirect,
                    Skills = skills
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug("JSearch parse error: {Message}", ex.Message);
                return null;
            }
        }

        // ─── Keyword Building ─────────────────────────────────────────────────

        /// <summary>
        /// Returns a short, clean keyword for NodeFlair and similar portals that
        /// struggle with long compound queries.
        /// E.g. "Full Stack Software Engineer ASP.NET Core" → "software engineer"
        /// </summary>
        private static string BuildShortKeyword(string? keyword, string? userSkills)
        {
            // Prefer a simple role term derived from skills
            var skills = string.IsNullOrWhiteSpace(userSkills)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : userSkills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (skills.Contains("c#") || skills.Contains(".net") || skills.Contains("asp.net"))
                return "software engineer";
            if (skills.Contains("angular") || skills.Contains("react"))
                return "full stack developer";
            if (skills.Contains("python"))
                return "python developer";
            if (skills.Contains("java"))
                return "java developer";

            // Fall back: take first 3 words of keyword or generic term
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var words = keyword.Trim().Split(' ');
                return string.Join(" ", words.Take(3));
            }

            return "software engineer";
        }

        /// <summary>
        /// Builds up to 8 targeted search terms from the explicit keyword + resume skills.
        /// Uses LinkedIn-style broad role terms alongside tech-specific ones so that
        /// the same jobs visible on LinkedIn also surface here.
        /// </summary>
        private static List<string> BuildKeywordList(string? keyword, string? userSkills)
        {
            var added = new LinkedList<string>();

            void Add(string term)
            {
                if (!added.Any(k => k.Equals(term, StringComparison.OrdinalIgnoreCase)))
                    added.AddLast(term);
            }

            // 1. Explicit keyword always first
            if (!string.IsNullOrWhiteSpace(keyword))
                added.AddFirst(keyword.Trim());

            // 2. Extract skills from user profile
            var skills = string.IsNullOrWhiteSpace(userSkills)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : userSkills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Broad LinkedIn-style role terms (what LinkedIn search bar returns for these)
            // Location is NOT appended here — Adzuna's where= and JSearch's query handle that.
            if (skills.Contains("c#") || skills.Contains(".net") || skills.Contains("asp.net"))
            {
                Add(".NET Software Engineer");
                Add("C# Developer");
            }
            if (skills.Contains("angular") || skills.Contains("typescript"))
                Add("Angular Full Stack Developer");
            if (skills.Contains("react"))
                Add("React Developer");
            if (skills.Contains("azure") || skills.Contains("aws"))
                Add("Cloud Software Engineer");
            if (skills.Contains("devops") || skills.Contains("kubernetes") || skills.Contains("docker"))
                Add("DevOps Engineer");
            if (skills.Contains("python"))
                Add("Python Developer");
            if (skills.Contains("java"))
                Add("Java Developer");

            // Broad catch-all role searches — these match LinkedIn's "Software Engineer" volume
            bool hasDotNet = skills.Contains("c#") || skills.Contains(".net");
            bool hasAngular = skills.Contains("angular") || skills.Contains("react");

            if (hasDotNet || hasAngular || skills.Count == 0)
                Add("Full Stack Developer");

            if (hasDotNet)
                Add("Backend .NET Engineer");

            if (skills.Count == 0 && added.Count == 0)
                Add(".NET Developer");

            // 3. Keep top 8 (2 get double-paged → up to 100+350 results)
            return added.Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
        }

        // ─── Source Priority ──────────────────────────────────────────────────

        /// <summary>
        /// Returns (priority, isTrustedAgency).
        /// Priority 1 = best (direct company), higher = lower priority.
        /// </summary>
        private static (int priority, bool isTrustedAgency) GetSourceMeta(string? source)
        {
            if (string.IsNullOrEmpty(source)) return (5, false);

            // Trusted agency detection (source is prefixed "Agency:{name}" by AdzunaService)
            if (source.StartsWith("Agency:", StringComparison.OrdinalIgnoreCase))
                return (2, true); // Agency = priority 2, same tier as LinkedIn (trusted)

            return source switch
            {
                "Company"           => (1, false),  // Direct company post — best
                "LinkedIn"          => (2, false),
                "Indeed"            => (3, false),
                "Glassdoor"         => (4, false),
                "JobStreet"         => (4, false),
                "NodeFlair"         => (3, false),  // SG tech-specific — same tier as Indeed
                "Glints"            => (5, false),
                "JobsDB"            => (5, false),
                "SEEK"              => (5, false),
                "Careers@Gov"       => (3, false),
                _                   => (6, false),
            };
        }

        // ─── Match Scoring ────────────────────────────────────────────────────

        private static int CalculateMatchScore(ExternalJobDto job, List<string> userSkills)
        {
            int score = 0;
            var jobText = $"{job.Title} {job.Description} {string.Join(" ", job.Skills)}".ToLower();
            var jobTitle = job.Title.ToLower();

            // 1. Skill match — up to 45 points (5 pts per matching skill)
            if (userSkills.Count > 0)
            {
                int skillMatches = userSkills.Count(skill => jobText.Contains(skill));
                score += Math.Min(skillMatches * 5, 45);
            }

            // 2. Role/title match — up to 20 points
            var roleKeywords = new[] { "full stack", ".net", "dotnet", "software engineer",
                                       "backend", "cloud", "azure", "angular", "c#", "asp.net" };
            int roleMatches = roleKeywords.Count(r => jobTitle.Contains(r));
            score += Math.Min(roleMatches * 7, 20);

            // 3. Source priority bonus — up to 15 points
            //    Direct company post = 15, LinkedIn/Agency = 12, Indeed = 10, etc.
            score += job.SourcePriority switch
            {
                1 => 15,  // Direct company
                2 => 12,  // LinkedIn or Trusted Agency
                3 => 10,  // Indeed / Careers@Gov
                4 => 7,   // Glassdoor / JobStreet
                5 => 4,   // Glints / JobsDB
                _ => 2
            };

            // 4. Trusted agency bonus — extra 5 pts for Michael Page, Robert Walters, Hays, JobPlus
            if (job.IsTrustedAgency) score += 5;

            // 5. EP salary eligibility — up to 10 points
            if (job.SalaryMin.HasValue)
            {
                if (job.SalaryMin >= 8000) score += 10;
                else if (job.SalaryMin >= 6000) score += 7;
                else if (job.SalaryMin >= 5000) score += 5;
                else if (job.SalaryMin >= 4000) score += 2;
            }

            // 6. Recency — up to 10 points
            if (job.PostedDate.HasValue)
            {
                var age = (DateTime.UtcNow - job.PostedDate.Value).TotalDays;
                if (age <= 1) score += 10;
                else if (age <= 3) score += 7;
                else if (age <= 7) score += 4;
                else if (age <= 14) score += 1;
            }

            return Math.Min(score, 100);
        }

        private int CalculateDefaultScore(ExternalJobDto job)
        {
            int score = 50; // baseline
            if (job.PostedDate.HasValue)
            {
                var age = (DateTime.UtcNow - job.PostedDate.Value).TotalDays;
                if (age <= 1) score += 20;
                else if (age <= 3) score += 15;
                else if (age <= 7) score += 10;
            }
            if (job.SalaryMin.HasValue && job.SalaryMin >= 5000) score += 10;
            if (job.IsEasyApply) score += 10;
            return Math.Min(score, 100);
        }

        // ─── JSearch Publisher Normaliser ─────────────────────────────────────

        // Job boards we accept as-is (must match our approved-source filter exactly)
        private static readonly HashSet<string> KnownJobBoards = new(StringComparer.OrdinalIgnoreCase)
        {
            "LinkedIn", "Indeed", "Glassdoor", "JobStreet", "Glints", "JobsDB"
        };

        // ⛔ Sources we NEVER show — spam, low-quality, or unverifiable boards
        private static readonly HashSet<string> BlockedSourceDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "bebee.com", "jooble.org", "jooble.com", "simplyhired.com",
            "trovit.com", "mitula.com", "adzuna.com",   // Adzuna jobs come via AdzunaService, not raw URLs
            "careerjet.com", "jobrapido.com", "neuvoo.com", "talent.com",
            "jobs2careers.com", "jobisland.com", "jobomas.com", "tip.it"
        };

        private static readonly HashSet<string> BlockedPublisherNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "BeBee", "Jooble", "SimplyHired", "Trovit", "Mitula",
            "CareerJet", "Jobrapido", "Neuvoo", "Talent.com"
        };

        // ATS / company career-page platforms → these are direct company posts
        private static readonly HashSet<string> AtsPlatforms = new(StringComparer.OrdinalIgnoreCase)
        {
            "Workday", "Greenhouse", "Lever", "SmartRecruiters", "Taleo", "iCIMS",
            "BambooHR", "Jobvite", "Bullhorn", "Recruitee", "Ashby", "Rippling",
            "SAP SuccessFactors", "Oracle HCM", "Breezy HR", "Teamtailor",
            "Zoho Recruit", "Zoho", "Keka", "Darwinbox", "Freshteam",
            "Company Website", "Company", "Employer Website",
        };

        // Trusted agencies from Adzuna also appear in JSearch — detect by name
        private static readonly string[] TrustedAgencyKeywords =
            { "Michael Page", "Robert Walters", "Hays", "JobPlus" };

        private static string NormaliseJSearchPublisher(string publisher, bool isDirect, string applyUrl)
        {
            // ⛔ 0. Blocked publisher names — reject immediately
            if (BlockedPublisherNames.Contains(publisher)) return "BLOCKED";

            // ⛔ 0b. Blocked URL domains — reject regardless of publisher name
            if (!string.IsNullOrEmpty(applyUrl))
            {
                var urlLower = applyUrl.ToLower();
                foreach (var blocked in BlockedSourceDomains)
                    if (urlLower.Contains(blocked)) return "BLOCKED";
            }

            // 1. Trusted agency — label as Agency: so the filter/badge works the same as Adzuna
            foreach (var agency in TrustedAgencyKeywords)
                if (publisher.Contains(agency, StringComparison.OrdinalIgnoreCase))
                    return $"Agency:{publisher}";

            // 2. Known job board — keep name exactly (passes approved-source filter)
            if (KnownJobBoards.Contains(publisher)) return publisher;

            // 3. ATS / career page = direct company post
            if (isDirect || AtsPlatforms.Contains(publisher)) return "Company";

            // 4. URL-based fallback — if redirect goes to a known board, use it
            var url = applyUrl.ToLower();
            if (url.Contains("linkedin.com"))  return "LinkedIn";
            if (url.Contains("indeed.com"))    return "Indeed";
            if (url.Contains("glassdoor.com")) return "Glassdoor";
            if (url.Contains("jobstreet.com")) return "JobStreet";
            if (url.Contains("glints.com"))    return "Glints";
            if (url.Contains("jobsdb.com"))    return "JobsDB";

            // 5. Anything else with a direct apply link = treat as Company
            if (isDirect || !string.IsNullOrEmpty(applyUrl)) return "Company";

            // 6. Unknown and no URL — skip (filter will drop it anyway)
            return publisher;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static string? GetString(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }

        private static decimal? GetDecimal(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var val))
                    return val;
            }
            return null;
        }

        private static string Normalize(string s) =>
            System.Text.RegularExpressions.Regex.Replace(s.ToLower().Trim(), @"\s+", " ");

        private static string ToTitleCase(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
    }
}
