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
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;
        private readonly ITenantService _tenantService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(
            LoanDbContext dbContext,
            ITenantService tenantService,
            ISubscriptionService subscriptionService,
            ILogger<SubscriptionController> logger)
        {
            _dbContext = dbContext;
            _tenantService = tenantService;
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        /// <summary>
        /// Get current tenant's subscription
        /// </summary>
        [HttpGet("current")]
        public async Task<ActionResult<TenantSubscription>> GetCurrentSubscription()
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return NotFound(new { message = "No tenant context" });
            }

            var subscription = await _dbContext.TenantSubscriptions
                .Include(s => s.PaymentPlan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value);

            if (subscription == null)
            {
                return NotFound(new { message = "No subscription found" });
            }

            return Ok(subscription);
        }

        /// <summary>
        /// Get current tenant's usage summary
        /// </summary>
        [HttpGet("usage")]
        public async Task<ActionResult<TenantUsageSummary>> GetUsageSummary()
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return NotFound(new { message = "No tenant context" });
            }

            var summary = await _subscriptionService.GetUsageSummary(tenantId.Value);
            return Ok(summary);
        }

        /// <summary>
        /// Get usage history for current tenant
        /// </summary>
        [HttpGet("usage/history")]
        [Authorize(Roles = TenantRoles.TenantAdmin)]
        public async Task<ActionResult<List<UsageTracking>>> GetUsageHistory([FromQuery] int? months = 12)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return NotFound(new { message = "No tenant context" });
            }

            var cutoffDate = DateTime.UtcNow.AddMonths(-months ?? -12);

            var history = await _dbContext.UsageTrackings
                .Where(ut => ut.TenantId == tenantId.Value && ut.RecordDate >= cutoffDate)
                .OrderByDescending(ut => ut.RecordDate)
                .ToListAsync();

            return Ok(history);
        }

        /// <summary>
        /// Create subscription for tenant (System Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "SystemAdminOnly")]
        public async Task<ActionResult<TenantSubscription>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            // Check if tenant already has subscription
            var existing = await _dbContext.TenantSubscriptions
                .FirstOrDefaultAsync(s => s.TenantId == request.TenantId);

            if (existing != null)
            {
                return BadRequest(new { message = "Tenant already has a subscription" });
            }

            // Verify payment plan exists
            var plan = await _dbContext.PaymentPlans.FindAsync(request.PaymentPlanId);
            if (plan == null)
            {
                return NotFound(new { message = "Payment plan not found" });
            }

            var subscription = new TenantSubscription
            {
                TenantId = request.TenantId,
                PaymentPlanId = request.PaymentPlanId,
                Status = request.Status ?? SubscriptionStatus.Trial,
                BillingPeriod = request.BillingPeriod ?? "Monthly",
                StartDate = request.StartDate ?? DateTime.UtcNow,
                TrialEndDate = request.TrialEndDate ?? DateTime.UtcNow.AddDays(14),
                NextBillingDate = request.NextBillingDate,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.TenantSubscriptions.Add(subscription);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Subscription created for tenant {TenantId} with plan {PlanId}", 
                request.TenantId, request.PaymentPlanId);

            return CreatedAtAction(nameof(GetCurrentSubscription), subscription);
        }

        /// <summary>
        /// Update subscription (System Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "SystemAdminOnly")]
        public async Task<ActionResult<TenantSubscription>> UpdateSubscription(int id, [FromBody] UpdateSubscriptionRequest request)
        {
            var subscription = await _dbContext.TenantSubscriptions.FindAsync(id);
            if (subscription == null)
            {
                return NotFound();
            }

            if (request.PaymentPlanId.HasValue)
            {
                subscription.PaymentPlanId = request.PaymentPlanId.Value;
            }

            if (request.Status != null)
            {
                subscription.Status = request.Status;
                if (request.Status == SubscriptionStatus.Cancelled)
                {
                    subscription.CancelledAt = DateTime.UtcNow;
                }
            }

            if (request.BillingPeriod != null)
            {
                subscription.BillingPeriod = request.BillingPeriod;
            }

            if (request.EndDate.HasValue)
            {
                subscription.EndDate = request.EndDate;
            }

            if (request.NextBillingDate.HasValue)
            {
                subscription.NextBillingDate = request.NextBillingDate;
            }

            // Custom overrides
            subscription.CustomMaxUsers = request.CustomMaxUsers;
            subscription.CustomMaxFunds = request.CustomMaxFunds;
            subscription.CustomMaxDebtors = request.CustomMaxDebtors;
            subscription.CustomMaxLoans = request.CustomMaxLoans;
            subscription.CustomStorageLimitMB = request.CustomStorageLimitMB;
            subscription.CustomAllowMonteCarloSimulation = request.CustomAllowMonteCarloSimulation;
            subscription.CustomAllowPortfolioAnalysis = request.CustomAllowPortfolioAnalysis;
            subscription.CustomAllowReporting = request.CustomAllowReporting;
            subscription.CustomAllowApiAccess = request.CustomAllowApiAccess;
            subscription.CustomAllowAdvancedAnalytics = request.CustomAllowAdvancedAnalytics;

            if (request.Notes != null)
            {
                subscription.Notes = request.Notes;
            }

            subscription.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Subscription {SubscriptionId} updated", id);

            return Ok(subscription);
        }

        /// <summary>
        /// Cancel subscription (System Admin or Tenant Admin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "TenantAdminOnly")]
        public async Task<ActionResult> CancelSubscription(int id)
        {
            var subscription = await _dbContext.TenantSubscriptions.FindAsync(id);
            if (subscription == null)
            {
                return NotFound();
            }

            // If user is tenant admin, verify they own this subscription
            if (!User.HasClaim("IsSystemAdmin", "true"))
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                if (subscription.TenantId != tenantId)
                {
                    return Forbid();
                }
            }

            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogWarning("Subscription {SubscriptionId} cancelled for tenant {TenantId}", 
                id, subscription.TenantId);

            return NoContent();
        }

        /// <summary>
        /// Get all subscriptions (System Admin only)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Policy = "SystemAdminOnly")]
        public async Task<ActionResult<List<TenantSubscription>>> GetAllSubscriptions([FromQuery] string? status = null)
        {
            var query = _dbContext.TenantSubscriptions
                .Include(s => s.Tenant)
                .Include(s => s.PaymentPlan)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            var subscriptions = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(subscriptions);
        }

        /// <summary>
        /// Check if specific feature is enabled for current tenant
        /// </summary>
        [HttpGet("features/{feature}")]
        public async Task<ActionResult<FeatureCheckResponse>> CheckFeature(string feature)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return NotFound(new { message = "No tenant context" });
            }

            var isEnabled = await _subscriptionService.IsFeatureEnabled(tenantId.Value, feature);

            return Ok(new FeatureCheckResponse
            {
                Feature = feature,
                IsEnabled = isEnabled,
                TenantId = tenantId.Value
            });
        }
    }

    public class CreateSubscriptionRequest
    {
        public int TenantId { get; set; }
        public int PaymentPlanId { get; set; }
        public string? Status { get; set; }
        public string? BillingPeriod { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateSubscriptionRequest
    {
        public int? PaymentPlanId { get; set; }
        public string? Status { get; set; }
        public string? BillingPeriod { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public int? CustomMaxUsers { get; set; }
        public int? CustomMaxFunds { get; set; }
        public int? CustomMaxDebtors { get; set; }
        public int? CustomMaxLoans { get; set; }
        public int? CustomStorageLimitMB { get; set; }
        public bool? CustomAllowMonteCarloSimulation { get; set; }
        public bool? CustomAllowPortfolioAnalysis { get; set; }
        public bool? CustomAllowReporting { get; set; }
        public bool? CustomAllowApiAccess { get; set; }
        public bool? CustomAllowAdvancedAnalytics { get; set; }
        public string? Notes { get; set; }
    }

    public class FeatureCheckResponse
    {
        public string Feature { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public int TenantId { get; set; }
    }
}
