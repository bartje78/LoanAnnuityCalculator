namespace LoanAnnuityCalculatorAPI.Models.DTOs
{
    public class TariffCalculationRequest
    {
        public decimal LoanAmount { get; set; }
        public int LoanTerm { get; set; }
        public decimal CollateralValue { get; set; }
        public decimal SubordinationAmount { get; set; }
        public decimal LiquidityHaircut { get; set; }
        public int InterestOnlyPeriod { get; set; }
        public string CreditRating { get; set; } = "Low";
        public decimal BaseRate { get; set; }
        public decimal? LtvSpread { get; set; }
        public decimal? RatingSpread { get; set; }
        public decimal? ExtraSpread { get; set; }
        public string RedemptionScheme { get; set; } = "Annuity"; // "Annuity", "Linear", or "Bullet"
        public bool IsNewCompany { get; set; } = false; // Flag for companies with no financial history
    }

    public class TariffCalculationResponse
    {
        public decimal LTV { get; set; }
        public decimal InterestRate { get; set; }
        public decimal BaseRate { get; set; }
        public decimal LtvSpread { get; set; }
        public decimal RatingSpread { get; set; }
        public decimal ExtraSpread { get; set; }
        public decimal MonthlyPayment { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BSE { get; set; }
        public BseBreakdown BseBreakdown { get; set; } = new BseBreakdown();
        public List<PaymentScheduleItem> PaymentSchedule { get; set; } = new List<PaymentScheduleItem>();
        public ChartData ChartData { get; set; } = new ChartData();
    }

    public class BseBreakdown
    {
        public decimal MarketRate { get; set; }
        public decimal LoanRate { get; set; }
        public MarketRateBreakdown MarketRateBreakdown { get; set; } = new MarketRateBreakdown();
        public List<BseYearlyDetail> YearlyBreakdown { get; set; } = new List<BseYearlyDetail>();
    }

    public class MarketRateBreakdown
    {
        public decimal BaseRate { get; set; }
        public decimal RiskPremium { get; set; }
        public string CreditRating { get; set; } = string.Empty;
        public decimal LTV { get; set; }
        public string SecurityLevel { get; set; } = string.Empty;
        public bool IsNewCompany { get; set; }
        public bool NewCompanyMinimumApplied { get; set; }
        
        // Deprecated - kept for backward compatibility
        [Obsolete("Use RiskPremium instead. This represents the combined matrix value.")]
        public decimal RatingPremium { get; set; }
        
        [Obsolete("No longer used. Risk premium is now a single matrix value based on rating and security level.")]
        public decimal LtvAdjustment { get; set; }
    }

    public class BseYearlyDetail
    {
        public int Year { get; set; }
        public decimal MarketInterest { get; set; }
        public decimal LoanInterest { get; set; }
        public decimal Difference { get; set; }
        public decimal DiscountedValue { get; set; }
    }

    public class PaymentScheduleItem
    {
        public int Month { get; set; }
        public decimal InterestComponent { get; set; }
        public decimal CapitalComponent { get; set; }
        public decimal RemainingLoan { get; set; }
    }

    public class ChartData
    {
        public List<decimal> InterestComponent { get; set; } = new List<decimal>();
        public List<decimal> CapitalComponent { get; set; } = new List<decimal>();
        public List<string> Labels { get; set; } = new List<string>();
    }
}
