using Microsoft.AspNetCore.Mvc;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Services;

namespace SmartJobTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIJobSearchController : ControllerBase
    {
        private readonly IAIJobSearchService _aiJobSearchService;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AIJobSearchController(IAIJobSearchService aiJobSearchService,
            IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _aiJobSearchService = aiJobSearchService;
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Diagnostic: Tests raw Adzuna API connectivity and returns the response.
        /// Visit /api/aijobsearch/ping-adzuna in browser or Swagger to debug.
        /// </summary>
        [HttpGet("ping-adzuna")]
        public async Task<IActionResult> PingAdzuna([FromQuery] string country = "sg", [FromQuery] string keyword = "software engineer")
        {
            var appId = _config["Adzuna:AppId"];
            var appKey = _config["Adzuna:AppKey"];
            var url = $"https://api.adzuna.com/v1/api/jobs/{country}/search/1" +
                      $"?app_id={appId}&app_key={appKey}&results_per_page=5&what={Uri.EscapeDataString(keyword)}&content-type=application/json";
            try
            {
                var resp = await _httpClient.GetAsync(url);
                var body = await resp.Content.ReadAsStringAsync();
                return Ok(new
                {
                    StatusCode = (int)resp.StatusCode,
                    IsSuccess = resp.IsSuccessStatusCode,
                    AppId = appId,
                    Url = url.Replace(appKey!, "***"),
                    ResponsePreview = body.Length > 800 ? body[..800] + "…" : body
                });
            }
            catch (Exception ex)
            {
                return Ok(new { Error = ex.Message, ExType = ex.GetType().Name });
            }
        }

        /// <summary>
        /// AI-powered Singapore job search.
        ///
        /// Returns real, active job listings enriched with:
        ///   - Tailored resume summary per job
        ///   - Recruiter LinkedIn outreach message per job
        ///   - Quick cover note per job
        ///   - EP/S Pass visa sponsorship chance score
        ///   - Curated list of Singapore companies known to hire from India
        ///
        /// Only returns jobs with direct apply links posted within the last 14 days.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AIJobSearchResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromBody] AIJobSearchRequestDto request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            // Clamp limits
            if (request.MaxJobs < 1) request.MaxJobs = 20;
            if (request.MaxJobs > 40) request.MaxJobs = 40;
            if (request.PostedWithinDays < 1) request.PostedWithinDays = 14;
            if (request.PostedWithinDays > 30) request.PostedWithinDays = 30;

            var result = await _aiJobSearchService.SearchAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Default search using the built-in .NET / Singapore / India profile.
        /// Convenience GET endpoint for quick testing.
        /// </summary>
        [HttpGet("default")]
        [ProducesResponseType(typeof(AIJobSearchResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> DefaultSearch()
        {
            var defaultRequest = new AIJobSearchRequestDto
            {
                CandidateLocation = "India",
                ExperienceYears = 3,
                TargetRoles = new() { ".NET Developer", "Backend Engineer", "Full Stack Developer", "Software Engineer" },
                CoreSkills = new() { "ASP.NET Core", "C#", "SQL Server", "Angular", "Azure", "Microservices" },
                Certifications = new() { "AZ-400", "AZ-305", "AZ-104" },
                SearchLocation = "Singapore",
                MaxJobs = 30,
                PostedWithinDays = 14
            };

            var result = await _aiJobSearchService.SearchAsync(defaultRequest);
            return Ok(result);
        }
    }
}
