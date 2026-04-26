using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// AI-powered Singapore job search service.
    ///
    /// Pipeline:
    ///   1. Pull real-time listings from ExternalJobService (Adzuna + JSearch + NodeFlair)
    ///   2. Filter: last N days, Singapore only, .NET / C# / Backend roles only
    ///   3. Score visa sponsorship likelihood per job
    ///   4. Generate AI content per job (resume summary, recruiter message, cover note)
    ///   5. Assemble curated "Companies Hiring from India" section (static list + live hits)
    /// </summary>
    public class AIJobSearchService : IAIJobSearchService
    {
        private readonly IExternalJobService _externalJobService;
        private readonly ILogger<AIJobSearchService> _logger;

        // ── Companies known to hire Indian developers in Singapore and sponsor EP ──
        private static readonly List<(string Company, string Industry, string EPNotes, string CareersUrl)> KnownSgEPSponsors = new()
        {
            ("DBS Bank",             "Banking & Finance",     "MNC bank; routinely sponsors EP for tech talent. Strong Indian employee community.", "https://jobs.dbs.com"),
            ("OCBC Bank",            "Banking & Finance",     "Actively recruits Indian tech professionals. EP sponsorship well-documented.", "https://www.ocbc.com/group/careers"),
            ("UOB",                  "Banking & Finance",     "Large tech headcount; EP-friendly for experienced hires.", "https://www.uobgroup.com/careers"),
            ("Standard Chartered",   "Banking & Finance",     "Global bank; Singapore tech hub regularly sponsors EP from India.", "https://jobs.sc.com"),
            ("Grab",                 "Tech / Super-App",      "Large eng team with significant Indian headcount. EP sponsorship standard for seniors.", "https://careers.grab.com"),
            ("Sea (Shopee/Garena)",  "Tech / E-Commerce",     "Sea Ltd actively recruits globally. EP common for backend/full-stack hires.", "https://jobs.sea.com"),
            ("Accenture Singapore",  "IT Consulting",         "Frequent EP sponsor; large Indian workforce on site.", "https://www.accenture.com/sg-en/careers"),
            ("Thoughtworks",         "Tech Consulting",       "Known for diverse international hiring. EP sponsorship well-established.", "https://www.thoughtworks.com/careers"),
            ("Cognizant Singapore",  "IT Services",           "Dedicated Singapore delivery centre; hires Indian engineers on EP.", "https://careers.cognizant.com"),
            ("Infosys Singapore",    "IT Services",           "Major Singapore presence. EP sponsorship for experienced hires is routine.", "https://www.infosys.com/careers"),
            ("Wipro Singapore",      "IT Services",           "Active in Singapore; EP sponsorship for skilled tech professionals.", "https://careers.wipro.com"),
            ("TCS Singapore",        "IT Services",           "Large Singapore office; EP sponsorship well-documented for 3+ yr profiles.", "https://www.tcs.com/careers"),
            ("Google Singapore",     "Big Tech",              "Highly competitive but strong EP track record. Indian hires common in eng roles.", "https://careers.google.com"),
            ("Microsoft Singapore",  "Big Tech",              "Engineering hub; EP for senior tech roles is routine.", "https://careers.microsoft.com"),
            ("Salesforce Singapore", "SaaS / CRM",            "Growing Singapore hub. EP sponsorship standard for experienced eng hires.", "https://salesforce.wd12.myworkdayjobs.com/External_Career_Site"),
        };

        // ── Default keywords when no skills are provided ──────────────────────────
        private static readonly HashSet<string> DefaultJobKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "software engineer", "software developer", "backend", "backend developer",
            "full stack", "fullstack", "web developer", "application developer"
        };

        // ── Companies considered large-MNC for higher EP chance ──
        private static readonly HashSet<string> MncCompanies = new(StringComparer.OrdinalIgnoreCase)
        {
            "dbs", "ocbc", "uob", "standard chartered", "citibank", "hsbc", "jp morgan",
            "goldman sachs", "morgan stanley", "grab", "sea", "shopee", "gojek",
            "lazada", "bytedance", "google", "microsoft", "amazon", "meta", "salesforce",
            "accenture", "thoughtworks", "cognizant", "infosys", "wipro", "tcs",
            "capgemini", "deloitte", "pwc", "kpmg", "ernst", "stripe", "twilio",
            "razer", "singtel", "starhub", "m1", "govtech"
        };

        // EP 2024 salary threshold (SGD/month)
        private const decimal EPSalaryThreshold = 5000m;
        private const decimal SPassThreshold = 3150m;

        public AIJobSearchService(IExternalJobService externalJobService, ILogger<AIJobSearchService> logger)
        {
            _externalJobService = externalJobService;
            _logger = logger;
        }

        public async Task<AIJobSearchResultDto> SearchAsync(AIJobSearchRequestDto request)
        {
            // Build primary keyword from target roles + core skills
            var keyword = request.Keyword
                ?? BuildPrimaryKeyword(request.TargetRoles, request.CoreSkills);

            var userSkills = string.Join(",", request.CoreSkills);

            _logger.LogInformation("AI Job Search — keyword: {Keyword} | location: {Loc} | skills: {Skills}",
                keyword, request.SearchLocation, userSkills);

            // ── 1. Fetch real-time jobs ──────────────────────────────────────────
            ExternalJobSearchResultDto externalResult;
            try
            {
                externalResult = await _externalJobService.SearchExternalJobsAsync(
                    searchCountry: request.SearchCountry,
                    searchLocation: request.SearchLocation,
                    keyword: keyword,
                    jobType: "full-time",
                    page: 1,
                    userSkills: userSkills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External job search failed");
                externalResult = new ExternalJobSearchResultDto();
            }

            var rawJobs = externalResult.Jobs ?? new List<ExternalJobDto>();

            // ── 2. Filter & de-duplicate ────────────────────────────────────────
            var keywords = BuildKeywords(request.CoreSkills, request.TargetRoles);
            var cutoff = DateTime.UtcNow.AddDays(-request.PostedWithinDays);
            var filtered = rawJobs
                .Where(j => IsRelevantJob(j, keywords))
                .Where(j => !j.PostedDate.HasValue || j.PostedDate.Value >= cutoff)
                .Where(j => !string.IsNullOrWhiteSpace(j.ApplyUrl))
                .Where(j => MatchesPortalFilter(j, request.JobPortals))
                .GroupBy(j => NormalizeKey(j.Title, j.Company))
                .Select(g => g.OrderByDescending(j => j.MatchScore).First())
                .ToList();

            // Take top N sorted by match score
            var topJobs = filtered
                .OrderByDescending(j => j.MatchScore)
                .ThenBy(j => j.SourcePriority)
                .Take(request.MaxJobs)
                .ToList();

            // ── 3. Build enriched AI results ────────────────────────────────────
            var aiJobs = topJobs
                .Select(j => EnrichJob(j, request))
                .Where(j => j.MatchPercent >= request.MinMatchPercent)
                .ToList();

            // ── 4. Build companies section ──────────────────────────────────────
            var companies = BuildCompaniesSection(aiJobs);

            var result = new AIJobSearchResultDto
            {
                Jobs = aiJobs,
                CompaniesHiringFromIndia = companies,
                TotalFound = aiJobs.Count,
                TotalSearched = rawJobs.Count,
                SourcesUsed = externalResult.SourcesUsed ?? new List<string>(),
                GeneratedAt = DateTime.UtcNow,
                SearchSummary = $"Found {aiJobs.Count} active jobs in {request.SearchLocation} matching your profile " +
                                $"from {rawJobs.Count} total listings across {(externalResult.SourcesUsed?.Count ?? 0)} sources."
            };

            return result;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static string BuildPrimaryKeyword(List<string> roles, List<string> skills)
        {
            var roleHint = roles.FirstOrDefault() ?? ".NET Developer";
            // Include top skills in the keyword for broader coverage
            var skillHint = skills.Take(3).Any()
                ? string.Join(" ", skills.Take(3))
                : "C# ASP.NET Core";
            return $"{roleHint} {skillHint}";
        }

        private static bool IsRelevantJob(ExternalJobDto job, HashSet<string> keywords)
        {
            var haystack = $"{job.Title} {job.Description} {string.Join(" ", job.Skills ?? new())}";
            return keywords.Any(kw => haystack.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Build a dynamic keyword set from the user's skills and target roles.</summary>
        private static HashSet<string> BuildKeywords(List<string> skills, List<string> roles)
        {
            var kws = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in skills) if (!string.IsNullOrWhiteSpace(s)) kws.Add(s.Trim());
            foreach (var r in roles) if (!string.IsNullOrWhiteSpace(r)) kws.Add(r.Trim());

            // Broad catch-all developer terms — keeps the filter from over-rejecting
            // Adzuna descriptions are truncated to 600 chars; many SG job ads lead with
            // role title and company info, so tech keywords may not appear until later.
            kws.Add("software engineer"); kws.Add("software developer");
            kws.Add("developer"); kws.Add("engineer");
            kws.Add("backend"); kws.Add("back-end"); kws.Add("back end");
            kws.Add("full stack"); kws.Add("fullstack"); kws.Add("full-stack");
            kws.Add("IT"); kws.Add("technology");
            return kws;
        }

        private static bool MatchesPortalFilter(ExternalJobDto job, List<string> portals)
        {
            if (portals == null || portals.Count == 0) return true; // all portals

            var src = job.Source ?? "";

            // Trusted agency posts are always included regardless of which portals are ticked.
            if (src.StartsWith("Agency:", StringComparison.OrdinalIgnoreCase)) return true;

            // Free SG portals — always pass when no portals filter or any SG portal is ticked.
            // NodeFlair is Singapore-specific and passes alongside LinkedIn/Indeed/Glassdoor/JobStreet.
            // Note: MyCareersFuture excluded — restricted to Singapore citizens/PRs only.
            var sgFreeSources = new[] { "NodeFlair" };
            if (sgFreeSources.Any(s => src.Equals(s, StringComparison.OrdinalIgnoreCase)))
                return portals.Any(p =>
                    !p.Equals("Adzuna", StringComparison.OrdinalIgnoreCase)); // show unless ONLY Adzuna selected

            // "Company" = direct company career page post (from JSearch direct apply).
            if (src.Equals("Company", StringComparison.OrdinalIgnoreCase))
                return portals.Any(p =>
                    p.Equals("LinkedIn", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("Indeed", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("Glassdoor", StringComparison.OrdinalIgnoreCase));

            return portals.Any(p => src.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeKey(string title, string company)
        {
            var t = new string(title.ToLower().Where(c => char.IsLetterOrDigit(c)).ToArray());
            var c = new string(company.ToLower().Where(c2 => char.IsLetterOrDigit(c2)).ToArray());
            return $"{t}|{c}";
        }

        private AIJobResultDto EnrichJob(ExternalJobDto job, AIJobSearchRequestDto req)
        {
            var (chance, score) = ComputeSponsorshipChance(job);
            var salary = FormatSalary(job);

            return new AIJobResultDto
            {
                Id = job.Id,
                Title = job.Title,
                Company = job.Company,
                CompanyLogo = job.CompanyLogo,
                Location = job.Location,
                Experience = InferExperience(job.Title, job.Description),
                Salary = salary,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                Currency = job.Currency,
                MatchPercent = job.MatchScore,
                VisaSponsorshipChance = chance,
                SponsorshipScore = score,
                ApplyUrl = job.ApplyUrl,
                Source = job.Source,
                SourcePriority = job.SourcePriority,
                IsTrustedAgency = job.IsTrustedAgency,
                IsEasyApply = job.IsEasyApply,
                PostedDate = job.PostedDate,
                Skills = job.Skills ?? new(),
                JobType = job.JobType,
                Description = job.Description,

                // AI-generated content
                TailoredResumeSummary = GenerateResumeSummary(job, req),
                RecruiterMessage = GenerateRecruiterMessage(job, req),
                CoverNote = GenerateCoverNote(job, req)
            };
        }

        // ── Visa Sponsorship Scoring ───────────────────────────────────────────

        private (string Chance, int Score) ComputeSponsorshipChance(ExternalJobDto job)
        {
            int score = 30; // baseline

            // MNC company bonus (+30)
            var companyLower = job.Company.ToLower();
            if (MncCompanies.Any(m => companyLower.Contains(m)))
                score += 30;

            // Salary vs EP threshold
            if (job.SalaryMin.HasValue)
            {
                var monthly = NormalizeToMonthly(job.SalaryMin.Value, job.Currency);
                if (monthly >= EPSalaryThreshold + 2000) score += 25;
                else if (monthly >= EPSalaryThreshold) score += 15;
                else if (monthly >= SPassThreshold) score += 5;
                else score -= 10;
            }
            else
            {
                // Unknown salary — small bonus if source is direct/LinkedIn (likely EP-grade)
                if (job.SourcePriority <= 2) score += 10;
            }

            // Source bonus: company direct posts tend to be higher-value roles
            if (job.SourcePriority == 1) score += 10;
            else if (job.SourcePriority <= 2) score += 5;

            // Trusted agency bonus (+5)
            if (job.IsTrustedAgency) score += 5;

            score = Math.Clamp(score, 5, 95);

            var chance = score >= 70 ? "High"
                       : score >= 45 ? "Medium"
                       : "Low";

            return (chance, score);
        }

        private static decimal NormalizeToMonthly(decimal salary, string currency)
        {
            // If salary looks annual (> 24000 for SGD or similar), divide by 12
            if (string.Equals(currency, "SGD", StringComparison.OrdinalIgnoreCase))
                return salary > 24000 ? salary / 12 : salary;
            // For other currencies treat as-is
            return salary;
        }

        private static string FormatSalary(ExternalJobDto job)
        {
            if (!job.SalaryMin.HasValue && !job.SalaryMax.HasValue)
                return "Not disclosed";

            var cur = job.Currency ?? "SGD";
            var min = job.SalaryMin.HasValue ? $"{cur} {job.SalaryMin:N0}" : "";
            var max = job.SalaryMax.HasValue ? $"{cur} {job.SalaryMax:N0}" : "";

            if (!string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                return $"{min} – {max}/mo";
            return string.IsNullOrEmpty(min) ? $"Up to {max}/mo" : $"From {min}/mo";
        }

        private static string InferExperience(string title, string? description)
        {
            var titleLower = title.ToLower();
            var descLower = (description ?? "").ToLower();

            // Check explicit year mentions in description first (most accurate)
            var yearMatch = System.Text.RegularExpressions.Regex.Match(
                descLower, @"(\d+)\s*[\+\-–]\s*\d*\s*years?");
            if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var minYears))
            {
                if (minYears >= 6) return "6+ years";
                if (minYears >= 4) return "4–6 years";
                if (minYears >= 2) return "2–4 years";
                return "0–2 years";
            }

            // Title-based inference — use exact senior/principal/architect signals,
            // NOT "lead" alone (many "Team Lead" roles accept 3+ yrs experience)
            if (titleLower.Contains("principal") || titleLower.Contains("architect") ||
                titleLower.Contains("staff engineer") || descLower.Contains("8+ years") ||
                descLower.Contains("7+ years") || descLower.Contains("6+ years"))
                return "6+ years";

            if (titleLower.Contains("senior") || titleLower.Contains("sr.") ||
                descLower.Contains("5+ years") || descLower.Contains("4+ years"))
                return "4–6 years";

            if (titleLower.Contains("junior") || titleLower.Contains("jr.") ||
                titleLower.Contains("graduate") || titleLower.Contains("intern") ||
                titleLower.Contains("entry") || titleLower.Contains("fresh"))
                return "0–2 years";

            // Default — most mid-level roles
            return "2–5 years";
        }

        // ── AI Content Generation (smart template-based) ───────────────────────

        private static string GenerateResumeSummary(ExternalJobDto job, AIJobSearchRequestDto req)
        {
            var yoe = req.ExperienceYears;
            var topSkills = req.CoreSkills.Take(4).ToList();
            var certLine = req.Certifications.Any()
                ? $" Certified in {string.Join(", ", req.Certifications)}."
                : "";
            var relevantSkills = topSkills.Any()
                ? string.Join(", ", topSkills)
                : "software development, system design, API development";
            var primaryRole = req.TargetRoles.FirstOrDefault() ?? "Software Engineer";
            var role = job.Title;
            var company = job.Company;

            return $"Experienced {primaryRole} with {yoe}+ years delivering production-grade solutions using {relevantSkills}.{certLine} " +
                   $"Proven track record in building scalable, maintainable systems and collaborative engineering environments. " +
                   $"Targeting the {role} role at {company} to contribute deep technical expertise within a high-performance team. " +
                   $"Open to relocation to {req.SearchLocation}; work authorisation-ready.";
        }

        private static string GenerateRecruiterMessage(ExternalJobDto job, AIJobSearchRequestDto req)
        {
            var role = job.Title;
            var company = job.Company;
            var yoe = req.ExperienceYears;
            var topSkill = req.CoreSkills.FirstOrDefault() ?? "software engineering";
            var cert = req.Certifications.FirstOrDefault();
            var certNote = cert != null ? $" I also hold the {cert} certification." : "";
            var candidateLoc = req.CandidateLocation;

            return $"Hi [Recruiter Name], I came across the {role} opening at {company} and believe it's an excellent match for my background. " +
                   $"I have {yoe}+ years of hands-on experience in {topSkill} and related technologies.{certNote} " +
                   $"I'm currently based in {candidateLoc}, actively targeting {req.SearchLocation} opportunities, and am eligible to work there. " +
                   $"I'd love a quick call to explore how I can contribute — happy to share my resume. Thank you!";
        }

        private static string GenerateCoverNote(ExternalJobDto job, AIJobSearchRequestDto req)
        {
            var role = job.Title;
            var company = job.Company;
            var yoe = req.ExperienceYears;
            var skills = req.CoreSkills.Take(3).ToList();
            var skillStr = skills.Any() ? string.Join(", ", skills) : "software development";
            var certLine = req.Certifications.Any()
                ? $" My {string.Join(", ", req.Certifications)} certification(s) further validate my technical depth."
                : "";

            return $"I am excited to apply for the {role} position at {company}. " +
                   $"With {yoe}+ years of experience building production systems using {skillStr}, " +
                   $"I bring strong alignment to your technical requirements.{certLine} " +
                   $"I thrive in collaborative, agile environments and value clean, well-tested code. " +
                   $"I am currently based in {req.CandidateLocation} and am ready to relocate to {req.SearchLocation} — " +
                   $"motivated to contribute to {company}'s goals from Day 1.";
        }

        // ── Companies Hiring From India Section ────────────────────────────────

        private List<CompanyHiringFromIndiaDto> BuildCompaniesSection(List<AIJobResultDto> aiJobs)
        {
            // Index live jobs by company name for quick lookup
            var liveByCompany = aiJobs
                .GroupBy(j => j.Company.ToLower())
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<CompanyHiringFromIndiaDto>();

            foreach (var (company, industry, epNotes, careersUrl) in KnownSgEPSponsors)
            {
                // Find live matches (partial name match)
                var liveMatches = liveByCompany
                    .Where(kvp => kvp.Key.Contains(company.Split(' ')[0].ToLower(), StringComparison.OrdinalIgnoreCase))
                    .SelectMany(kvp => kvp.Value)
                    .Take(3)
                    .ToList();

                result.Add(new CompanyHiringFromIndiaDto
                {
                    Company = company,
                    Industry = industry,
                    LogoInitial = company[0].ToString().ToUpper(),
                    HiresFromIndia = true,
                    SponsorEP = true,
                    EpNotes = epNotes,
                    MatchingJobTitles = liveMatches.Select(j => j.Title).ToList(),
                    MatchingJobLinks = liveMatches.Select(j => j.ApplyUrl).ToList(),
                    CareersUrl = careersUrl
                });
            }

            // Also surface any live jobs from companies NOT in our curated list
            var curatedLower = KnownSgEPSponsors.Select(x => x.Company.Split(' ')[0].ToLower()).ToHashSet();
            var extraCompanies = aiJobs
                .Where(j => !curatedLower.Any(c => j.Company.ToLower().Contains(c)))
                .GroupBy(j => j.Company)
                .Take(5);

            foreach (var grp in extraCompanies)
            {
                var jobs = grp.ToList();
                var isMnc = MncCompanies.Any(m => grp.Key.ToLower().Contains(m));
                result.Add(new CompanyHiringFromIndiaDto
                {
                    Company = grp.Key,
                    Industry = "Technology",
                    LogoInitial = grp.Key.Length > 0 ? grp.Key[0].ToString().ToUpper() : "?",
                    HiresFromIndia = isMnc,
                    SponsorEP = jobs.Any(j => j.SponsorshipScore >= 50),
                    EpNotes = isMnc
                        ? "Large employer in Singapore; EP sponsorship likely for experienced candidates."
                        : "Check company size and salary — EP sponsorship depends on role level.",
                    MatchingJobTitles = jobs.Select(j => j.Title).Take(3).ToList(),
                    MatchingJobLinks = jobs.Select(j => j.ApplyUrl).Take(3).ToList(),
                    CareersUrl = ""
                });
            }

            return result;
        }
    }
}
