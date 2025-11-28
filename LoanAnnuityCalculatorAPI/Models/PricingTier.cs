namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// System-wide base pricing per user with tiered discounts
    /// Example: 1-9 users = €50/user, 10-19 = €45/user (10% off), 20-49 = €42.50 (15% off)
    /// </summary>
    public class UserPricingTier
    {
        public int TierId { get; set; }
        public int MinUsers { get; set; }
        public int? MaxUsers { get; set; } // null = no upper limit
        public decimal BaseMonthlyPricePerUser { get; set; }
        public decimal BaseAnnualPricePerUser { get; set; }
        public decimal DiscountPercentage { get; set; } // 0-100
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Add-on pricing based on quantity assigned
    /// Example: 1-10 assignments = €10/user, 11-20 = €9/user, etc.
    /// </summary>
    public class AddOnPricingTier
    {
        public int TierId { get; set; }
        public int AddOnId { get; set; }
        public int MinQuantity { get; set; }
        public int? MaxQuantity { get; set; } // null = no upper limit
        public decimal MonthlyPricePerAssignment { get; set; }
        public decimal AnnualPricePerAssignment { get; set; }
        public decimal DiscountPercentage { get; set; } // 0-100
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual PlanAddOn AddOn { get; set; } = null!;
    }

    /// <summary>
    /// Maps add-ons to specific permissions/features they enable
    /// </summary>
    public class AddOnPermission
    {
        public int AddOnPermissionId { get; set; }
        public int AddOnId { get; set; }
        public string PermissionKey { get; set; } = string.Empty; // e.g., "MonteCarloSimulation", "AdvancedReporting"
        public string PermissionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual PlanAddOn AddOn { get; set; } = null!;
    }

    /// <summary>
    /// Tracks which add-ons are assigned to which users
    /// Used to calculate add-on pricing based on quantity tiers
    /// </summary>
    public class UserAddOn
    {
        public int UserAddOnId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int AddOnId { get; set; }
        public int TenantId { get; set; } // For data isolation
        public bool IsActive { get; set; } = true;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
        public string? AssignedByUserId { get; set; } // Tenant admin who assigned it

        // Navigation
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual PlanAddOn AddOn { get; set; } = null!;
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual ApplicationUser? AssignedBy { get; set; }
    }

    /// <summary>
    /// Tenant's pricing summary - calculated from system tiers and usage
    /// </summary>
    public class TenantPricingSummary
    {
        public int SummaryId { get; set; }
        public int TenantId { get; set; }
        public int ActiveUserCount { get; set; }
        public decimal MonthlyUserCost { get; set; } // Based on tier
        public decimal AnnualUserCost { get; set; }
        public decimal MonthlyAddOnCost { get; set; }
        public decimal AnnualAddOnCost { get; set; }
        public decimal TotalMonthly { get; set; }
        public decimal TotalAnnual { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public bool TenantAgreed { get; set; } = false; // Tenant admin must agree to pricing
        public DateTime? AgreedAt { get; set; }
        public string? AgreedByUserId { get; set; }

        // Navigation
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual ApplicationUser? AgreedBy { get; set; }
    }
}
