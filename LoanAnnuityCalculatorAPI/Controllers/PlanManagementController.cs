using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "SystemAdminOnly")]
    public class PlanManagementController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;
        private readonly ILogger<PlanManagementController> _logger;

        public PlanManagementController(LoanDbContext dbContext, ILogger<PlanManagementController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        #region Add-Ons Management

        /// <summary>
        /// Get all available add-ons
        /// </summary>
        [HttpGet("addons")]
        public async Task<ActionResult<List<PlanAddOn>>> GetAddOns()
        {
            var addOns = await _dbContext.PlanAddOns
                .Where(a => a.IsActive)
                .ToListAsync();
            return Ok(addOns);
        }

        /// <summary>
        /// Create a new add-on
        /// </summary>
        [HttpPost("addons")]
        public async Task<ActionResult<PlanAddOn>> CreateAddOn([FromBody] CreateAddOnRequest request)
        {
            var addOn = new PlanAddOn
            {
                Name = request.Name,
                Description = request.Description,
                FeatureKey = request.FeatureKey,
                MonthlyPrice = request.MonthlyPrice,
                AnnualPrice = request.AnnualPrice,
                IsActive = true
            };

            _dbContext.PlanAddOns.Add(addOn);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Add-on created: {Name}", addOn.Name);
            return CreatedAtAction(nameof(GetAddOns), new { id = addOn.AddOnId }, addOn);
        }

        /// <summary>
        /// Seed default add-ons
        /// </summary>
        [HttpPost("addons/seed")]
        public async Task<ActionResult> SeedDefaultAddOns()
        {
            if (await _dbContext.PlanAddOns.AnyAsync())
            {
                return BadRequest(new { message = "Add-ons already exist" });
            }

            var addOns = new[]
            {
                new PlanAddOn
                {
                    Name = "Monte Carlo Simulaties",
                    Description = "Geavanceerde risico-analyses met Monte Carlo simulaties",
                    FeatureKey = "MonteCarloSimulation",
                    MonthlyPrice = 49,
                    AnnualPrice = 490,
                    IsActive = true
                },
                new PlanAddOn
                {
                    Name = "Tarief Generatie",
                    Description = "Automatische tariefberekeningen en -voorstellen",
                    FeatureKey = "TariffGeneration",
                    MonthlyPrice = 29,
                    AnnualPrice = 290,
                    IsActive = true
                },
                new PlanAddOn
                {
                    Name = "Contract Generatie",
                    Description = "Geautomatiseerde contractgeneratie met templates",
                    FeatureKey = "ContractGeneration",
                    MonthlyPrice = 39,
                    AnnualPrice = 390,
                    IsActive = true
                },
                new PlanAddOn
                {
                    Name = "Geavanceerde Analytics",
                    Description = "Uitgebreide rapportages en data-analyses",
                    FeatureKey = "AdvancedAnalytics",
                    MonthlyPrice = 59,
                    AnnualPrice = 590,
                    IsActive = true
                },
                new PlanAddOn
                {
                    Name = "API Toegang",
                    Description = "Volledige API toegang voor integraties",
                    FeatureKey = "ApiAccess",
                    MonthlyPrice = 99,
                    AnnualPrice = 990,
                    IsActive = true
                }
            };

            _dbContext.PlanAddOns.AddRange(addOns);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Default add-ons seeded");
            return Ok(new { message = $"{addOns.Length} add-ons created" });
        }

        #endregion

        #region Tenant Custom Pricing

        /// <summary>
        /// Get custom pricing for a tenant
        /// </summary>
        [HttpGet("tenant/{tenantId}/pricing")]
        public async Task<ActionResult<TenantCustomPricing>> GetTenantPricing(int tenantId)
        {
            var pricing = await _dbContext.TenantCustomPricings
                .FirstOrDefaultAsync(p => p.TenantId == tenantId);

            if (pricing == null)
            {
                return NotFound(new { message = "No custom pricing found for this tenant" });
            }

            return Ok(pricing);
        }

        /// <summary>
        /// Create or update custom pricing for a tenant
        /// </summary>
        [HttpPut("tenant/{tenantId}/pricing")]
        public async Task<ActionResult<TenantCustomPricing>> UpdateTenantPricing(
            int tenantId, 
            [FromBody] UpdatePricingRequest request)
        {
            var tenant = await _dbContext.Tenants.FindAsync(tenantId);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            var pricing = await _dbContext.TenantCustomPricings
                .FirstOrDefaultAsync(p => p.TenantId == tenantId);

            if (pricing == null)
            {
                pricing = new TenantCustomPricing { TenantId = tenantId };
                _dbContext.TenantCustomPricings.Add(pricing);
            }

            // Update limits
            pricing.CustomMaxUsers = request.MaxUsers;
            pricing.CustomMaxFunds = request.MaxFunds;
            pricing.CustomMaxDebtors = request.MaxDebtors;
            pricing.CustomMaxLoans = request.MaxLoans;
            pricing.CustomStorageLimitMB = request.StorageLimitMB;

            // Update pricing
            pricing.PricePerUser = request.PricePerUser;
            pricing.BaseMonthlyPrice = request.BaseMonthlyPrice;
            pricing.BaseAnnualPrice = request.BaseAnnualPrice;
            pricing.MultiYearDiscount = request.MultiYearDiscount;
            pricing.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Custom pricing updated for tenant {TenantId}", tenantId);
            return Ok(pricing);
        }

        #endregion

        #region Tenant Add-Ons

        /// <summary>
        /// Get add-ons for a tenant
        /// </summary>
        [HttpGet("tenant/{tenantId}/addons")]
        public async Task<ActionResult<List<TenantAddOnDto>>> GetTenantAddOns(int tenantId)
        {
            var tenantAddOns = await _dbContext.TenantAddOns
                .Include(ta => ta.AddOn)
                .Where(ta => ta.TenantId == tenantId && ta.IsEnabled)
                .Select(ta => new TenantAddOnDto
                {
                    TenantAddOnId = ta.TenantAddOnId,
                    AddOnId = ta.AddOnId,
                    AddOnName = ta.AddOn.Name,
                    Description = ta.AddOn.Description,
                    FeatureKey = ta.AddOn.FeatureKey,
                    MonthlyPrice = ta.CustomMonthlyPrice ?? ta.AddOn.MonthlyPrice,
                    AnnualPrice = ta.CustomAnnualPrice ?? ta.AddOn.AnnualPrice,
                    IsCustomPrice = ta.CustomMonthlyPrice.HasValue,
                    EnabledAt = ta.EnabledAt
                })
                .ToListAsync();

            return Ok(tenantAddOns);
        }

        /// <summary>
        /// Enable an add-on for a tenant
        /// </summary>
        [HttpPost("tenant/{tenantId}/addons/{addOnId}")]
        public async Task<ActionResult> EnableTenantAddOn(
            int tenantId, 
            int addOnId,
            [FromBody] EnableAddOnRequest? request = null)
        {
            var tenant = await _dbContext.Tenants.FindAsync(tenantId);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            var addOn = await _dbContext.PlanAddOns.FindAsync(addOnId);
            if (addOn == null)
            {
                return NotFound(new { message = "Add-on not found" });
            }

            var existing = await _dbContext.TenantAddOns
                .FirstOrDefaultAsync(ta => ta.TenantId == tenantId && ta.AddOnId == addOnId);

            if (existing != null)
            {
                existing.IsEnabled = true;
                if (request?.CustomMonthlyPrice.HasValue == true)
                {
                    existing.CustomMonthlyPrice = request.CustomMonthlyPrice;
                    existing.CustomAnnualPrice = request.CustomAnnualPrice;
                }
            }
            else
            {
                var tenantAddOn = new TenantAddOn
                {
                    TenantId = tenantId,
                    AddOnId = addOnId,
                    IsEnabled = true,
                    CustomMonthlyPrice = request?.CustomMonthlyPrice,
                    CustomAnnualPrice = request?.CustomAnnualPrice
                };
                _dbContext.TenantAddOns.Add(tenantAddOn);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Add-on {AddOnId} enabled for tenant {TenantId}", addOnId, tenantId);
            return Ok(new { message = "Add-on enabled" });
        }

        /// <summary>
        /// Disable an add-on for a tenant
        /// </summary>
        [HttpDelete("tenant/{tenantId}/addons/{addOnId}")]
        public async Task<ActionResult> DisableTenantAddOn(int tenantId, int addOnId)
        {
            var tenantAddOn = await _dbContext.TenantAddOns
                .FirstOrDefaultAsync(ta => ta.TenantId == tenantId && ta.AddOnId == addOnId);

            if (tenantAddOn == null)
            {
                return NotFound(new { message = "Add-on not found for this tenant" });
            }

            tenantAddOn.IsEnabled = false;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Add-on {AddOnId} disabled for tenant {TenantId}", addOnId, tenantId);
            return Ok(new { message = "Add-on disabled" });
        }

        #endregion

        #region DTOs

        public class CreateAddOnRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string FeatureKey { get; set; } = string.Empty;
            public decimal MonthlyPrice { get; set; }
            public decimal AnnualPrice { get; set; }
        }

        public class UpdatePricingRequest
        {
            public int? MaxUsers { get; set; }
            public int? MaxFunds { get; set; }
            public int? MaxDebtors { get; set; }
            public int? MaxLoans { get; set; }
            public int? StorageLimitMB { get; set; }
            public decimal? PricePerUser { get; set; }
            public decimal? BaseMonthlyPrice { get; set; }
            public decimal? BaseAnnualPrice { get; set; }
            public decimal? MultiYearDiscount { get; set; }
        }

        public class EnableAddOnRequest
        {
            public decimal? CustomMonthlyPrice { get; set; }
            public decimal? CustomAnnualPrice { get; set; }
        }

        public class TenantAddOnDto
        {
            public int TenantAddOnId { get; set; }
            public int AddOnId { get; set; }
            public string AddOnName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string FeatureKey { get; set; } = string.Empty;
            public decimal MonthlyPrice { get; set; }
            public decimal AnnualPrice { get; set; }
            public bool IsCustomPrice { get; set; }
            public DateTime EnabledAt { get; set; }
        }

        #endregion
    }
}
