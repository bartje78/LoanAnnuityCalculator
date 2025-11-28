using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using System.Security.Claims;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [Authorize(Roles = "TenantAdmin,SystemAdmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class TenantAdminController : ControllerBase
    {
        private readonly LoanDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TenantAdminController> _logger;

        public TenantAdminController(
            LoanDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<TenantAdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        private int GetTenantId()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
            {
                throw new UnauthorizedAccessException("TenantId not found in claims");
            }
            return int.Parse(tenantIdClaim);
        }

        private bool IsSystemAdmin()
        {
            return User.IsInRole("SystemAdmin");
        }

        #region User Management

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<object>>> GetTenantUsers()
        {
            var tenantId = GetTenantId();

            var users = await _userManager.Users
                .Where(u => u.TenantId == tenantId)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLoginAt,
                    AddOnCount = u.AddOns.Count(a => a.IsActive)
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("users/{userId}")]
        public async Task<ActionResult<object>> GetUser(string userId)
        {
            var tenantId = GetTenantId();

            var user = await _userManager.Users
                .Where(u => u.Id == userId && u.TenantId == tenantId)
                .Include(u => u.AddOns)
                    .ThenInclude(a => a.AddOn)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt,
                Roles = roles,
                AddOns = user.AddOns.Where(a => a.IsActive).Select(a => new
                {
                    a.UserAddOnId,
                    a.AddOnId,
                    AddOnName = a.AddOn.Name,
                    AddOnDescription = a.AddOn.Description,
                    a.AssignedAt
                })
            });
        }

        [HttpPost("users")]
        public async Task<ActionResult<object>> CreateUser([FromBody] CreateUserRequest request)
        {
            var tenantId = GetTenantId();

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { error = "Email already exists" });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                TenantId = tenantId,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "User");

            _logger.LogInformation($"User {user.Email} created for tenant {tenantId}");

            return CreatedAtAction(nameof(GetUser), new { userId = user.Id }, new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName
            });
        }

        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
        {
            var tenantId = GetTenantId();

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.IsActive = request.IsActive;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            return NoContent();
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            var tenantId = GetTenantId();

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = false;

            // Revoke all add-ons
            var userAddOns = await _context.UserAddOns
                .Where(a => a.UserId == userId && a.IsActive)
                .ToListAsync();

            foreach (var addOn in userAddOns)
            {
                addOn.IsActive = false;
                addOn.RevokedAt = DateTime.UtcNow;
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Add-on Assignment

        [HttpGet("users/{userId}/addons")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserAddOns(string userId)
        {
            var tenantId = GetTenantId();

            // Verify user belongs to tenant
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

            if (user == null)
            {
                return NotFound();
            }

            var userAddOns = await _context.UserAddOns
                .Where(a => a.UserId == userId && a.IsActive)
                .Include(a => a.AddOn)
                    .ThenInclude(ao => ao.Permissions)
                .Select(a => new
                {
                    a.UserAddOnId,
                    a.AddOnId,
                    AddOnName = a.AddOn.Name,
                    AddOnDescription = a.AddOn.Description,
                    a.AssignedAt,
                    Permissions = a.AddOn.Permissions.Select(p => new
                    {
                        p.PermissionKey,
                        p.PermissionName,
                        p.Description
                    })
                })
                .ToListAsync();

            return Ok(userAddOns);
        }

        [HttpPost("users/{userId}/addons")]
        public async Task<ActionResult> AssignAddOn(string userId, [FromBody] AssignAddOnRequest request)
        {
            var tenantId = GetTenantId();
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Verify user belongs to tenant
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Verify add-on exists and is active
            var addOn = await _context.PlanAddOns.FindAsync(request.AddOnId);
            if (addOn == null || !addOn.IsActive)
            {
                return BadRequest(new { error = "Add-on not found or inactive" });
            }

            // Check if already assigned
            var existing = await _context.UserAddOns
                .FirstOrDefaultAsync(a => a.UserId == userId && a.AddOnId == request.AddOnId && a.IsActive);

            if (existing != null)
            {
                return BadRequest(new { error = "Add-on already assigned to user" });
            }

            var userAddOn = new UserAddOn
            {
                UserId = userId,
                AddOnId = request.AddOnId,
                TenantId = tenantId,
                IsActive = true,
                AssignedByUserId = currentUserId
            };

            _context.UserAddOns.Add(userAddOn);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Add-on {request.AddOnId} assigned to user {userId} by {currentUserId}");

            return Ok(new { message = "Add-on assigned successfully", userAddOnId = userAddOn.UserAddOnId });
        }

        [HttpDelete("users/{userId}/addons/{userAddOnId}")]
        public async Task<IActionResult> RevokeAddOn(string userId, int userAddOnId)
        {
            var tenantId = GetTenantId();

            var userAddOn = await _context.UserAddOns
                .FirstOrDefaultAsync(a => a.UserAddOnId == userAddOnId && 
                                         a.UserId == userId && 
                                         a.TenantId == tenantId &&
                                         a.IsActive);

            if (userAddOn == null)
            {
                return NotFound();
            }

            userAddOn.IsActive = false;
            userAddOn.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Add-on {userAddOn.AddOnId} revoked from user {userId}");

            return NoContent();
        }

        #endregion

        #region Pricing View

        [HttpGet("pricing-summary")]
        public async Task<ActionResult<object>> GetPricingSummary()
        {
            var tenantId = GetTenantId();

            // Count active users
            var activeUserCount = await _userManager.Users
                .CountAsync(u => u.TenantId == tenantId && u.IsActive);

            // Count add-on assignments per add-on
            var addOnAssignments = await _context.UserAddOns
                .Where(a => a.TenantId == tenantId && a.IsActive)
                .GroupBy(a => a.AddOnId)
                .Select(g => new
                {
                    AddOnId = g.Key,
                    Quantity = g.Count()
                })
                .ToListAsync();

            // Get applicable user tier
            var userTier = await _context.UserPricingTiers
                .Where(t => t.IsActive && 
                           t.MinUsers <= activeUserCount && 
                           (t.MaxUsers == null || t.MaxUsers >= activeUserCount))
                .OrderBy(t => t.MinUsers)
                .FirstOrDefaultAsync();

            if (userTier == null)
            {
                return Ok(new
                {
                    ActiveUsers = activeUserCount,
                    Message = "No pricing tier configured for this user count. Please contact support."
                });
            }

            var monthlyUserCost = userTier.BaseMonthlyPricePerUser * activeUserCount;
            var annualUserCost = userTier.BaseAnnualPricePerUser * activeUserCount;

            // Calculate add-on costs
            decimal monthlyAddOnCost = 0;
            decimal annualAddOnCost = 0;
            var addOnBreakdown = new List<object>();

            foreach (var assignment in addOnAssignments)
            {
                var addOn = await _context.PlanAddOns.FindAsync(assignment.AddOnId);
                var tier = await _context.AddOnPricingTiers
                    .Where(t => t.IsActive && 
                               t.AddOnId == assignment.AddOnId &&
                               t.MinQuantity <= assignment.Quantity && 
                               (t.MaxQuantity == null || t.MaxQuantity >= assignment.Quantity))
                    .OrderBy(t => t.MinQuantity)
                    .FirstOrDefaultAsync();

                if (tier != null)
                {
                    var monthlyCost = tier.MonthlyPricePerAssignment * assignment.Quantity;
                    var annualCost = tier.AnnualPricePerAssignment * assignment.Quantity;

                    monthlyAddOnCost += monthlyCost;
                    annualAddOnCost += annualCost;

                    addOnBreakdown.Add(new
                    {
                        AddOnId = assignment.AddOnId,
                        AddOnName = addOn?.Name,
                        Quantity = assignment.Quantity,
                        PricePerAssignment = tier.MonthlyPricePerAssignment,
                        MonthlyCost = monthlyCost,
                        AnnualCost = annualCost,
                        DiscountPercentage = tier.DiscountPercentage
                    });
                }
            }

            // Get or create pricing summary
            var summary = await _context.TenantPricingSummaries
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.CalculatedAt)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                TenantId = tenantId,
                ActiveUsers = activeUserCount,
                UserTier = new
                {
                    userTier.TierId,
                    userTier.MinUsers,
                    userTier.MaxUsers,
                    PricePerUser = userTier.BaseMonthlyPricePerUser,
                    userTier.DiscountPercentage
                },
                MonthlyUserCost = monthlyUserCost,
                AnnualUserCost = annualUserCost,
                MonthlyAddOnCost = monthlyAddOnCost,
                AnnualAddOnCost = annualAddOnCost,
                TotalMonthly = monthlyUserCost + monthlyAddOnCost,
                TotalAnnual = annualUserCost + annualAddOnCost,
                AddOnBreakdown = addOnBreakdown,
                TenantAgreed = summary?.TenantAgreed ?? false,
                AgreedAt = summary?.AgreedAt
            });
        }

        [HttpPost("agree-pricing")]
        public async Task<ActionResult> AgreeToPricing()
        {
            var tenantId = GetTenantId();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Get current pricing summary directly
            var activeUserCount = await _userManager.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive);
            
            // Count add-on assignments per add-on
            var addOnAssignments = await _context.UserAddOns
                .Where(a => a.TenantId == tenantId && a.IsActive)
                .GroupBy(a => a.AddOnId)
                .Select(g => new
                {
                    AddOnId = g.Key,
                    Quantity = g.Count()
                })
                .ToListAsync();

            // Get applicable user tier
            var userTier = await _context.UserPricingTiers
                .Where(t => t.IsActive && 
                           t.MinUsers <= activeUserCount && 
                           (t.MaxUsers == null || t.MaxUsers >= activeUserCount))
                .OrderBy(t => t.MinUsers)
                .FirstOrDefaultAsync();

            if (userTier == null)
            {
                return BadRequest(new { error = "No pricing tier configured" });
            }

            var monthlyUserCost = userTier.BaseMonthlyPricePerUser * activeUserCount;
            var annualUserCost = userTier.BaseAnnualPricePerUser * activeUserCount;

            // Calculate add-on costs
            decimal monthlyAddOnCost = 0;
            decimal annualAddOnCost = 0;

            foreach (var assignment in addOnAssignments)
            {
                var tier = await _context.AddOnPricingTiers
                    .Where(t => t.IsActive && 
                               t.AddOnId == assignment.AddOnId &&
                               t.MinQuantity <= assignment.Quantity && 
                               (t.MaxQuantity == null || t.MaxQuantity >= assignment.Quantity))
                    .OrderBy(t => t.MinQuantity)
                    .FirstOrDefaultAsync();

                if (tier != null)
                {
                    monthlyAddOnCost += tier.MonthlyPricePerAssignment * assignment.Quantity;
                    annualAddOnCost += tier.AnnualPricePerAssignment * assignment.Quantity;
                }
            }

            // Create pricing summary record
            var summary = new TenantPricingSummary
            {
                TenantId = tenantId,
                ActiveUserCount = activeUserCount,
                MonthlyUserCost = monthlyUserCost,
                AnnualUserCost = annualUserCost,
                MonthlyAddOnCost = monthlyAddOnCost,
                AnnualAddOnCost = annualAddOnCost,
                TotalMonthly = monthlyUserCost + monthlyAddOnCost,
                TotalAnnual = annualUserCost + annualAddOnCost,
                TenantAgreed = true,
                AgreedAt = DateTime.UtcNow,
                AgreedByUserId = userId
            };

            _context.TenantPricingSummaries.Add(summary);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Tenant {tenantId} agreed to pricing by user {userId}");

            return Ok(new { message = "Pricing agreement recorded", summaryId = summary.SummaryId });
        }

        #endregion

        #region Available Add-ons

        [HttpGet("available-addons")]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailableAddOns()
        {
            var addOns = await _context.PlanAddOns
                .Where(a => a.IsActive)
                .Include(a => a.Permissions)
                .Include(a => a.PricingTiers.Where(t => t.IsActive))
                .Select(a => new
                {
                    a.AddOnId,
                    a.Name,
                    a.Description,
                    a.FeatureKey,
                    Permissions = a.Permissions.Select(p => new
                    {
                        p.PermissionKey,
                        p.PermissionName,
                        p.Description
                    }),
                    PricingTiers = a.PricingTiers
                        .OrderBy(t => t.MinQuantity)
                        .Select(t => new
                        {
                            t.MinQuantity,
                            t.MaxQuantity,
                            t.MonthlyPricePerAssignment,
                            t.AnnualPricePerAssignment,
                            t.DiscountPercentage
                        })
                })
                .ToListAsync();

            return Ok(addOns);
        }

        #endregion
    }

    #region Request Models

    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class AssignAddOnRequest
    {
        public int AddOnId { get; set; }
    }

    #endregion
}
