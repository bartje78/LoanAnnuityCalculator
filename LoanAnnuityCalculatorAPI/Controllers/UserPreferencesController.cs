using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using System.Security.Claims;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserPreferencesController : ControllerBase
    {
        private readonly LoanDbContext _context;
        private readonly ILogger<UserPreferencesController> _logger;

        public UserPreferencesController(LoanDbContext context, ILogger<UserPreferencesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        /// <summary>
        /// Get a specific user preference by key
        /// </summary>
        [HttpGet("{key}")]
        public async Task<ActionResult<UserPreference>> GetPreference(string key)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var preference = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.PreferenceKey == key);

            if (preference == null)
            {
                return NotFound();
            }

            return preference;
        }

        /// <summary>
        /// Get all preferences for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserPreference>>> GetAllPreferences()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var preferences = await _context.UserPreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return Ok(preferences);
        }

        /// <summary>
        /// Save or update a user preference
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserPreference>> SavePreference([FromBody] SavePreferenceRequest request)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var existingPreference = await _context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.PreferenceKey == request.Key);

                if (existingPreference != null)
                {
                    // Update existing preference
                    existingPreference.PreferenceValue = request.Value;
                    existingPreference.UpdatedAt = DateTime.UtcNow;
                    _context.UserPreferences.Update(existingPreference);
                }
                else
                {
                    // Create new preference
                    var newPreference = new UserPreference
                    {
                        UserId = userId,
                        PreferenceKey = request.Key,
                        PreferenceValue = request.Value,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserPreferences.Add(newPreference);
                }

                await _context.SaveChangesAsync();

                var savedPreference = await _context.UserPreferences
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.PreferenceKey == request.Key);

                return Ok(savedPreference);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving user preference for user {UserId}, key {Key}", userId, request.Key);
                return StatusCode(500, new { error = "Failed to save preference", details = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Delete a user preference
        /// </summary>
        [HttpDelete("{key}")]
        public async Task<IActionResult> DeletePreference(string key)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var preference = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.PreferenceKey == key);

            if (preference == null)
            {
                return NotFound();
            }

            _context.UserPreferences.Remove(preference);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class SavePreferenceRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
