using SmartJobTracker.API.DTOs;
using System.Text;
using System.Text.Json;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Claude-powered job search pipeline:
    ///   1. Claude generates optimised search queries from the candidate profile
    ///   2. Queries run against the existing external job APIs (Adzuna / JSearch / LinkedIn)
    ///   3. Claude analyses every raw listing and scores fit (0–100) with a written explanation
    ///   4. Claude writes a personalised cover note and recruiter message for each top job
    /// </summary>
    public class ClaudeJobSearchService : IClaudeJobSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IExternalJobService _externalJobService;
        private readonly ILogger<ClaudeJobSearchService> _logger;
        private readonly string _apiKey;
        private readonly string _model;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
        };

        public ClaudeJobSearchService(
            IHttpClientFactory httpClientFactory,
            IExternalJobService externalJobService,
            IConfiguration config,
            ILogger<ClaudeJobSearchService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("AnthropicClient");
            _externalJobService = externalJobService;
            _logger = logger;
            _apiKey = config["Anthropic:ApiKey"]
                ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured in appsettings.json");
            _model = config["Anthropic:Model"] ?? "claude-opus-4-6";
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Public entry point
        // ─────────────────────────────────────────────────────────────────────────

        public async Task<ClaudeJobSearchResponseDto> SearchAsync(ClaudeJobSearchRequestDto request)
        {
            _logger.LogInformation("Claude Job Search — roles: {Roles} | location: {Loc} | model: {Model}",
                string.Join(", ", request.TargetRoles), request.SearchCountry, _model);

            // ── 1. Let Claude generate optimised search queries ──────────────────
            var queries = await GenerateSearchQueriesAsync(request);
            _logger.LogInformation("Claude generated {N} queries: {Q}", queries.Count, string.Join(" | ", queries));

            // ── 2. Fetch raw jobs using the existing job API aggregator ──────────
            var rawJobs = await FetchRawJobsAsync(request, queries);
            _logger.LogInformation("Fetched {N} raw jobs from external APIs", rawJobs.Count);

            if (rawJobs.Count == 0)
            {
                return new ClaudeJobSearchResponseDto
                {
                    Jobs = new(),
                    TotalFound = 0,
                    TotalSearched = 0,
                    SearchSummary = "No jobs found from external APIs. Try broader skills or a different location.",
                    GeneratedAt = DateTime.UtcNow,
                    Model = _model,
                    GeneratedQueries = queries,
                };
            }

            // ── 3. Claude analyses and ranks the raw listings ────────────────────
            var analysed = await AnalyseAndRankWithClaudeAsync(rawJobs, request);
            _logger.LogInformation("Claude ranked {N} jobs", analysed.Count);

            // ── 4. Claude writes cover notes for top results ─────────────────────
            var topJobs = analysed.Take(request.MaxJobs).ToList();
            await GenerateContentWithClaudeAsync(topJobs, request);

            var sourcesUsed = rawJobs
                .Select(j => j.Source ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();

            return new ClaudeJobSearchResponseDto
            {
                Jobs = topJobs,
                TotalFound = topJobs.Count,
                TotalSearched = rawJobs.Count,
                SourcesUsed = sourcesUsed,
                GeneratedAt = DateTime.UtcNow,
                Model = _model,
                GeneratedQueries = queries,
                SearchSummary =
                    $"Claude analysed {rawJobs.Count} live listings and identified {topJobs.Count} best-fit matches " +
                    $"for {request.TargetRoles.FirstOrDefault() ?? "your profile"} in {request.SearchCountry}.",
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Step 1 – Query generation
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<List<string>> GenerateSearchQueriesAsync(ClaudeJobSearchRequestDto req)
        {
            var prompt = $"""
You are an expert technical recruiter. Generate exactly 5 diverse, high-signal job search queries for the candidate below.
Vary the phrasing (some role-focused, some skill-focused) to maximise coverage across job boards.

Candidate profile:
- Target roles: {string.Join(", ", req.TargetRoles)}
- Core skills: {string.Join(", ", req.CoreSkills)}
- Experience: {req.ExperienceYears} years
- Current location: {req.CandidateLocation}
- Target location: {req.SearchCountry}{(string.IsNullOrWhiteSpace(req.SearchLocation) ? "" : ", " + req.SearchLocation)}
- Certifications: {(req.Certifications.Any() ? string.Join(", ", req.Certifications) : "none")}

Respond with ONLY a valid JSON array of exactly 5 strings — no markdown, no explanation.
Example: ["query 1","query 2","query 3","query 4","query 5"]
""";

            try
            {
                var response = await CallClaudeAsync(prompt, maxTokens: 400);
                // Strip any markdown code fences if present
                var clean = response.Trim().TrimStart('`');
                if (clean.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                    clean = clean[4..].Trim();
                clean = clean.TrimEnd('`').Trim();

                var queries = JsonSerializer.Deserialize<List<string>>(clean, _jsonOpts);
                if (queries != null && queries.Count > 0)
                    return queries;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Claude query generation failed: {Msg}", ex.Message);
            }

            // Fallback: build queries from profile data
            var fallback = req.TargetRoles
                .Take(3)
                .Select(r => $"{r} {string.Join(" ", req.CoreSkills.Take(2))} {req.SearchCountry}")
                .ToList();
            if (fallback.Count < 3)
                fallback.Add($"{req.CoreSkills.FirstOrDefault() ?? "software engineer"} {req.SearchCountry}");
            return fallback;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Step 2 – Fetch raw jobs using the existing aggregator
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<List<ExternalJobDto>> FetchRawJobsAsync(
            ClaudeJobSearchRequestDto req, List<string> queries)
        {
            var allJobs = new List<ExternalJobDto>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var userSkills = string.Join(",", req.CoreSkills);

            // Run the primary query and a couple of alternates in parallel
            var tasks = queries
                .Take(3)
                .Select(q => _externalJobService.SearchExternalJobsAsync(
                    searchCountry: req.SearchCountry,
                    searchLocation: req.SearchLocation,
                    keyword: q,
                    jobType: "full-time",
                    page: 1,
                    userSkills: userSkills))
                .ToList();

            var results = await Task.WhenAll(tasks);

            foreach (var res in results)
            {
                foreach (var job in res.Jobs ?? new())
                {
                    var key = $"{(job.Title ?? "").ToLower()}|{(job.Company ?? "").ToLower()}";
                    if (seen.Add(key))
                        allJobs.Add(job);
                }
            }

            // Apply portal filter if specified
            if (req.JobPortals.Count > 0)
            {
                allJobs = allJobs
                    .Where(j => req.JobPortals.Any(p =>
                        (j.Source ?? "").Contains(p, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Apply date filter
            var cutoff = DateTime.UtcNow.AddDays(-req.PostedWithinDays);
            allJobs = allJobs
                .Where(j => !j.PostedDate.HasValue || j.PostedDate.Value >= cutoff)
                .Where(j => !string.IsNullOrWhiteSpace(j.ApplyUrl))
                .ToList();

            return allJobs;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Step 3 – Claude analyses + ranks every listing
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<List<ClaudeJobResultDto>> AnalyseAndRankWithClaudeAsync(
            List<ExternalJobDto> rawJobs, ClaudeJobSearchRequestDto req)
        {
            // Trim description to keep token count manageable
            var jobsPayload = rawJobs.Take(40).Select(j => new
            {
                id = j.Id,
                title = j.Title,
                company = j.Company,
                location = j.Location,
                salary = j.SalaryMin.HasValue
                    ? $"{j.Currency ?? "SGD"} {j.SalaryMin:N0}{(j.SalaryMax.HasValue ? "–" + j.SalaryMax.Value.ToString("N0") : "+")} /mo"
                    : "Not disclosed",
                description = (j.Description ?? "").Length > 400
                    ? j.Description![..400] + "…"
                    : j.Description ?? "",
                skills = j.Skills ?? new(),
                source = j.Source,
                postedDate = j.PostedDate?.ToString("yyyy-MM-dd"),
                applyUrl = j.ApplyUrl,
                jobType = j.JobType,
                salaryMin = j.SalaryMin,
                salaryMax = j.SalaryMax,
                currency = j.Currency,
                sourcePriority = j.SourcePriority,
                isTrustedAgency = j.IsTrustedAgency,
                isEasyApply = j.IsEasyApply,
            });

            var prompt = $$"""
You are a senior technical recruiter AI. Analyse the following job listings for a candidate and return a ranked JSON array.

Candidate profile:
- Target roles: {string.Join(", ", req.TargetRoles)}
- Core skills: {string.Join(", ", req.CoreSkills)}
- Experience: {req.ExperienceYears} years
- Current location: {req.CandidateLocation}
- Target location: {req.SearchCountry}{(string.IsNullOrWhiteSpace(req.SearchLocation) ? "" : ", " + req.SearchLocation)}
- Certifications: {(req.Certifications.Any() ? string.Join(", ", req.Certifications) : "none")}

Job listings (JSON):
{JsonSerializer.Serialize(jobsPayload, _jsonOpts)}

Instructions:
1. Score each job's fit for this candidate (matchPercent 0–100) based on skills overlap, role alignment, and seniority.
2. Score visa/EP sponsorship likelihood (sponsorshipScore 0–100): high for MNCs, high salary (>= SGD 5000/mo), direct company posts.
3. Set visaSponsorshipChance to "High" (score>=70), "Medium" (>=45), or "Low".
4. Write a concise matchAnalysis (1–2 sentences) explaining the fit.
5. Sort descending by matchPercent.

Respond with ONLY a valid JSON array (no markdown, no explanation) using this schema per item:
{
  "id": "string",
  "title": "string",
  "company": "string",
  "location": "string",
  "salary": "string",
  "salaryMin": number_or_null,
  "salaryMax": number_or_null,
  "currency": "string",
  "matchPercent": integer,
  "matchAnalysis": "string",
  "visaSponsorshipChance": "High|Medium|Low",
  "sponsorshipScore": integer,
  "applyUrl": "string",
  "source": "string",
  "sourcePriority": integer,
  "isTrustedAgency": boolean,
  "isEasyApply": boolean,
  "postedDate": "ISO date string or null",
  "skills": ["string"],
  "jobType": "string",
  "description": "string"
}
""";

            try
            {
                var response = await CallClaudeAsync(prompt, maxTokens: 8000);
                var clean = CleanJsonResponse(response);
                var analysed = JsonSerializer.Deserialize<List<ClaudeJobResultDto>>(clean, _jsonOpts);
                if (analysed != null && analysed.Count > 0)
                    return analysed.OrderByDescending(j => j.MatchPercent).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Claude analysis failed: {Msg}", ex.Message);
            }

            // Fallback: map raw jobs without AI analysis
            return rawJobs.Take(req.MaxJobs).Select(j => new ClaudeJobResultDto
            {
                Id = j.Id,
                Title = j.Title,
                Company = j.Company,
                Location = j.Location,
                Salary = j.SalaryMin.HasValue ? $"{j.Currency} {j.SalaryMin:N0}/mo" : "Not disclosed",
                SalaryMin = j.SalaryMin,
                SalaryMax = j.SalaryMax,
                Currency = j.Currency ?? "SGD",
                MatchPercent = j.MatchScore,
                MatchAnalysis = "Analysis unavailable — Claude API may be misconfigured.",
                VisaSponsorshipChance = "Medium",
                SponsorshipScore = 40,
                ApplyUrl = j.ApplyUrl,
                Source = j.Source ?? "",
                SourcePriority = j.SourcePriority,
                IsTrustedAgency = j.IsTrustedAgency,
                IsEasyApply = j.IsEasyApply,
                PostedDate = j.PostedDate,
                Skills = j.Skills ?? new(),
                JobType = j.JobType,
                Description = j.Description,
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Step 4 – Claude writes cover note + recruiter message per job
        // ─────────────────────────────────────────────────────────────────────────

        private async Task GenerateContentWithClaudeAsync(
            List<ClaudeJobResultDto> jobs, ClaudeJobSearchRequestDto req)
        {
            // Batch all jobs in one call to save on latency + cost
            var jobSummaries = jobs.Select((j, i) => new
            {
                index = i,
                title = j.Title,
                company = j.Company,
                location = j.Location,
                matchAnalysis = j.MatchAnalysis,
            });

            var prompt = $$"""
Write personalised application content for each job below.

Candidate:
- Roles: {string.Join(", ", req.TargetRoles)}
- Skills: {string.Join(", ", req.CoreSkills.Take(6))}
- Experience: {req.ExperienceYears} years
- From: {req.CandidateLocation} → targeting {req.SearchCountry}
- Certifications: {(req.Certifications.Any() ? string.Join(", ", req.Certifications) : "none")}

Jobs:
{JsonSerializer.Serialize(jobSummaries, _jsonOpts)}

For each job produce:
1. resumeSummary – 3 sentences tailored to that specific job, highlighting matching skills.
2. recruiterMessage – 2 sentences LinkedIn DM to the recruiter. Start with "Hi [Recruiter Name],".
3. coverNote – 4 sentences cover note. Mention relocation readiness and work authorisation.

Respond with ONLY a valid JSON array ordered by index:
[{"index":0,"resumeSummary":"...","recruiterMessage":"...","coverNote":"..."}]
""";

            try
            {
                var response = await CallClaudeAsync(prompt, maxTokens: 6000);
                var clean = CleanJsonResponse(response);
                var items = JsonSerializer.Deserialize<List<ContentItem>>(clean, _jsonOpts);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (item.Index >= 0 && item.Index < jobs.Count)
                        {
                            jobs[item.Index].TailoredResumeSummary = item.ResumeSummary ?? "";
                            jobs[item.Index].RecruiterMessage = item.RecruiterMessage ?? "";
                            jobs[item.Index].CoverNote = item.CoverNote ?? "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Claude content generation failed: {Msg}", ex.Message);
                // Leave fields empty — UI will show a fallback message
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Anthropic Messages API call
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<string> CallClaudeAsync(string userPrompt, int maxTokens = 4096)
        {
            var body = new
            {
                model = _model,
                max_tokens = maxTokens,
                messages = new[]
                {
                    new { role = "user", content = userPrompt }
                }
            };

            var json = JsonSerializer.Serialize(body, _jsonOpts);

            using var request = new HttpRequestMessage(HttpMethod.Post,
                "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Anthropic API error {Status}: {Body}",
                    (int)response.StatusCode, responseBody);
                throw new HttpRequestException(
                    $"Anthropic API returned {(int)response.StatusCode}: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            return text;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────────

        private static string CleanJsonResponse(string raw)
        {
            var s = raw.Trim();
            // Strip ```json ... ``` fences
            if (s.StartsWith("```"))
            {
                var firstNewline = s.IndexOf('\n');
                if (firstNewline >= 0) s = s[(firstNewline + 1)..];
                if (s.EndsWith("```")) s = s[..^3];
            }
            return s.Trim();
        }

        /// <summary>Internal DTO used when deserialising Claude's content-generation response.</summary>
        private class ContentItem
        {
            public int Index { get; set; }
            public string? ResumeSummary { get; set; }
            public string? RecruiterMessage { get; set; }
            public string? CoverNote { get; set; }
        }
    }
}
