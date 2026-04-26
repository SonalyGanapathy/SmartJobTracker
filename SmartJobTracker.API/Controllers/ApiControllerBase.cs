using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SmartJobTracker.API.Controllers
{
    /// <summary>
    /// Base controller for all authenticated endpoints.
    /// Provides GetCurrentUserId() to read the user ID from the JWT claim.
    /// </summary>
    [Authorize]
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Returns the authenticated user's profile ID from the JWT sub claim.
        /// Throws UnauthorizedAccessException if the claim is missing or malformed.
        /// </summary>
        protected int GetCurrentUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");

            if (int.TryParse(sub, out var userId))
                return userId;

            throw new UnauthorizedAccessException("User identity claim is missing or invalid.");
        }
    }
}
