using System.ComponentModel.DataAnnotations;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Subscription/payment plan defining features and limits
    /// </summary>
    public class PaymentPlan
    {
        [Key]
        public int PaymentPlanId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "Starter", "Professional", "Enterprise"

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Monthly price in euros
        /// </summary>
        public decimal MonthlyPrice { get; set; }

        /// <summary>
        /// Annual price in euros (usually discounted)
        /// </summary>
        public decimal AnnualPrice { get; set; }

        // === USER LIMITS ===
        public int MaxUsers { get; set; } = 5;
        public int MaxFunds { get; set; } = 3;
        public int MaxDebtors { get; set; } = 100;
        public int MaxLoans { get; set; } = 500;

        // === STORAGE LIMITS ===
        /// <summary>
        /// Storage limit in MB
        /// </summary>
        public int StorageLimitMB { get; set; } = 1000;

        // === FEATURE FLAGS ===
        public bool AllowMonteCarloSimulation { get; set; } = true;
        public bool AllowPortfolioAnalysis { get; set; } = true;
        public bool AllowReporting { get; set; } = true;
        public bool AllowExport { get; set; } = true;
        public bool AllowImport { get; set; } = true;
        public bool AllowApiAccess { get; set; } = false;
        public bool AllowCustomBranding { get; set; } = false;
        public bool AllowAdvancedAnalytics { get; set; } = false;
        public bool AllowMultipleFunds { get; set; } = true;

        // === SUPPORT LEVEL ===
        [MaxLength(50)]
        public string SupportLevel { get; set; } = "Email"; // Email, Priority, 24/7

        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = true; // Can be selected by new tenants
        
        public int DisplayOrder { get; set; } = 0; // For ordering in UI

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<TenantSubscription> Subscriptions { get; set; } = new List<TenantSubscription>();
    }

    /// <summary>
    /// Predefined payment plan types
    /// </summary>
    public static class PaymentPlanTypes
    {
        public const string Free = "Free";
        public const string Starter = "Starter";
        public const string Professional = "Professional";
        public const string Enterprise = "Enterprise";
        public const string Custom = "Custom";
    }
}
