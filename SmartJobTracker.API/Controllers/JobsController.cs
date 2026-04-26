using Microsoft.AspNetCore.Mvc;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.DTOs;
using SmartJobTracker.API.Entities;
using System.Linq;
using System;

namespace SmartJobTracker.API.Controllers
{
    /// <summary>
    /// Legacy Jobs controller. Use /api/joblistings instead.
    /// Kept for backward compatibility with existing integrations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public JobsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetJobs()
        {
            return Ok(_context.Jobs.ToList());
        }

        [HttpPost]
        public IActionResult AddJob(CreateJobDto dto)
        {
            var job = new Job
            {
                Company = dto.Company,
                Role = dto.Role,
                Status = dto.Status,
                AppliedDate = DateTime.Now
            };

            _context.Jobs.Add(job);
            _context.SaveChanges();

            return Ok(job);
        }
    }
}