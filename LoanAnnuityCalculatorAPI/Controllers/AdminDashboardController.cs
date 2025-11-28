using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Services;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "SystemAdminOnly")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            LoanDbContext dbContext,
            ISubscriptionService subscriptionService,
            ILogger<AdminDashboardController> logger)
        {
            _dbContext = dbContext;
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        /// <summary>
        /// Get system overview statistics
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult<SystemOverview>> GetSystemOverview()
        {
            var totalTenants = await _dbContext.Tenants.CountAsync(t => t.IsActive);
            var totalUsers = await _dbContext.Users.CountAsync(u => u.IsActive);
            var totalFunds = await _dbContext.Funds.CountAsync(f => f.IsActive);
            var totalDebtors = await _dbContext.DebtorDetails.CountAsync();
            var totalLoans = await _dbContext.Loans.CountAsync();

            var activeSubscriptions = await _dbContext.TenantSubscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active);
            
            var trialSubscriptions = await _dbContext.TenantSubscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Trial);
            
            var monthlyRevenue = await _dbContext.TenantSubscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.BillingPeriod == "Monthly")
                .Include(s => s.PaymentPlan)
                .SumAsync(s => s.PaymentPlan.MonthlyPrice);

            var annualRevenue = await _dbContext.TenantSubscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.BillingPeriod == "Annual")
                .Include(s => s.PaymentPlan)
                .SumAsync(s => s.PaymentPlan.AnnualPrice / 12); // Convert to monthly

            var totalMonthlyRevenue = monthlyRevenue + annualRevenue;

            // Get subscription breakdown by plan
            var subscriptionsByPlan = await _dbContext.TenantSubscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .Include(s => s.PaymentPlan)
                .GroupBy(s => s.PaymentPlan.Name)
                .Select(g => new PlanStats
                {
                    PlanName = g.Key,
                    Count = g.Count(),
                    MonthlyRevenue = g.Sum(s => 
                        s.BillingPeriod == "Monthly" ? s.PaymentPlan.MonthlyPrice : s.PaymentPlan.AnnualPrice / 12)
                })
                .ToListAsync();

            return Ok(new SystemOverview
            {
                TotalTenants = totalTenants,
                TotalUsers = totalUsers,
                TotalFunds = totalFunds,
                TotalDebtors = totalDebtors,
                TotalLoans = totalLoans,
                ActiveSubscriptions = activeSubscriptions,
                TrialSubscriptions = trialSubscriptions,
                MonthlyRecurringRevenue = totalMonthlyRevenue,
                AnnualRecurringRevenue = totalMonthlyRevenue * 12,
                SubscriptionsByPlan = subscriptionsByPlan
            });
        }

        /// <summary>
        /// Get all tenants with their subscription status and usage
        /// </summary>
        [HttpGet("tenants")]
        public async Task<ActionResult<List<TenantDashboard>>> GetTenantsDashboard()
        {
            var tenants = await _dbContext.Tenants
                .Include(t => t.Subscription)
                    .ThenInclude(s => s!.PaymentPlan)
                .Where(t => t.IsActive)
                .ToListAsync();

            var dashboards = new List<TenantDashboard>();

            foreach (var tenant in tenants)
            {
                var usage = await _subscriptionService.GetUsageSummary(tenant.TenantId);
                
                dashboards.Add(new TenantDashboard
                {
                    TenantId = tenant.TenantId,
                    TenantName = tenant.Name,
                    CreatedAt = tenant.CreatedAt,
                    SubscriptionStatus = tenant.Subscription?.Status ?? "None",
                    PaymentPlan = tenant.Subscription?.PaymentPlan?.Name ?? "None",
                    PaymentPlanId = tenant.Subscription?.PaymentPlanId,
                    BillingPeriod = tenant.Subscription?.BillingPeriod ?? "N/A",
                    MonthlyValue = tenant.Subscription?.BillingPeriod == "Monthly" 
                        ? tenant.Subscription.PaymentPlan?.MonthlyPrice ?? 0
                        : tenant.Subscription?.PaymentPlan?.AnnualPrice / 12 ?? 0,
                    NextBillingDate = tenant.Subscription?.NextBillingDate,
                    UserCount = usage.CurrentUsers,
                    FundCount = usage.CurrentFunds,
                    DebtorCount = usage.CurrentDebtors,
                    LoanCount = usage.CurrentLoans,
                    StorageUsedMB = usage.StorageUsedMB,
                    IsLimitExceeded = usage.IsAnyLimitExceeded
                });
            }

            return Ok(dashboards.OrderByDescending(d => d.MonthlyValue).ToList());
        }

        /// <summary>
        /// Get tenants approaching or exceeding limits
        /// </summary>
        [HttpGet("tenants/at-risk")]
        public async Task<ActionResult<List<TenantRiskAlert>>> GetTenantsAtRisk()
        {
            var tenants = await _dbContext.Tenants
                .Include(t => t.Subscription)
                .Where(t => t.IsActive)
                .ToListAsync();

            var alerts = new List<TenantRiskAlert>();

            foreach (var tenant in tenants)
            {
                var usage = await _subscriptionService.GetUsageSummary(tenant.TenantId);
                var issues = new List<string>();

                if (usage.UserUsagePercent >= 90) issues.Add($"Users: {usage.UserUsagePercent:F0}%");
                if (usage.FundUsagePercent >= 90) issues.Add($"Funds: {usage.FundUsagePercent:F0}%");
                if (usage.DebtorUsagePercent >= 90) issues.Add($"Debtors: {usage.DebtorUsagePercent:F0}%");
                if (usage.LoanUsagePercent >= 90) issues.Add($"Loans: {usage.LoanUsagePercent:F0}%");
                if (usage.StorageUsagePercent >= 90) issues.Add($"Storage: {usage.StorageUsagePercent:F0}%");

                if (tenant.Subscription?.Status == SubscriptionStatus.Trial && 
                    tenant.Subscription.TrialEndDate.HasValue &&
                    tenant.Subscription.TrialEndDate.Value <= DateTime.UtcNow.AddDays(7))
                {
                    var daysLeft = (tenant.Subscription.TrialEndDate.Value - DateTime.UtcNow).Days;
                    issues.Add($"Trial ends in {daysLeft} days");
                }

                if (issues.Any())
                {
                    alerts.Add(new TenantRiskAlert
                    {
                        TenantId = tenant.TenantId,
                        TenantName = tenant.Name,
                        Severity = usage.IsAnyLimitExceeded ? "High" : "Medium",
                        Issues = issues,
                        UsageSummary = usage
                    });
                }
            }

            return Ok(alerts.OrderBy(a => a.Severity == "High" ? 0 : 1).ToList());
        }

        /// <summary>
        /// Get revenue statistics over time
        /// </summary>
        [HttpGet("revenue/trends")]
        public async Task<ActionResult<List<RevenueTrend>>> GetRevenueTrends([FromQuery] int months = 12)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);
            
            var trends = await _dbContext.TenantSubscriptions
                .Where(s => s.CreatedAt >= startDate)
                .Include(s => s.PaymentPlan)
                .GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
                .Select(g => new RevenueTrend
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    NewSubscriptions = g.Count(),
                    TotalMRR = g.Sum(s => 
                        s.BillingPeriod == "Monthly" ? s.PaymentPlan.MonthlyPrice : s.PaymentPlan.AnnualPrice / 12)
                })
                .OrderBy(t => t.Year)
                .ThenBy(t => t.Month)
                .ToListAsync();

            return Ok(trends);
        }

        /// <summary>
        /// Seed default pricing tiers with realistic SaaS pricing
        /// </summary>
        [HttpPost("seed-pricing-tiers")]
        public async Task<ActionResult> SeedPricingTiers()
        {
            try
            {
                // Check if already seeded
                var existingUserTiers = await _dbContext.UserPricingTiers.AnyAsync();
                if (existingUserTiers)
                {
                    return BadRequest(new { error = "Pricing tiers already exist. Delete existing tiers first if you want to reseed." });
                }

                // Seed User Pricing Tiers
                var userTiers = new List<UserPricingTier>
                {
                    new UserPricingTier { MinUsers = 1, MaxUsers = 9, BaseMonthlyPricePerUser = 50m, BaseAnnualPricePerUser = 540m, DiscountPercentage = 0, IsActive = true },
                    new UserPricingTier { MinUsers = 10, MaxUsers = 19, BaseMonthlyPricePerUser = 45m, BaseAnnualPricePerUser = 486m, DiscountPercentage = 10, IsActive = true },
                    new UserPricingTier { MinUsers = 20, MaxUsers = 49, BaseMonthlyPricePerUser = 42.5m, BaseAnnualPricePerUser = 459m, DiscountPercentage = 15, IsActive = true },
                    new UserPricingTier { MinUsers = 50, MaxUsers = 99, BaseMonthlyPricePerUser = 40m, BaseAnnualPricePerUser = 432m, DiscountPercentage = 20, IsActive = true },
                    new UserPricingTier { MinUsers = 100, MaxUsers = null, BaseMonthlyPricePerUser = 35m, BaseAnnualPricePerUser = 378m, DiscountPercentage = 30, IsActive = true }
                };

                _dbContext.UserPricingTiers.AddRange(userTiers);
                await _dbContext.SaveChangesAsync();

                // Seed add-on pricing tiers
                var addOns = await _dbContext.PlanAddOns.Where(a => a.IsActive).ToListAsync();
                var addOnPricingTiers = new List<AddOnPricingTier>();

                foreach (var addOn in addOns)
                {
                    addOnPricingTiers.Add(new AddOnPricingTier { AddOnId = addOn.AddOnId, MinQuantity = 1, MaxQuantity = 10, MonthlyPricePerAssignment = addOn.MonthlyPrice, AnnualPricePerAssignment = addOn.AnnualPrice, DiscountPercentage = 0, IsActive = true });
                    addOnPricingTiers.Add(new AddOnPricingTier { AddOnId = addOn.AddOnId, MinQuantity = 11, MaxQuantity = 25, MonthlyPricePerAssignment = addOn.MonthlyPrice * 0.9m, AnnualPricePerAssignment = addOn.AnnualPrice * 0.9m, DiscountPercentage = 10, IsActive = true });
                    addOnPricingTiers.Add(new AddOnPricingTier { AddOnId = addOn.AddOnId, MinQuantity = 26, MaxQuantity = null, MonthlyPricePerAssignment = addOn.MonthlyPrice * 0.8m, AnnualPricePerAssignment = addOn.AnnualPrice * 0.8m, DiscountPercentage = 20, IsActive = true });
                }

                _dbContext.AddOnPricingTiers.AddRange(addOnPricingTiers);

                // Seed add-on permissions
                var permissions = new List<AddOnPermission>();
                var monteCarloAddOn = addOns.FirstOrDefault(a => a.FeatureKey == "MonteCarloSimulation");
                var pricingGenAddOn = addOns.FirstOrDefault(a => a.FeatureKey == "PricingGeneration");
                var contractGenAddOn = addOns.FirstOrDefault(a => a.FeatureKey == "ContractGeneration");

                if (monteCarloAddOn != null)
                {
                    permissions.Add(new AddOnPermission { AddOnId = monteCarloAddOn.AddOnId, PermissionKey = "MonteCarloSimulation", PermissionName = "Monte Carlo Simulation", Description = "Run portfolio-wide Monte Carlo simulations" });
                    permissions.Add(new AddOnPermission { AddOnId = monteCarloAddOn.AddOnId, PermissionKey = "AdvancedRiskAnalytics", PermissionName = "Advanced Risk Analytics", Description = "Access to advanced risk metrics" });
                }

                if (pricingGenAddOn != null)
                {
                    permissions.Add(new AddOnPermission { AddOnId = pricingGenAddOn.AddOnId, PermissionKey = "PricingGeneration", PermissionName = "Automated Pricing Generation", Description = "Generate loan pricing" });
                    permissions.Add(new AddOnPermission { AddOnId = pricingGenAddOn.AddOnId, PermissionKey = "BulkPricing", PermissionName = "Bulk Pricing", Description = "Process multiple pricing requests" });
                }

                if (contractGenAddOn != null)
                {
                    permissions.Add(new AddOnPermission { AddOnId = contractGenAddOn.AddOnId, PermissionKey = "ContractGeneration", PermissionName = "Contract Generation", Description = "Generate loan contracts" });
                    permissions.Add(new AddOnPermission { AddOnId = contractGenAddOn.AddOnId, PermissionKey = "TemplateManagement", PermissionName = "Template Management", Description = "Manage contract templates" });
                }

                _dbContext.AddOnPermissions.AddRange(permissions);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully seeded pricing tiers and add-on permissions");

                return Ok(new { message = "Pricing tiers seeded successfully", userTiers = userTiers.Count, addOnTiers = addOnPricingTiers.Count, permissions = permissions.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding pricing tiers");
                return StatusCode(500, new { error = "Error seeding pricing tiers", details = ex.Message });
            }
        }

        [HttpGet("user-pricing-tiers")]
        public async Task<ActionResult<IEnumerable<UserPricingTier>>> GetUserPricingTiers()
        {
            try
            {
                var tiers = await _dbContext.UserPricingTiers
                    .OrderBy(t => t.MinUsers)
                    .ToListAsync();
                return Ok(tiers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user pricing tiers");
                return StatusCode(500, new { error = "Error fetching user pricing tiers", details = ex.Message });
            }
        }

        [HttpGet("addon-pricing-tiers")]
        public async Task<ActionResult<IEnumerable<AddOnPricingTier>>> GetAddOnPricingTiers()
        {
            try
            {
                var tiers = await _dbContext.AddOnPricingTiers
                    .Include(t => t.AddOn)
                    .OrderBy(t => t.AddOnId)
                    .ThenBy(t => t.MinQuantity)
                    .ToListAsync();
                return Ok(tiers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching add-on pricing tiers");
                return StatusCode(500, new { error = "Error fetching add-on pricing tiers", details = ex.Message });
            }
        }

        [HttpPut("user-pricing-tiers/{tierId}")]
        public async Task<ActionResult<UserPricingTier>> UpdateUserPricingTier(int tierId, [FromBody] UserPricingTier tier)
        {
            try
            {
                if (tierId != tier.TierId)
                {
                    return BadRequest(new { error = "Tier ID mismatch" });
                }

                var existingTier = await _dbContext.UserPricingTiers.FindAsync(tierId);
                if (existingTier == null)
                {
                    return NotFound(new { error = "User pricing tier not found" });
                }

                existingTier.MinUsers = tier.MinUsers;
                existingTier.MaxUsers = tier.MaxUsers;
                existingTier.BaseMonthlyPricePerUser = tier.BaseMonthlyPricePerUser;
                existingTier.BaseAnnualPricePerUser = tier.BaseAnnualPricePerUser;
                existingTier.DiscountPercentage = tier.DiscountPercentage;
                existingTier.IsActive = tier.IsActive;

                await _dbContext.SaveChangesAsync();
                return Ok(existingTier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user pricing tier");
                return StatusCode(500, new { error = "Error updating user pricing tier", details = ex.Message });
            }
        }

        [HttpPut("addon-pricing-tiers/{tierId}")]
        public async Task<ActionResult<AddOnPricingTier>> UpdateAddOnPricingTier(int tierId, [FromBody] AddOnPricingTier tier)
        {
            try
            {
                if (tierId != tier.TierId)
                {
                    return BadRequest(new { error = "Tier ID mismatch" });
                }

                var existingTier = await _dbContext.AddOnPricingTiers.FindAsync(tierId);
                if (existingTier == null)
                {
                    return NotFound(new { error = "Add-on pricing tier not found" });
                }

                existingTier.MinQuantity = tier.MinQuantity;
                existingTier.MaxQuantity = tier.MaxQuantity;
                existingTier.MonthlyPricePerAssignment = tier.MonthlyPricePerAssignment;
                existingTier.AnnualPricePerAssignment = tier.AnnualPricePerAssignment;
                existingTier.DiscountPercentage = tier.DiscountPercentage;

                await _dbContext.SaveChangesAsync();
                return Ok(existingTier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating add-on pricing tier");
                return StatusCode(500, new { error = "Error updating add-on pricing tier", details = ex.Message });
            }
        }
    }

    public class SystemOverview
    {
        public int TotalTenants { get; set; }
        public int TotalUsers { get; set; }
        public int TotalFunds { get; set; }
        public int TotalDebtors { get; set; }
        public int TotalLoans { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TrialSubscriptions { get; set; }
        public decimal MonthlyRecurringRevenue { get; set; }
        public decimal AnnualRecurringRevenue { get; set; }
        public List<PlanStats> SubscriptionsByPlan { get; set; } = new();
    }

    public class PlanStats
    {
        public string PlanName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }

    public class TenantDashboard
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string SubscriptionStatus { get; set; } = string.Empty;
        public string PaymentPlan { get; set; } = string.Empty;
        public int? PaymentPlanId { get; set; }
        public string BillingPeriod { get; set; } = string.Empty;
        public decimal MonthlyValue { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public int UserCount { get; set; }
        public int FundCount { get; set; }
        public int DebtorCount { get; set; }
        public int LoanCount { get; set; }
        public decimal StorageUsedMB { get; set; }
        public bool IsLimitExceeded { get; set; }
    }

    public class TenantRiskAlert
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public List<string> Issues { get; set; } = new();
        public TenantUsageSummary? UsageSummary { get; set; }
    }

    public class RevenueTrend
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int NewSubscriptions { get; set; }
        public decimal TotalMRR { get; set; }
    }
}
