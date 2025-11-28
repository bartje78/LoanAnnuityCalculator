namespace LoanAnnuityCalculatorAPI.Models.DTOs
{
    public class DebtorFinancialWithLoansDto
    {
        public int DebtorId { get; set; }
        public string DebtorName { get; set; } = string.Empty;
        public List<LoanSummaryDto> Loans { get; set; } = new List<LoanSummaryDto>();
        public List<EnhancedBalanceSheetDto> BalanceSheets { get; set; } = new List<EnhancedBalanceSheetDto>();
        public List<EnhancedProfitLossDto> ProfitLossStatements { get; set; } = new List<EnhancedProfitLossDto>();
    }

    public class LoanSummaryDto
    {
        public int LoanId { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TenorMonths { get; set; }
        public int InterestOnlyMonths { get; set; }
        public string RedemptionSchedule { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalCollateralValue { get; set; }
        public string? PrimaryCollateralType { get; set; }  // e.g., "Industrial", "Residential", "Commercial"
        public List<LoanYearlyDetailsDto> YearlyDetails { get; set; } = new List<LoanYearlyDetailsDto>();
    }

    public class LoanYearlyDetailsDto
    {
        public int Year { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal InterestExpense { get; set; }
        public decimal RedemptionAmount { get; set; }
    }

    public class EnhancedBalanceSheetDto
    {
        public int Id { get; set; }
        public int DebtorID { get; set; }
        public int BookYear { get; set; }
        public decimal CurrentAssets { get; set; }
        public decimal LongTermAssets { get; set; }
        public decimal CurrentLiabilities { get; set; }
        public decimal LongTermLiabilities { get; set; }
        public decimal OwnersEquity { get; set; }
        
        // Enhanced fields
        public decimal TotalLoanDebt { get; set; }
        public decimal TotalCollateralValue { get; set; }
        public decimal TotalSubordinationAmount { get; set; }
        public List<int> ActiveLoanIds { get; set; } = new List<int>();
        
        // First lien loan fields (external mortgage)
        public decimal? FirstLienLoanAmount { get; set; }
        public decimal? FirstLienInterestRate { get; set; }
        public int? FirstLienTenorMonths { get; set; }
        public string? FirstLienRedemptionSchedule { get; set; }
    }

    public class EnhancedProfitLossDto
    {
        public int Id { get; set; }
        public int DebtorID { get; set; }
        public int BookYear { get; set; }
        public decimal Revenue { get; set; }
        public decimal OperatingExpenses { get; set; }
        public decimal CostOfGoodsSold { get; set; }
        public decimal EBITDA { get; set; }
        public decimal InterestExpense { get; set; }
        public decimal TaxExpense { get; set; }
        public decimal NetIncome { get; set; }
        public string? RevenueSectorBreakdown { get; set; }
        
        // Enhanced fields
        public decimal CalculatedLoanInterest { get; set; }
        public decimal OtherInterestExpense { get; set; }
        public List<int> ActiveLoanIds { get; set; } = new List<int>();
    }
}
