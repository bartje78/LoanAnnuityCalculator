using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Services;
using System.Security.Claims;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FundController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;
        private readonly ITenantService _tenantService;
        private readonly ILogger<FundController> _logger;

        public FundController(
            LoanDbContext dbContext,
            ITenantService tenantService,
            ILogger<FundController> logger)
        {
            _dbContext = dbContext;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>
        /// Get all funds the current user has access to
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<Fund>>> GetUserFunds()
        {
            var fundIds = await _tenantService.GetUserFundIds();
            
            var funds = await _dbContext.Funds
                .Where(f => fundIds.Contains(f.FundId))
                .Include(f => f.Tenant)
                .ToListAsync();

            return Ok(funds);
        }

        /// <summary>
        /// Get specific fund (if user has access)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Fund>> GetFund(int id)
        {
            if (!await _tenantService.HasFundAccess(id))
            {
                return Forbid();
            }

            var fund = await _dbContext.Funds
                .Include(f => f.Tenant)
                .FirstOrDefaultAsync(f => f.FundId == id);

            if (fund == null)
            {
                return NotFound();
            }

            return Ok(fund);
        }

        /// <summary>
        /// Create new fund (TenantAdmin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = TenantRoles.TenantAdmin)]
        public async Task<ActionResult<Fund>> CreateFund([FromBody] CreateFundRequest request)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "No tenant context" });
            }

            var fund = new Fund
            {
                TenantId = tenantId.Value,
                Name = request.Name,
                Description = request.Description,
                FundCode = request.FundCode,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Funds.Add(fund);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Fund {FundName} created for tenant {TenantId}", fund.Name, tenantId.Value);

            return CreatedAtAction(nameof(GetFund), new { id = fund.FundId }, fund);
        }

        /// <summary>
        /// Update fund (TenantAdmin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = TenantRoles.TenantAdmin)]
        public async Task<ActionResult<Fund>> UpdateFund(int id, [FromBody] UpdateFundRequest request)
        {
            if (!await _tenantService.ValidateFundBelongsToTenant(id))
            {
                return Forbid();
            }

            var fund = await _dbContext.Funds.FindAsync(id);
            if (fund == null)
            {
                return NotFound();
            }

            fund.Name = request.Name;
            fund.Description = request.Description;
            fund.FundCode = request.FundCode;
            fund.IsActive = request.IsActive;

            if (!request.IsActive && !fund.ClosedAt.HasValue)
            {
                fund.ClosedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Fund {FundId} updated", id);

            return Ok(fund);
        }

        /// <summary>
        /// Close fund (TenantAdmin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = TenantRoles.TenantAdmin)]
        public async Task<ActionResult> CloseFund(int id)
        {
            if (!await _tenantService.ValidateFundBelongsToTenant(id))
            {
                return Forbid();
            }

            var fund = await _dbContext.Funds.FindAsync(id);
            if (fund == null)
            {
                return NotFound();
            }

            fund.IsActive = false;
            fund.ClosedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogWarning("Fund {FundId} closed", id);

            return NoContent();
        }

        /// <summary>
        /// Grant user access to fund (TenantAdmin only)
        /// </summary>
        [HttpPost("{fundId}/users/{userId}")]
        [Authorize(Roles = TenantRoles.TenantAdmin)]
        public async Task<ActionResult> GrantFundAccess(int fundId, string userId, [FromBody] GrantAccessRequest request)
        {
            if (!await _tenantService.ValidateFundBelongsToTenant(fundId))
            {
                return Forbid();
            }

            // Check if user belongs to same tenant
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var tenantId = _tenantService.GetCurrentTenantId();
            if (user.TenantId != tenantId)
            {
                return Forbid();
            }

            // Check if access already exists
            var existingAccess = await _dbContext.UserFundAccesses
                .FirstOrDefaultAsync(ufa => ufa.UserId == userId && ufa.FundId == fundId);

            if (existingAccess != null)
            {
                // Update existing access
                existingAccess.Role = request.Role;
                existingAccess.RevokedAt = null;
            }
            else
            {
                // Create new access
                var access = new UserFundAccess
                {
                    UserId = userId,
                    FundId = fundId,
                    Role = request.Role,
                    GrantedAt = DateTime.UtcNow
                };
                _dbContext.UserFundAccesses.Add(access);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {UserId} granted {Role} access to fund {FundId}", userId, request.Role, fundId);

            return Ok();
        }

        /// <summary>
        /// Revoke user access to fund (TenantAdmin only)
        /// </summary>
        [HttpDelete("{fundId}/users/{userId}")]
        [Authorize(Roles = TenantRoles.TenantAdmin)]
        public async Task<ActionResult> RevokeFundAccess(int fundId, string userId)
        {
            if (!await _tenantService.ValidateFundBelongsToTenant(fundId))
            {
                return Forbid();
            }

            var access = await _dbContext.UserFundAccesses
                .FirstOrDefaultAsync(ufa => ufa.UserId == userId && ufa.FundId == fundId);

            if (access == null)
            {
                return NotFound();
            }

            access.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogWarning("User {UserId} access to fund {FundId} revoked", userId, fundId);

            return NoContent();
        }

        /// <summary>
        /// Get all users with access to a fund (TenantAdmin only)
        /// </summary>
        [HttpGet("{fundId}/users")]
        [Authorize(Roles = TenantRoles.TenantAdmin)]
        public async Task<ActionResult<List<object>>> GetFundUsers(int fundId)
        {
            if (!await _tenantService.ValidateFundBelongsToTenant(fundId))
            {
                return Forbid();
            }

            var users = await _dbContext.UserFundAccesses
                .Where(ufa => ufa.FundId == fundId && ufa.RevokedAt == null)
                .Include(ufa => ufa.User)
                .Select(ufa => new
                {
                    ufa.User.Id,
                    ufa.User.UserName,
                    ufa.User.Email,
                    ufa.User.FirstName,
                    ufa.User.LastName,
                    ufa.Role,
                    ufa.GrantedAt
                })
                .ToListAsync();

            return Ok(users);
        }
    }

    public class CreateFundRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? FundCode { get; set; }
    }

    public class UpdateFundRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? FundCode { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class GrantAccessRequest
    {
        public string Role { get; set; } = FundRoles.Viewer;
    }
}
