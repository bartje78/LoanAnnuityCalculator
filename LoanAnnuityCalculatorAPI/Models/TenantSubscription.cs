using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Links tenant to a payment plan with subscription details
    /// </summary>
    public class TenantSubscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public int PaymentPlanId { get; set; }

        /// <summary>
        /// Subscription status
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = SubscriptionStatus.Trial; // Trial, Active, Suspended, Cancelled

        /// <summary>
        /// Billing period
        /// </summary>
        [MaxLength(20)]
        public string BillingPeriod { get; set; } = "Monthly"; // Monthly, Annual

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public DateTime? NextBillingDate { get; set; }

        /// <summary>
        /// Custom overrides for this tenant (if different from plan defaults)
        /// </summary>
        public int? CustomMaxUsers { get; set; }
        public int? CustomMaxFunds { get; set; }
        public int? CustomMaxDebtors { get; set; }
        public int? CustomMaxLoans { get; set; }
        public int? CustomStorageLimitMB { get; set; }

        /// <summary>
        /// Custom feature overrides
        /// </summary>
        public bool? CustomAllowMonteCarloSimulation { get; set; }
        public bool? CustomAllowPortfolioAnalysis { get; set; }
        public bool? CustomAllowReporting { get; set; }
        public bool? CustomAllowApiAccess { get; set; }
        public bool? CustomAllowAdvancedAnalytics { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Navigation properties
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;

        [ForeignKey("PaymentPlanId")]
        public virtual PaymentPlan PaymentPlan { get; set; } = null!;

        /// <summary>
        /// Get effective limit considering custom overrides
        /// </summary>
        public int GetEffectiveMaxUsers() => CustomMaxUsers ?? PaymentPlan?.MaxUsers ?? 5;
        public int GetEffectiveMaxFunds() => CustomMaxFunds ?? PaymentPlan?.MaxFunds ?? 3;
        public int GetEffectiveMaxDebtors() => CustomMaxDebtors ?? PaymentPlan?.MaxDebtors ?? 100;
        public int GetEffectiveMaxLoans() => CustomMaxLoans ?? PaymentPlan?.MaxLoans ?? 500;
        public int GetEffectiveStorageLimit() => CustomStorageLimitMB ?? PaymentPlan?.StorageLimitMB ?? 1000;

        /// <summary>
        /// Check if feature is enabled
        /// </summary>
        public bool IsFeatureEnabled(string feature)
        {
            return feature switch
            {
                "MonteCarloSimulation" => CustomAllowMonteCarloSimulation ?? PaymentPlan?.AllowMonteCarloSimulation ?? false,
                "PortfolioAnalysis" => CustomAllowPortfolioAnalysis ?? PaymentPlan?.AllowPortfolioAnalysis ?? false,
                "Reporting" => CustomAllowReporting ?? PaymentPlan?.AllowReporting ?? false,
                "ApiAccess" => CustomAllowApiAccess ?? PaymentPlan?.AllowApiAccess ?? false,
                "AdvancedAnalytics" => CustomAllowAdvancedAnalytics ?? PaymentPlan?.AllowAdvancedAnalytics ?? false,
                _ => false
            };
        }
    }

    /// <summary>
    /// Subscription status constants
    /// </summary>
    public static class SubscriptionStatus
    {
        public const string Trial = "Trial";
        public const string Active = "Active";
        public const string Suspended = "Suspended";
        public const string Cancelled = "Cancelled";
        public const string PastDue = "PastDue";
    }
}
