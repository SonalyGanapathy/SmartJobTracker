using SmartJobTracker.API.DTOs;

namespace SmartJobTracker.API.Services
{
    public interface IClaudeJobSearchService
    {
        Task<ClaudeJobSearchResponseDto> SearchAsync(ClaudeJobSearchRequestDto request);
    }
}
