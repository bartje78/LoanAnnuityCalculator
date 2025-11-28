namespace LoanAnnuityCalculatorAPI.Models
{
    public class PlanAddOn
    {
        public int AddOnId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FeatureKey { get; set; } = string.Empty; // e.g., "MonteCarloSimulation", "ContractGeneration"
        public decimal MonthlyPrice { get; set; }
        public decimal AnnualPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public virtual ICollection<TenantAddOn> TenantAddOns { get; set; } = new List<TenantAddOn>();
        public virtual ICollection<AddOnPricingTier> PricingTiers { get; set; } = new List<AddOnPricingTier>();
        public virtual ICollection<AddOnPermission> Permissions { get; set; } = new List<AddOnPermission>();
        public virtual ICollection<UserAddOn> UserAddOns { get; set; } = new List<UserAddOn>();
    }

    public class TenantAddOn
    {
        public int TenantAddOnId { get; set; }
        public int TenantId { get; set; }
        public int AddOnId { get; set; }
        public bool IsEnabled { get; set; } = true;
        public decimal? CustomMonthlyPrice { get; set; } // Override default price
        public decimal? CustomAnnualPrice { get; set; }
        public DateTime EnabledAt { get; set; } = DateTime.UtcNow;
        
        // Navigation
        public virtual Tenant Tenant { get; set; } = null!;
        public virtual PlanAddOn AddOn { get; set; } = null!;
    }

    public class TenantCustomPricing
    {
        public int CustomPricingId { get; set; }
        public int TenantId { get; set; }
        
        // Custom limits (override plan defaults)
        public int? CustomMaxUsers { get; set; }
        public int? CustomMaxFunds { get; set; }
        public int? CustomMaxDebtors { get; set; }
        public int? CustomMaxLoans { get; set; }
        public int? CustomStorageLimitMB { get; set; }
        
        // Pricing structure
        public decimal? PricePerUser { get; set; } // Per user per month
        public decimal? BaseMonthlyPrice { get; set; }
        public decimal? BaseAnnualPrice { get; set; }
        public decimal? MultiYearDiscount { get; set; } // Percentage discount for multi-year
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation
        public virtual Tenant Tenant { get; set; } = null!;
    }
}
