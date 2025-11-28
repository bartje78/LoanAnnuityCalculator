using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LoanAnnuityCalculatorAPI.Models.Loan; // Import the Loan namespace
using LoanAnnuityCalculatorAPI.Models.Debtor; // Import the Debtor namespace // Import the Debtor namespace
using LoanAnnuityCalculatorAPI.Models.Ratios;

namespace LoanAnnuityCalculatorAPI.Models.Debtor
{
   public class DebtorDetails
    {
        [Key]
        public int DebtorID { get; set; }

        /// <summary>
        /// Tenant this debtor belongs to - CRITICAL for data isolation
        /// </summary>
        [Required]
        public int TenantId { get; set; }

        /// <summary>
        /// Fund this debtor is associated with
        /// </summary>
        [Required]
        public int FundId { get; set; }
        
        // Company information
        public string DebtorName { get; set; } = string.Empty;
        
        // Main contact person details
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactFirstNames { get; set; } = string.Empty;
        public string ContactLastName { get; set; } = string.Empty;
        public string ContactCallingName { get; set; } = string.Empty;
        
        // Address details
        public string Address { get; set; } = string.Empty; // Legacy field, kept for backwards compatibility
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        
        // Contact information
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        
        // Status
        public bool IsProspect { get; set; } = false;
        
        // Signatory 1
        public string Signatory1Name { get; set; } = string.Empty;
        public string Signatory1Function { get; set; } = string.Empty;
        
        // Signatory 2
        public string Signatory2Name { get; set; } = string.Empty;
        public string Signatory2Function { get; set; } = string.Empty;
        
        // Signatory 3
        public string Signatory3Name { get; set; } = string.Empty;
        public string Signatory3Function { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<LoanAnnuityCalculatorAPI.Models.Loan.Loan> Loans { get; set; } = new List<LoanAnnuityCalculatorAPI.Models.Loan.Loan>();
        public ICollection<DebtorBalanceSheet> BalanceSheets { get; set; } = new List<DebtorBalanceSheet>();
        public ICollection<DebtorPL> ProfitAndLossStatements { get; set; } = new List<DebtorPL>();
        public ICollection<DebtorSignatory> Signatories { get; set; } = new List<DebtorSignatory>();
    
    }

    public class DebtorPL
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("DebtorDetails")]
        public int DebtorID { get; set; }

        public int BookYear { get; set; }
        
        /// <summary>
        /// Indicates whether this is a projected/pro forma statement (true) or actual/realized (false)
        /// </summary>
        public bool IsProForma { get; set; } = false;
        
        // Updated field names to match frontend
        public decimal Revenue { get; set; }
        public decimal OperatingExpenses { get; set; }
        public decimal CostOfGoodsSold { get; set; }
        public decimal InterestExpense { get; set; }
        public decimal TaxExpense { get; set; }
        public decimal NetIncome { get; set; }
        
        // Legacy fields (optional for backwards compatibility)
        public decimal? GrossRevenue { get; set; }
        public decimal? EBITDA { get; set; }
        public decimal? InterestCost { get; set; }
        public decimal? NetProfit { get; set; }
        
        /// <summary>
        /// JSON field storing revenue breakdown by sector (for Monte Carlo correlations)
        /// Format: { "Manufacturing": 50000, "Retail": 30000, "Technology": 20000 }
        /// </summary>
        public string? RevenueSectorBreakdown { get; set; }

        public required DebtorDetails DebtorDetails { get; set; }
        
        // Navigation property for revenue details
        public ICollection<RevenueDetail> RevenueDetails { get; set; } = new List<RevenueDetail>();
    }

    public class DebtorBalanceSheet
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("DebtorDetails")]
        public int DebtorID { get; set; }

        public int BookYear { get; set; }
        
        /// <summary>
        /// Indicates whether this is a projected/pro forma statement (true) or actual/realized (false)
        /// </summary>
        public bool IsProForma { get; set; } = false;
        
        // Summary totals (calculated from line items)
        public decimal CurrentAssets { get; set; }
        public decimal LongTermAssets { get; set; }
        public decimal CurrentLiabilities { get; set; }
        public decimal LongTermLiabilities { get; set; }
        public decimal OwnersEquity { get; set; }

        // First Lien Loan fields (for subordinated loans) - DEPRECATED, will be moved to line items
        public decimal? FirstLienLoanAmount { get; set; }
        public decimal? FirstLienInterestRate { get; set; }
        public int? FirstLienTenorMonths { get; set; }
        public string? FirstLienRedemptionSchedule { get; set; }

        // Navigation properties
        public required DebtorDetails DebtorDetails { get; set; }
        public ICollection<BalanceSheetLineItem> LineItems { get; set; } = new List<BalanceSheetLineItem>();
    }

    // DTO for updating balance sheet without navigation properties
    public class UpdateBalanceSheetDto
    {
        public int Id { get; set; }
        public int DebtorID { get; set; }
        public int BookYear { get; set; }
        public bool IsProForma { get; set; }
        public decimal CurrentAssets { get; set; }
        public decimal LongTermAssets { get; set; }
        public decimal CurrentLiabilities { get; set; }
        public decimal LongTermLiabilities { get; set; }
        public decimal OwnersEquity { get; set; }
        
        // First Lien Loan fields
        public decimal? FirstLienLoanAmount { get; set; }
        public decimal? FirstLienInterestRate { get; set; }
        public int? FirstLienTenorMonths { get; set; }
        public string? FirstLienRedemptionSchedule { get; set; }
    }

    // DTO for updating profit & loss without navigation properties
    public class UpdateProfitLossDto
    {
        public int Id { get; set; }
        public int DebtorID { get; set; }
        public int BookYear { get; set; }
        public bool IsProForma { get; set; }
        public decimal Revenue { get; set; }
        public decimal OperatingExpenses { get; set; }
        public decimal CostOfGoodsSold { get; set; }
        public decimal EBITDA { get; set; }
        public decimal InterestExpense { get; set; }
        public decimal TaxExpense { get; set; }
        public decimal NetIncome { get; set; }
        public string? RevenueSectorBreakdown { get; set; }
    }
}