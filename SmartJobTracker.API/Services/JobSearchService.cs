using Microsoft.EntityFrameworkCore;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;

namespace SmartJobTracker.API.Services
{
    /// <summary>
    /// Job search service with filtering and pagination support.
    /// </summary>
    public class JobSearchService : IJobSearchService
    {
        private readonly AppDbContext _context;
        private readonly IJobMatchingService _matchingService;

        public JobSearchService(AppDbContext context, IJobMatchingService matchingService)
        {
            _context = context;
            _matchingService = matchingService;
        }

        public async Task<PagedResultDto<JobListingDto>> SearchJobsAsync(JobSearchFilterDto filter, int? userProfileId = null)
        {
            var query = _context.JobListings.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(j =>
                    j.Title.ToLower().Contains(searchTerm) ||
                    j.Company.ToLower().Contains(searchTerm) ||
                    j.Description!.ToLower().Contains(searchTerm) ||
                    j.Tags!.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                var location = filter.Location.ToLower();
                query = query.Where(j => j.Location != null && j.Location.ToLower().Contains(location));
            }

            if (!string.IsNullOrWhiteSpace(filter.JobType))
            {
                var jobType = filter.JobType.ToLower();
                query = query.Where(j => j.JobType != null && j.JobType.ToLower().Contains(jobType));
            }

            if (filter.MinSalary.HasValue)
            {
                query = query.Where(j => j.SalaryMax == null || j.SalaryMax >= filter.MinSalary);
            }

            if (filter.MaxSalary.HasValue)
            {
                query = query.Where(j => j.SalaryMin == null || j.SalaryMin <= filter.MaxSalary);
            }

            if (!string.IsNullOrWhiteSpace(filter.Source))
            {
                var source = filter.Source.ToLower();
                query = query.Where(j => j.Source != null && j.Source.ToLower().Contains(source));
            }

            if (filter.IsEasyApply.HasValue && filter.IsEasyApply.Value)
            {
                query = query.Where(j => j.IsEasyApply);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "salary" => query.OrderByDescending(j => j.SalaryMax),
                "matchscore" => query.OrderByDescending(j => j.MatchScore),
                _ => query.OrderByDescending(j => j.PostedDate) // "newest" or default
            };

            // Apply pagination
            var page = Math.Max(filter.Page, 1);
            var pageSize = Math.Min(Math.Max(filter.PageSize, 1), 100); // Cap at 100
            var skip = (page - 1) * pageSize;

            var jobListings = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            // Apply match scoring if user profile provided
            if (userProfileId.HasValue)
            {
                var userProfile = await _context.UserProfiles.FindAsync(userProfileId);
                if (userProfile != null)
                {
                    var rankedJobs = _matchingService.RankJobs(jobListings, userProfile);
                    if (filter.MinMatchScore.HasValue)
                    {
                        rankedJobs = rankedJobs
                            .Where(j => j.MatchScore >= filter.MinMatchScore)
                            .ToList();
                    }
                    return new PagedResultDto<JobListingDto>
                    {
                        Items = rankedJobs,
                        TotalCount = totalCount,
                        Page = page,
                        PageSize = pageSize
                    };
                }
            }

            // Convert to DTOs without match scoring
            var dtos = jobListings.Select(j => new JobListingDto
            {
                Id = j.Id,
                Title = j.Title,
                Company = j.Company,
                Location = j.Location,
                JobType = j.JobType,
                Description = j.Description,
                Requirements = j.Requirements,
                SalaryMin = j.SalaryMin,
                SalaryMax = j.SalaryMax,
                Currency = j.Currency,
                Source = j.Source,
                SourceUrl = j.SourceUrl,
                PostedDate = j.PostedDate,
                IsEasyApply = j.IsEasyApply,
                MatchScore = j.MatchScore,
                Tags = j.Tags,
                CreatedAt = j.CreatedAt
            }).ToList();

            return new PagedResultDto<JobListingDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
