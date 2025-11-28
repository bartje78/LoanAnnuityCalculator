using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    [ApiController]
    [Route("api/[controller]")]
    public class PricingManagementController : ControllerBase
    {
        private readonly LoanDbContext _context;
        private readonly ILogger<PricingManagementController> _logger;

        public PricingManagementController(LoanDbContext context, ILogger<PricingManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region User Pricing Tiers

        [HttpGet("user-pricing-tiers")]
        public async Task<ActionResult<IEnumerable<UserPricingTier>>> GetUserPricingTiers()
        {
            var tiers = await _context.UserPricingTiers
                .OrderBy(t => t.MinUsers)
                .ToListAsync();
            return Ok(tiers);
        }

        [HttpPost("user-pricing-tiers")]
        public async Task<ActionResult<UserPricingTier>> CreateUserPricingTier([FromBody] UserPricingTier tier)
        {
            // Validate no overlap
            var hasOverlap = await _context.UserPricingTiers
                .Where(t => t.IsActive && t.TierId != tier.TierId)
                .AnyAsync(t => 
                    (tier.MinUsers >= t.MinUsers && tier.MinUsers <= (t.MaxUsers ?? int.MaxValue)) ||
                    ((tier.MaxUsers ?? int.MaxValue) >= t.MinUsers && (tier.MaxUsers ?? int.MaxValue) <= (t.MaxUsers ?? int.MaxValue))
                );

            if (hasOverlap)
            {
                return BadRequest(new { error = "User range overlaps with existing tier" });
            }

            _context.UserPricingTiers.Add(tier);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserPricingTiers), new { id = tier.TierId }, tier);
        }

        [HttpPut("user-pricing-tiers/{id}")]
        public async Task<IActionResult> UpdateUserPricingTier(int id, [FromBody] UserPricingTier tier)
        {
            if (id != tier.TierId)
            {
                return BadRequest();
            }

            _context.Entry(tier).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.UserPricingTiers.AnyAsync(t => t.TierId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("user-pricing-tiers/{id}")]
        public async Task<IActionResult> DeleteUserPricingTier(int id)
        {
            var tier = await _context.UserPricingTiers.FindAsync(id);
            if (tier == null)
            {
                return NotFound();
            }

            tier.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Add-on Pricing Tiers

        [HttpGet("addon-pricing-tiers")]
        public async Task<ActionResult<IEnumerable<AddOnPricingTier>>> GetAddOnPricingTiers()
        {
            var tiers = await _context.AddOnPricingTiers
                .Include(t => t.AddOn)
                .OrderBy(t => t.AddOnId)
                .ThenBy(t => t.MinQuantity)
                .ToListAsync();
            return Ok(tiers);
        }

        [HttpGet("addon-pricing-tiers/{addOnId}")]
        public async Task<ActionResult<IEnumerable<AddOnPricingTier>>> GetAddOnPricingTiersByAddOn(int addOnId)
        {
            var tiers = await _context.AddOnPricingTiers
                .Where(t => t.AddOnId == addOnId && t.IsActive)
                .OrderBy(t => t.MinQuantity)
                .ToListAsync();
            return Ok(tiers);
        }

        [HttpPost("addon-pricing-tiers")]
        public async Task<ActionResult<AddOnPricingTier>> CreateAddOnPricingTier([FromBody] AddOnPricingTier tier)
        {
            // Validate no overlap for this add-on
            var hasOverlap = await _context.AddOnPricingTiers
                .Where(t => t.IsActive && t.AddOnId == tier.AddOnId && t.TierId != tier.TierId)
                .AnyAsync(t => 
                    (tier.MinQuantity >= t.MinQuantity && tier.MinQuantity <= (t.MaxQuantity ?? int.MaxValue)) ||
                    ((tier.MaxQuantity ?? int.MaxValue) >= t.MinQuantity && (tier.MaxQuantity ?? int.MaxValue) <= (t.MaxQuantity ?? int.MaxValue))
                );

            if (hasOverlap)
            {
                return BadRequest(new { error = "Quantity range overlaps with existing tier for this add-on" });
            }

            _context.AddOnPricingTiers.Add(tier);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAddOnPricingTiersByAddOn), new { addOnId = tier.AddOnId }, tier);
        }

        [HttpPut("addon-pricing-tiers/{id}")]
        public async Task<IActionResult> UpdateAddOnPricingTier(int id, [FromBody] AddOnPricingTier tier)
        {
            if (id != tier.TierId)
            {
                return BadRequest();
            }

            _context.Entry(tier).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.AddOnPricingTiers.AnyAsync(t => t.TierId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("addon-pricing-tiers/{id}")]
        public async Task<IActionResult> DeleteAddOnPricingTier(int id)
        {
            var tier = await _context.AddOnPricingTiers.FindAsync(id);
            if (tier == null)
            {
                return NotFound();
            }

            tier.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Add-on Permissions

        [HttpGet("addon-permissions")]
        public async Task<ActionResult<IEnumerable<AddOnPermission>>> GetAddOnPermissions()
        {
            var permissions = await _context.AddOnPermissions
                .Include(p => p.AddOn)
                .ToListAsync();
            return Ok(permissions);
        }

        [HttpGet("addon-permissions/{addOnId}")]
        public async Task<ActionResult<IEnumerable<AddOnPermission>>> GetAddOnPermissionsByAddOn(int addOnId)
        {
            var permissions = await _context.AddOnPermissions
                .Where(p => p.AddOnId == addOnId)
                .ToListAsync();
            return Ok(permissions);
        }

        [HttpPost("addon-permissions")]
        public async Task<ActionResult<AddOnPermission>> CreateAddOnPermission([FromBody] AddOnPermission permission)
        {
            _context.AddOnPermissions.Add(permission);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAddOnPermissionsByAddOn), new { addOnId = permission.AddOnId }, permission);
        }

        [HttpPut("addon-permissions/{id}")]
        public async Task<IActionResult> UpdateAddOnPermission(int id, [FromBody] AddOnPermission permission)
        {
            if (id != permission.AddOnPermissionId)
            {
                return BadRequest();
            }

            _context.Entry(permission).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.AddOnPermissions.AnyAsync(p => p.AddOnPermissionId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("addon-permissions/{id}")]
        public async Task<IActionResult> DeleteAddOnPermission(int id)
        {
            var permission = await _context.AddOnPermissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            _context.AddOnPermissions.Remove(permission);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region Pricing Calculation

        /// <summary>
        /// Calculate pricing for a given number of users and add-on assignments
        /// </summary>
        [HttpPost("calculate-pricing")]
        public async Task<ActionResult> CalculatePricing([FromBody] PricingCalculationRequest request)
        {
            // Get applicable user tier
            var userTier = await _context.UserPricingTiers
                .Where(t => t.IsActive && 
                           t.MinUsers <= request.UserCount && 
                           (t.MaxUsers == null || t.MaxUsers >= request.UserCount))
                .OrderBy(t => t.MinUsers)
                .FirstOrDefaultAsync();

            if (userTier == null)
            {
                return BadRequest(new { error = "No pricing tier found for user count" });
            }

            var monthlyUserCost = userTier.BaseMonthlyPricePerUser * request.UserCount;
            var annualUserCost = userTier.BaseAnnualPricePerUser * request.UserCount;

            // Calculate add-on costs
            decimal monthlyAddOnCost = 0;
            decimal annualAddOnCost = 0;
            var addOnBreakdown = new List<object>();

            foreach (var assignment in request.AddOnAssignments)
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
                    var monthlyCost = tier.MonthlyPricePerAssignment * assignment.Quantity;
                    var annualCost = tier.AnnualPricePerAssignment * assignment.Quantity;

                    monthlyAddOnCost += monthlyCost;
                    annualAddOnCost += annualCost;

                    addOnBreakdown.Add(new
                    {
                        AddOnId = assignment.AddOnId,
                        Quantity = assignment.Quantity,
                        MonthlyCost = monthlyCost,
                        AnnualCost = annualCost,
                        PricePerAssignment = tier.MonthlyPricePerAssignment,
                        DiscountPercentage = tier.DiscountPercentage
                    });
                }
            }

            return Ok(new
            {
                UserCount = request.UserCount,
                UserTier = new
                {
                    TierId = userTier.TierId,
                    MinUsers = userTier.MinUsers,
                    MaxUsers = userTier.MaxUsers,
                    PricePerUser = userTier.BaseMonthlyPricePerUser,
                    DiscountPercentage = userTier.DiscountPercentage
                },
                MonthlyUserCost = monthlyUserCost,
                AnnualUserCost = annualUserCost,
                MonthlyAddOnCost = monthlyAddOnCost,
                AnnualAddOnCost = annualAddOnCost,
                TotalMonthly = monthlyUserCost + monthlyAddOnCost,
                TotalAnnual = annualUserCost + annualAddOnCost,
                AddOnBreakdown = addOnBreakdown
            });
        }

        #endregion

        #region Seeding

        /// <summary>
        /// Seed default pricing tiers with realistic SaaS pricing
        /// </summary>
        [HttpPost("seed-pricing-tiers")]
        public async Task<ActionResult> SeedPricingTiers()
        {
            try
            {
                // Check if already seeded
                var existingUserTiers = await _context.UserPricingTiers.AnyAsync();
                if (existingUserTiers)
                {
                    return BadRequest(new { error = "Pricing tiers already exist. Delete existing tiers first if you want to reseed." });
                }

                // Seed User Pricing Tiers
                var userTiers = new List<UserPricingTier>
                {
                    new UserPricingTier
                    {
                        MinUsers = 1,
                        MaxUsers = 9,
                        BaseMonthlyPricePerUser = 50m,
                        BaseAnnualPricePerUser = 540m, // 10% discount for annual
                        DiscountPercentage = 0,
                        IsActive = true
                    },
                    new UserPricingTier
                    {
                        MinUsers = 10,
                        MaxUsers = 19,
                        BaseMonthlyPricePerUser = 45m,
                        BaseAnnualPricePerUser = 486m, // 10% discount
                        DiscountPercentage = 10,
                        IsActive = true
                    },
                    new UserPricingTier
                    {
                        MinUsers = 20,
                        MaxUsers = 49,
                        BaseMonthlyPricePerUser = 42.5m,
                        BaseAnnualPricePerUser = 459m, // 15% discount
                        DiscountPercentage = 15,
                        IsActive = true
                    },
                    new UserPricingTier
                    {
                        MinUsers = 50,
                        MaxUsers = 99,
                        BaseMonthlyPricePerUser = 40m,
                        BaseAnnualPricePerUser = 432m, // 20% discount
                        DiscountPercentage = 20,
                        IsActive = true
                    },
                    new UserPricingTier
                    {
                        MinUsers = 100,
                        MaxUsers = null, // Unlimited
                        BaseMonthlyPricePerUser = 35m,
                        BaseAnnualPricePerUser = 378m, // 30% discount
                        DiscountPercentage = 30,
                        IsActive = true
                    }
                };

                _context.UserPricingTiers.AddRange(userTiers);
                await _context.SaveChangesAsync();

                // Now seed add-on pricing tiers for existing add-ons
                var addOns = await _context.PlanAddOns.Where(a => a.IsActive).ToListAsync();
                var addOnPricingTiers = new List<AddOnPricingTier>();

                foreach (var addOn in addOns)
                {
                    // Base pricing for 1-10 assignments
                    addOnPricingTiers.Add(new AddOnPricingTier
                    {
                        AddOnId = addOn.AddOnId,
                        MinQuantity = 1,
                        MaxQuantity = 10,
                        MonthlyPricePerAssignment = addOn.MonthlyPrice,
                        AnnualPricePerAssignment = addOn.AnnualPrice,
                        DiscountPercentage = 0,
                        IsActive = true
                    });

                    // 10% discount for 11-25 assignments
                    addOnPricingTiers.Add(new AddOnPricingTier
                    {
                        AddOnId = addOn.AddOnId,
                        MinQuantity = 11,
                        MaxQuantity = 25,
                        MonthlyPricePerAssignment = addOn.MonthlyPrice * 0.9m,
                        AnnualPricePerAssignment = addOn.AnnualPrice * 0.9m,
                        DiscountPercentage = 10,
                        IsActive = true
                    });

                    // 20% discount for 26+ assignments
                    addOnPricingTiers.Add(new AddOnPricingTier
                    {
                        AddOnId = addOn.AddOnId,
                        MinQuantity = 26,
                        MaxQuantity = null,
                        MonthlyPricePerAssignment = addOn.MonthlyPrice * 0.8m,
                        AnnualPricePerAssignment = addOn.AnnualPrice * 0.8m,
                        DiscountPercentage = 20,
                        IsActive = true
                    });
                }

                _context.AddOnPricingTiers.AddRange(addOnPricingTiers);

                // Seed add-on permissions
                var monteCarloAddOn = addOns.FirstOrDefault(a => a.FeatureKey == "MonteCarloSimulation");
                var pricingGenAddOn = addOns.FirstOrDefault(a => a.FeatureKey == "PricingGeneration");
                var contractGenAddOn = addOns.FirstOrDefault(a => a.FeatureKey == "ContractGeneration");

                var permissions = new List<AddOnPermission>();

                if (monteCarloAddOn != null)
                {
                    permissions.Add(new AddOnPermission
                    {
                        AddOnId = monteCarloAddOn.AddOnId,
                        PermissionKey = "MonteCarloSimulation",
                        PermissionName = "Monte Carlo Simulation",
                        Description = "Run portfolio-wide Monte Carlo simulations with correlation matrices"
                    });
                    permissions.Add(new AddOnPermission
                    {
                        AddOnId = monteCarloAddOn.AddOnId,
                        PermissionKey = "AdvancedRiskAnalytics",
                        PermissionName = "Advanced Risk Analytics",
                        Description = "Access to advanced risk metrics and VaR calculations"
                    });
                }

                if (pricingGenAddOn != null)
                {
                    permissions.Add(new AddOnPermission
                    {
                        AddOnId = pricingGenAddOn.AddOnId,
                        PermissionKey = "PricingGeneration",
                        PermissionName = "Automated Pricing Generation",
                        Description = "Generate loan pricing with tariff calculator"
                    });
                    permissions.Add(new AddOnPermission
                    {
                        AddOnId = pricingGenAddOn.AddOnId,
                        PermissionKey = "BulkPricing",
                        PermissionName = "Bulk Pricing",
                        Description = "Process multiple pricing requests at once"
                    });
                }

                if (contractGenAddOn != null)
                {
                    permissions.Add(new AddOnPermission
                    {
                        AddOnId = contractGenAddOn.AddOnId,
                        PermissionKey = "ContractGeneration",
                        PermissionName = "Contract Generation",
                        Description = "Generate loan contracts from templates"
                    });
                    permissions.Add(new AddOnPermission
                    {
                        AddOnId = contractGenAddOn.AddOnId,
                        PermissionKey = "TemplateManagement",
                        PermissionName = "Template Management",
                        Description = "Create and manage contract templates"
                    });
                }

                _context.AddOnPermissions.AddRange(permissions);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully seeded pricing tiers and add-on permissions");

                return Ok(new
                {
                    message = "Pricing tiers seeded successfully",
                    userTiers = userTiers.Count,
                    addOnTiers = addOnPricingTiers.Count,
                    permissions = permissions.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding pricing tiers");
                return StatusCode(500, new { error = "Error seeding pricing tiers", details = ex.Message });
            }
        }

        #endregion
    }

    public class PricingCalculationRequest
    {
        public int UserCount { get; set; }
        public List<AddOnAssignment> AddOnAssignments { get; set; } = new();
    }

    public class AddOnAssignment
    {
        public int AddOnId { get; set; }
        public int Quantity { get; set; }
    }
}
