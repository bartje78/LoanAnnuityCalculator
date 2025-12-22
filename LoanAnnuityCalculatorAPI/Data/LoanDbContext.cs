using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Models.Loan;
using LoanAnnuityCalculatorAPI.Models.Debtor;
using LoanAnnuityCalculatorAPI.Models.Ratios;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Models.Payment;
using LoanAnnuityCalculatorAPI.Models.Settings;

namespace LoanAnnuityCalculatorAPI.Data
{
    public class LoanDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public LoanDbContext(DbContextOptions<LoanDbContext> options) : base(options) { }

        public LoanDbContext(DbContextOptions<LoanDbContext> options, IHttpContextAccessor httpContextAccessor) 
            : base(options) 
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Multi-tenancy entities
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Fund> Funds { get; set; }
        public DbSet<UserFundAccess> UserFundAccesses { get; set; }
        public DbSet<PaymentPlan> PaymentPlans { get; set; }
        public DbSet<TenantSubscription> TenantSubscriptions { get; set; }
        public DbSet<UsageTracking> UsageTrackings { get; set; }
        public DbSet<PlanAddOn> PlanAddOns { get; set; }
        public DbSet<TenantAddOn> TenantAddOns { get; set; }
        public DbSet<TenantCustomPricing> TenantCustomPricings { get; set; }
        
        // New tiered pricing entities
        public DbSet<UserPricingTier> UserPricingTiers { get; set; }
        public DbSet<AddOnPricingTier> AddOnPricingTiers { get; set; }
        public DbSet<AddOnPermission> AddOnPermissions { get; set; }
        public DbSet<UserAddOn> UserAddOns { get; set; }
        public DbSet<TenantPricingSummary> TenantPricingSummaries { get; set; }

        // Existing entities
        public DbSet<DebtorDetails> DebtorDetails { get; set; }
        public DbSet<DebtorSignatory> DebtorSignatories { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<Collateral> Collaterals { get; set; }
        public DbSet<LoanCollateral> LoanCollaterals { get; set; }
        public DbSet<LoanPayment> LoanPayments { get; set; }
        public DbSet<DebtorBalanceSheet> DebtorBalanceSheets { get; set; }
        public DbSet<BalanceSheetLineItem> BalanceSheetLineItems { get; set; }
        public DbSet<DebtorPL> DebtorPLs { get; set; }
        public DbSet<RevenueDetail> RevenueDetails { get; set; }
        public DbSet<CreditRatingThreshold> CreditRatingThresholds { get; set; }
        public DbSet<CollateralIndex> CollateralIndexes { get; set; }
        public DbSet<CompanySettings> CompanySettings { get; set; }
        public DbSet<InvoiceSettings> InvoiceSettings { get; set; }
        public DbSet<LoanStatus> LoanStatuses { get; set; }
        public DbSet<TariffSettings> TariffSettings { get; set; }
        public DbSet<LtvSpreadTier> LtvSpreadTiers { get; set; }
        public DbSet<CreditRatingSpread> CreditRatingSpreads { get; set; }
        public DbSet<ImpactDiscount> ImpactDiscounts { get; set; }
        public DbSet<ModelSettings> ModelSettings { get; set; }
        public DbSet<PropertyTypeParameters> PropertyTypeParameters { get; set; }
        public DbSet<SectorCorrelation> SectorCorrelations { get; set; }
        public DbSet<SectorCollateralCorrelation> SectorCollateralCorrelations { get; set; }
        public DbSet<SectorDefinition> SectorDefinitions { get; set; }
        public DbSet<StandardRevenueCategory> StandardRevenueCategories { get; set; }
        public DbSet<ContractTextBlock> ContractTextBlocks { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<BuildingDepotWithdrawal> BuildingDepotWithdrawals { get; set; }
        public DbSet<BuildingDepotWithdrawalLineItem> BuildingDepotWithdrawalLineItems { get; set; }
        public DbSet<ExactOnlineToken> ExactOnlineTokens { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define the relationship between Loan and DebtorDetails
            modelBuilder.Entity<Loan>()
                .HasOne(l => l.DebtorDetails)
                .WithMany(d => d.Loans)
                .HasForeignKey(l => l.DebtorID)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete loans when a debtor is deleted

            // Define the relationship between DebtorBalanceSheet and DebtorDetails
            modelBuilder.Entity<DebtorBalanceSheet>()
                .HasOne(bs => bs.DebtorDetails)
                .WithMany(d => d.BalanceSheets)
                .HasForeignKey(bs => bs.DebtorID)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete balance sheets when a debtor is deleted

            // Define the relationship between BalanceSheetLineItem and DebtorBalanceSheet
            modelBuilder.Entity<BalanceSheetLineItem>()
                .HasOne(li => li.DebtorBalanceSheet)
                .WithMany(bs => bs.LineItems)
                .HasForeignKey(li => li.BalanceSheetId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete line items when a balance sheet is deleted

            // Define optional relationship between BalanceSheetLineItem and Loan
            modelBuilder.Entity<BalanceSheetLineItem>()
                .HasOne(li => li.Loan)
                .WithMany()
                .HasForeignKey(li => li.LoanId)
                .OnDelete(DeleteBehavior.SetNull); // Set LoanId to null if loan is deleted

            // Define optional relationship between BalanceSheetLineItem and Collateral
            modelBuilder.Entity<BalanceSheetLineItem>()
                .HasOne(li => li.Collateral)
                .WithMany()
                .HasForeignKey(li => li.CollateralId)
                .OnDelete(DeleteBehavior.SetNull); // Set CollateralId to null if collateral is deleted

            // Define the relationship between DebtorPL and DebtorDetails
            modelBuilder.Entity<DebtorPL>()
                .HasOne(pl => pl.DebtorDetails)
                .WithMany(d => d.ProfitAndLossStatements)
                .HasForeignKey(pl => pl.DebtorID)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete P&L records when a debtor is deleted

            // Configure many-to-many relationship between Loans and Collaterals through LoanCollateral
            modelBuilder.Entity<LoanCollateral>()
                .HasOne(lc => lc.Loan)
                .WithMany(l => l.LoanCollaterals)
                .HasForeignKey(lc => lc.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LoanCollateral>()
                .HasOne(lc => lc.Collateral)
                .WithMany(c => c.LoanCollaterals)
                .HasForeignKey(lc => lc.CollateralId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add unique constraint for collateral identification fields
            // Land Registry Code - unique for plots of land
            modelBuilder.Entity<Collateral>()
                .HasIndex(c => c.LandRegistryCode)
                .IsUnique()
                .HasFilter("[LandRegistryCode] IS NOT NULL");

            // Postal Code + House Number combination - unique for properties
            modelBuilder.Entity<Collateral>()
                .HasIndex(c => new { c.PostalCode, c.HouseNumber })
                .IsUnique()
                .HasFilter("[PostalCode] IS NOT NULL AND [HouseNumber] IS NOT NULL");

            // Generic Asset Unique ID - for non-real estate assets
            modelBuilder.Entity<Collateral>()
                .HasIndex(c => c.AssetUniqueId)
                .IsUnique()
                .HasFilter("[AssetUniqueId] IS NOT NULL");

            // Define the relationship between RevenueDetail and DebtorPL
            modelBuilder.Entity<RevenueDetail>()
                .HasOne(rd => rd.DebtorPL)
                .WithMany(pl => pl.RevenueDetails)
                .HasForeignKey(rd => rd.PLId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete revenue details when a P&L is deleted

            // Configure relationship between LoanPayment and Loan
            modelBuilder.Entity<LoanPayment>()
                .HasOne(lp => lp.Loan)
                .WithMany() // Loan doesn't need to know about payments in this direction
                .HasForeignKey(lp => lp.LoanId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete payments when a loan is deleted

            // Configure decimal precision for payment amounts
            modelBuilder.Entity<LoanPayment>()
                .Property(lp => lp.InterestAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LoanPayment>()
                .Property(lp => lp.CapitalAmount)
                .HasPrecision(18, 2);

            // === MULTI-TENANCY CONFIGURATION ===

            // Tenant relationships
            modelBuilder.Entity<Tenant>()
                .HasMany(t => t.Users)
                .WithOne(u => u.Tenant)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tenant>()
                .HasMany(t => t.Funds)
                .WithOne(f => f.Tenant)
                .HasForeignKey(f => f.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Fund relationships
            modelBuilder.Entity<Fund>()
                .HasMany(f => f.UserAccesses)
                .WithOne(ufa => ufa.Fund)
                .HasForeignKey(ufa => ufa.FundId)
                .OnDelete(DeleteBehavior.Cascade);

            // User fund access
            modelBuilder.Entity<UserFundAccess>()
                .HasOne(ufa => ufa.User)
                .WithMany(u => u.FundAccesses)
                .HasForeignKey(ufa => ufa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment plan relationships
            modelBuilder.Entity<PaymentPlan>()
                .HasMany(pp => pp.Subscriptions)
                .WithOne(s => s.PaymentPlan)
                .HasForeignKey(s => s.PaymentPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tenant subscription (one-to-one)
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.Subscription)
                .WithOne(s => s.Tenant)
                .HasForeignKey<TenantSubscription>(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Usage tracking
            modelBuilder.Entity<UsageTracking>()
                .HasOne(ut => ut.Tenant)
                .WithMany(t => t.UsageHistory)
                .HasForeignKey(ut => ut.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Usage tracking index for efficient queries
            modelBuilder.Entity<UsageTracking>()
                .HasIndex(ut => new { ut.TenantId, ut.Year, ut.Month });

            // Add-on relationships
            modelBuilder.Entity<PlanAddOn>()
                .HasKey(a => a.AddOnId);

            modelBuilder.Entity<PlanAddOn>()
                .HasMany(a => a.TenantAddOns)
                .WithOne(ta => ta.AddOn)
                .HasForeignKey(ta => ta.AddOnId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TenantAddOn>()
                .HasKey(ta => ta.TenantAddOnId);

            modelBuilder.Entity<TenantAddOn>()
                .HasOne(ta => ta.Tenant)
                .WithMany()
                .HasForeignKey(ta => ta.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TenantAddOn>()
                .HasIndex(ta => new { ta.TenantId, ta.AddOnId })
                .IsUnique();

            // Custom pricing relationship
            modelBuilder.Entity<TenantCustomPricing>()
                .HasKey(cp => cp.CustomPricingId);

            modelBuilder.Entity<TenantCustomPricing>()
                .HasOne(cp => cp.Tenant)
                .WithOne()
                .HasForeignKey<TenantCustomPricing>(cp => cp.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TenantCustomPricing>()
                .HasIndex(cp => cp.TenantId)
                .IsUnique();

            // Tiered pricing relationships
            modelBuilder.Entity<UserPricingTier>()
                .HasKey(t => t.TierId);

            modelBuilder.Entity<AddOnPricingTier>()
                .HasKey(t => t.TierId);

            modelBuilder.Entity<AddOnPricingTier>()
                .HasOne(t => t.AddOn)
                .WithMany(a => a.PricingTiers)
                .HasForeignKey(t => t.AddOnId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AddOnPermission>()
                .HasKey(p => p.AddOnPermissionId);

            modelBuilder.Entity<AddOnPermission>()
                .HasOne(p => p.AddOn)
                .WithMany(a => a.Permissions)
                .HasForeignKey(p => p.AddOnId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAddOn>()
                .HasKey(ua => ua.UserAddOnId);

            modelBuilder.Entity<UserAddOn>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.AddOns)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAddOn>()
                .HasOne(ua => ua.AddOn)
                .WithMany(a => a.UserAddOns)
                .HasForeignKey(ua => ua.AddOnId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserAddOn>()
                .HasOne(ua => ua.Tenant)
                .WithMany()
                .HasForeignKey(ua => ua.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserAddOn>()
                .HasIndex(ua => new { ua.UserId, ua.AddOnId, ua.TenantId });

            modelBuilder.Entity<TenantPricingSummary>()
                .HasKey(s => s.SummaryId);

            modelBuilder.Entity<TenantPricingSummary>()
                .HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TenantPricingSummary>()
                .HasIndex(s => new { s.TenantId, s.CalculatedAt });

            // Add tenant indexes for performance
            modelBuilder.Entity<DebtorDetails>()
                .HasIndex(d => d.TenantId);
            
            modelBuilder.Entity<DebtorDetails>()
                .HasIndex(d => d.FundId);

            modelBuilder.Entity<Loan>()
                .HasIndex(l => l.TenantId);
            
            modelBuilder.Entity<Loan>()
                .HasIndex(l => l.FundId);

            modelBuilder.Entity<Collateral>()
                .HasIndex(c => c.TenantId);
            
            modelBuilder.Entity<Collateral>()
                .HasIndex(c => c.FundId);

            // === GLOBAL QUERY FILTERS FOR TENANT ISOLATION ===
            // This ensures users can NEVER see data from other tenants
            var tenantId = GetCurrentTenantId();
            if (tenantId.HasValue)
            {
                modelBuilder.Entity<DebtorDetails>()
                    .HasQueryFilter(d => d.TenantId == tenantId.Value);

                modelBuilder.Entity<Loan>()
                    .HasQueryFilter(l => l.TenantId == tenantId.Value);

                modelBuilder.Entity<Collateral>()
                    .HasQueryFilter(c => c.TenantId == tenantId.Value);

                modelBuilder.Entity<Fund>()
                    .HasQueryFilter(f => f.TenantId == tenantId.Value);
            }

            modelBuilder.Entity<LoanPayment>()
                .Property(lp => lp.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LoanPayment>()
                .Property(lp => lp.RemainingBalance)
                .HasPrecision(18, 2);

            // Configure unique constraint on UserPreference (UserId + PreferenceKey)
            modelBuilder.Entity<UserPreference>()
                .HasIndex(p => new { p.UserId, p.PreferenceKey })
                .IsUnique();
        }

        /// <summary>
        /// Get the current tenant ID from HTTP context
        /// This is set by the TenantMiddleware
        /// </summary>
        private int? GetCurrentTenantId()
        {
            if (_httpContextAccessor?.HttpContext == null)
                return null;

            var tenantIdClaim = _httpContextAccessor.HttpContext.User?.FindFirst("TenantId");
            if (tenantIdClaim != null && int.TryParse(tenantIdClaim.Value, out int tenantId))
            {
                return tenantId;
            }

            return null;
        }
    }
}