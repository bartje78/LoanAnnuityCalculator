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
    public class PaymentPlanController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;
        private readonly ILogger<PaymentPlanController> _logger;

        public PaymentPlanController(LoanDbContext dbContext, ILogger<PaymentPlanController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Get all payment plans
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<PaymentPlan>>> GetPaymentPlans([FromQuery] bool includeInactive = false)
        {
            var query = _dbContext.PaymentPlans.AsQueryable();
            
            if (!includeInactive)
            {
                query = query.Where(pp => pp.IsActive);
            }

            var plans = await query
                .OrderBy(pp => pp.DisplayOrder)
                .Include(pp => pp.Subscriptions)
                .ToListAsync();

            return Ok(plans);
        }

        /// <summary>
        /// Get public payment plans (for tenant selection)
        /// </summary>
        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<ActionResult<List<PaymentPlan>>> GetPublicPaymentPlans()
        {
            var plans = await _dbContext.PaymentPlans
                .Where(pp => pp.IsActive && pp.IsPublic)
                .OrderBy(pp => pp.DisplayOrder)
                .ToListAsync();

            return Ok(plans);
        }

        /// <summary>
        /// Get specific payment plan
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentPlan>> GetPaymentPlan(int id)
        {
            var plan = await _dbContext.PaymentPlans
                .Include(pp => pp.Subscriptions)
                .FirstOrDefaultAsync(pp => pp.PaymentPlanId == id);

            if (plan == null)
            {
                return NotFound();
            }

            return Ok(plan);
        }

        /// <summary>
        /// Create new payment plan
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PaymentPlan>> CreatePaymentPlan([FromBody] CreatePaymentPlanRequest request)
        {
            var plan = new PaymentPlan
            {
                Name = request.Name,
                Description = request.Description,
                MonthlyPrice = request.MonthlyPrice,
                AnnualPrice = request.AnnualPrice,
                MaxUsers = request.MaxUsers,
                MaxFunds = request.MaxFunds,
                MaxDebtors = request.MaxDebtors,
                MaxLoans = request.MaxLoans,
                StorageLimitMB = request.StorageLimitMB,
                AllowMonteCarloSimulation = request.AllowMonteCarloSimulation,
                AllowPortfolioAnalysis = request.AllowPortfolioAnalysis,
                AllowReporting = request.AllowReporting,
                AllowExport = request.AllowExport,
                AllowImport = request.AllowImport,
                AllowApiAccess = request.AllowApiAccess,
                AllowCustomBranding = request.AllowCustomBranding,
                AllowAdvancedAnalytics = request.AllowAdvancedAnalytics,
                AllowMultipleFunds = request.AllowMultipleFunds,
                SupportLevel = request.SupportLevel,
                IsActive = request.IsActive,
                IsPublic = request.IsPublic,
                DisplayOrder = request.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.PaymentPlans.Add(plan);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment plan {PlanName} created with ID {PlanId}", plan.Name, plan.PaymentPlanId);

            return CreatedAtAction(nameof(GetPaymentPlan), new { id = plan.PaymentPlanId }, plan);
        }

        /// <summary>
        /// Update payment plan
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PaymentPlan>> UpdatePaymentPlan(int id, [FromBody] UpdatePaymentPlanRequest request)
        {
            var plan = await _dbContext.PaymentPlans.FindAsync(id);
            if (plan == null)
            {
                return NotFound();
            }

            plan.Name = request.Name;
            plan.Description = request.Description;
            plan.MonthlyPrice = request.MonthlyPrice;
            plan.AnnualPrice = request.AnnualPrice;
            plan.MaxUsers = request.MaxUsers;
            plan.MaxFunds = request.MaxFunds;
            plan.MaxDebtors = request.MaxDebtors;
            plan.MaxLoans = request.MaxLoans;
            plan.StorageLimitMB = request.StorageLimitMB;
            plan.AllowMonteCarloSimulation = request.AllowMonteCarloSimulation;
            plan.AllowPortfolioAnalysis = request.AllowPortfolioAnalysis;
            plan.AllowReporting = request.AllowReporting;
            plan.AllowExport = request.AllowExport;
            plan.AllowImport = request.AllowImport;
            plan.AllowApiAccess = request.AllowApiAccess;
            plan.AllowCustomBranding = request.AllowCustomBranding;
            plan.AllowAdvancedAnalytics = request.AllowAdvancedAnalytics;
            plan.AllowMultipleFunds = request.AllowMultipleFunds;
            plan.SupportLevel = request.SupportLevel;
            plan.IsActive = request.IsActive;
            plan.IsPublic = request.IsPublic;
            plan.DisplayOrder = request.DisplayOrder;
            plan.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment plan {PlanId} updated", id);

            return Ok(plan);
        }

        /// <summary>
        /// Delete payment plan (only if no active subscriptions)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePaymentPlan(int id)
        {
            var plan = await _dbContext.PaymentPlans
                .Include(pp => pp.Subscriptions)
                .FirstOrDefaultAsync(pp => pp.PaymentPlanId == id);

            if (plan == null)
            {
                return NotFound();
            }

            if (plan.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active))
            {
                return BadRequest(new { message = "Cannot delete plan with active subscriptions. Deactivate it instead." });
            }

            _dbContext.PaymentPlans.Remove(plan);
            await _dbContext.SaveChangesAsync();

            _logger.LogWarning("Payment plan {PlanId} deleted", id);

            return NoContent();
        }

        /// <summary>
        /// Seed default payment plans
        /// </summary>
        [HttpPost("seed")]
        public async Task<ActionResult> SeedDefaultPlans()
        {
            if (await _dbContext.PaymentPlans.AnyAsync())
            {
                return BadRequest(new { message = "Payment plans already exist" });
            }

            var plans = new[]
            {
                new PaymentPlan
                {
                    Name = "Free",
                    Description = "Perfect for trying out the platform",
                    MonthlyPrice = 0,
                    AnnualPrice = 0,
                    MaxUsers = 2,
                    MaxFunds = 1,
                    MaxDebtors = 25,
                    MaxLoans = 100,
                    StorageLimitMB = 500,
                    AllowMonteCarloSimulation = false,
                    AllowPortfolioAnalysis = true,
                    AllowReporting = true,
                    AllowExport = false,
                    AllowImport = true,
                    AllowApiAccess = false,
                    AllowCustomBranding = false,
                    AllowAdvancedAnalytics = false,
                    AllowMultipleFunds = false,
                    SupportLevel = "Email",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 1
                },
                new PaymentPlan
                {
                    Name = "Starter",
                    Description = "Great for small teams managing a single fund",
                    MonthlyPrice = 99,
                    AnnualPrice = 990,
                    MaxUsers = 5,
                    MaxFunds = 3,
                    MaxDebtors = 100,
                    MaxLoans = 500,
                    StorageLimitMB = 2000,
                    AllowMonteCarloSimulation = true,
                    AllowPortfolioAnalysis = true,
                    AllowReporting = true,
                    AllowExport = true,
                    AllowImport = true,
                    AllowApiAccess = false,
                    AllowCustomBranding = false,
                    AllowAdvancedAnalytics = false,
                    AllowMultipleFunds = true,
                    SupportLevel = "Email",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 2
                },
                new PaymentPlan
                {
                    Name = "Professional",
                    Description = "For growing asset managers with multiple funds",
                    MonthlyPrice = 299,
                    AnnualPrice = 2990,
                    MaxUsers = 15,
                    MaxFunds = 10,
                    MaxDebtors = 500,
                    MaxLoans = 2500,
                    StorageLimitMB = 10000,
                    AllowMonteCarloSimulation = true,
                    AllowPortfolioAnalysis = true,
                    AllowReporting = true,
                    AllowExport = true,
                    AllowImport = true,
                    AllowApiAccess = true,
                    AllowCustomBranding = false,
                    AllowAdvancedAnalytics = true,
                    AllowMultipleFunds = true,
                    SupportLevel = "Priority",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 3
                },
                new PaymentPlan
                {
                    Name = "Enterprise",
                    Description = "Unlimited scale for large organizations",
                    MonthlyPrice = 999,
                    AnnualPrice = 9990,
                    MaxUsers = 100,
                    MaxFunds = 50,
                    MaxDebtors = 10000,
                    MaxLoans = 50000,
                    StorageLimitMB = 100000,
                    AllowMonteCarloSimulation = true,
                    AllowPortfolioAnalysis = true,
                    AllowReporting = true,
                    AllowExport = true,
                    AllowImport = true,
                    AllowApiAccess = true,
                    AllowCustomBranding = true,
                    AllowAdvancedAnalytics = true,
                    AllowMultipleFunds = true,
                    SupportLevel = "24/7",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 4
                }
            };

            _dbContext.PaymentPlans.AddRange(plans);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Default payment plans seeded");

            return Ok(new { message = $"{plans.Length} payment plans created" });
        }
    }

    public class CreatePaymentPlanRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal AnnualPrice { get; set; }
        public int MaxUsers { get; set; } = 5;
        public int MaxFunds { get; set; } = 3;
        public int MaxDebtors { get; set; } = 100;
        public int MaxLoans { get; set; } = 500;
        public int StorageLimitMB { get; set; } = 1000;
        public bool AllowMonteCarloSimulation { get; set; } = true;
        public bool AllowPortfolioAnalysis { get; set; } = true;
        public bool AllowReporting { get; set; } = true;
        public bool AllowExport { get; set; } = true;
        public bool AllowImport { get; set; } = true;
        public bool AllowApiAccess { get; set; } = false;
        public bool AllowCustomBranding { get; set; } = false;
        public bool AllowAdvancedAnalytics { get; set; } = false;
        public bool AllowMultipleFunds { get; set; } = true;
        public string SupportLevel { get; set; } = "Email";
        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdatePaymentPlanRequest : CreatePaymentPlanRequest { }
}
