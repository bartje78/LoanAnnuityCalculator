using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Tracks usage statistics per tenant for billing and limit enforcement
    /// </summary>
    public class UsageTracking
    {
        [Key]
        public int UsageId { get; set; }

        [Required]
        public int TenantId { get; set; }

        public DateTime RecordDate { get; set; } = DateTime.UtcNow;

        // === USER METRICS ===
        public int ActiveUserCount { get; set; }
        public int TotalUserCount { get; set; }

        // === ENTITY COUNTS ===
        public int FundCount { get; set; }
        public int DebtorCount { get; set; }
        public int LoanCount { get; set; }
        public int CollateralCount { get; set; }

        // === STORAGE METRICS ===
        /// <summary>
        /// Storage used in MB
        /// </summary>
        public decimal StorageUsedMB { get; set; }

        // === ACTIVITY METRICS ===
        public int MonteCarloSimulationsRun { get; set; }
        public int ReportsGenerated { get; set; }
        public int ApiCallsCount { get; set; }
        public int ExportsCount { get; set; }
        public int ImportsCount { get; set; }

        // === MONTHLY AGGREGATES ===
        public int Year { get; set; }
        public int Month { get; set; }

        [ForeignKey("TenantId")]
        public virtual Tenant? Tenant { get; set; }
    }

    /// <summary>
    /// Real-time usage snapshot (cached for performance)
    /// </summary>
    public class TenantUsageSummary
    {
        public int TenantId { get; set; }
        public int CurrentUsers { get; set; }
        public int CurrentFunds { get; set; }
        public int CurrentDebtors { get; set; }
        public int CurrentLoans { get; set; }
        public decimal StorageUsedMB { get; set; }
        public DateTime LastUpdated { get; set; }

        // Limit checks
        public int MaxUsers { get; set; }
        public int MaxFunds { get; set; }
        public int MaxDebtors { get; set; }
        public int MaxLoans { get; set; }
        public int StorageLimitMB { get; set; }

        // Percentage used
        public decimal UserUsagePercent => MaxUsers > 0 ? (CurrentUsers * 100m / MaxUsers) : 0;
        public decimal FundUsagePercent => MaxFunds > 0 ? (CurrentFunds * 100m / MaxFunds) : 0;
        public decimal DebtorUsagePercent => MaxDebtors > 0 ? (CurrentDebtors * 100m / MaxDebtors) : 0;
        public decimal LoanUsagePercent => MaxLoans > 0 ? (CurrentLoans * 100m / MaxLoans) : 0;
        public decimal StorageUsagePercent => StorageLimitMB > 0 ? (StorageUsedMB * 100m / StorageLimitMB) : 0;

        // Limit exceeded flags
        public bool IsUserLimitExceeded => CurrentUsers >= MaxUsers;
        public bool IsFundLimitExceeded => CurrentFunds >= MaxFunds;
        public bool IsDebtorLimitExceeded => CurrentDebtors >= MaxDebtors;
        public bool IsLoanLimitExceeded => CurrentLoans >= MaxLoans;
        public bool IsStorageLimitExceeded => StorageUsedMB >= StorageLimitMB;

        public bool IsAnyLimitExceeded => 
            IsUserLimitExceeded || IsFundLimitExceeded || IsDebtorLimitExceeded || 
            IsLoanLimitExceeded || IsStorageLimitExceeded;
    }
}
