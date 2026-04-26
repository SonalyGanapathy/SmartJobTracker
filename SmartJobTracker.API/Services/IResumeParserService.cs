using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Service for parsing resume files and extracting structured data.
    /// </summary>
    public interface IResumeParserService
    {
        /// <summary>
        /// Parse a resume file (PDF or text) and extract key information.
        /// </summary>
        /// <param name="fileStream">Stream containing the resume file content</param>
        /// <param name="fileName">Name of the resume file</param>
        /// <returns>Parsed resume data</returns>
        Task<ResumeParseResultDto> ParseResumeAsync(Stream fileStream, string fileName);
    }
}
