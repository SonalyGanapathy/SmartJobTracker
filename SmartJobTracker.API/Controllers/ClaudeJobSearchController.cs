using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Services;

namespace SmartJobTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaudeJobSearchController : ControllerBase
    {
        private readonly IClaudeJobSearchService _claudeService;
        private readonly ILogger<ClaudeJobSearchController> _logger;

        public ClaudeJobSearchController(
            IClaudeJobSearchService claudeService,
            ILogger<ClaudeJobSearchController> logger)
        {
            _claudeService = claudeService;
            _logger = logger;
        }

        /// <summary>
        /// Claude-powered job search.
        ///
        /// Pipeline:
        ///   1. Claude generates optimised search queries from the candidate profile.
        ///   2. Queries run against external job APIs (Adzuna / JSearch / LinkedIn).
        ///   3. Claude analyses every listing, scores fit (0–100) with written explanation.
        ///   4. Claude writes personalised resume summary, recruiter message, and cover note
        ///      for every top result.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ClaudeJobSearchResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Search([FromBody] ClaudeJobSearchRequestDto request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            // Clamp limits
            request.MaxJobs = Math.Clamp(request.MaxJobs, 1, 20);
            request.PostedWithinDays = Math.Clamp(request.PostedWithinDays, 1, 30);

            try
            {
                var result = await _claudeService.SearchAsync(request);
                return Ok(result);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("Anthropic API"))
            {
                _logger.LogError(ex, "Anthropic API call failed");
                return StatusCode(502, new
                {
                    message = "Could not reach the Anthropic API. Check your API key in appsettings.json.",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Claude job search failed unexpectedly");
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }
    }
}
