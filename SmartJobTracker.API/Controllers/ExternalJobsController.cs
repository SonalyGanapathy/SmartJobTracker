using Microsoft.AspNetCore.Mvc;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Services;

namespace SmartJobTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalJobsController : ControllerBase
    {
        private readonly IExternalJobService _externalJobService;

        public ExternalJobsController(IExternalJobService externalJobService)
        {
            _externalJobService = externalJobService;
        }

        /// <summary>
        /// Search real-time jobs from external portals.
        /// Sources: JSearch (LinkedIn, Indeed, Glassdoor, company portals) + Adzuna + Careers@Gov.
        /// </summary>
        /// <param name="searchCountry">Country to search jobs in (e.g. "Australia", "United Kingdom", "Singapore")</param>
        /// <param name="searchLocation">Specific city/area within that country (e.g. "Sydney", "London") — leave blank to search whole country</param>
        /// <param name="keyword">Job title or skill keyword</param>
        /// <param name="jobType">full-time | part-time | contract | internship</param>
        /// <param name="page">Page number (default 1)</param>
        /// <param name="userSkills">Comma-separated user skills for match scoring</param>
        [HttpGet]
        [ProducesResponseType(typeof(ExternalJobSearchResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchExternalJobs(
            [FromQuery] string searchCountry = "Singapore",
            [FromQuery] string searchLocation = "",
            [FromQuery] string? keyword = "Dot Net Full Stack Angular C#",
            [FromQuery] string? jobType = null,
            [FromQuery] int page = 1,
            [FromQuery] string? userSkills = null)
        {
            if (page < 1) page = 1;
            if (page > 50) page = 50;

            var result = await _externalJobService.SearchExternalJobsAsync(
                searchCountry, searchLocation, keyword, jobType, page, userSkills);

            return Ok(result);
        }
    }
}
