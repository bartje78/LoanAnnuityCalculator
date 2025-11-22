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
        public LoanDbContext(DbContextOptions<LoanDbContext> options) : base(options) { }

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
        public DbSet<ModelSettings> ModelSettings { get; set; }
        public DbSet<PropertyTypeParameters> PropertyTypeParameters { get; set; }
        public DbSet<ContractTextBlock> ContractTextBlocks { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }

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
    }
}