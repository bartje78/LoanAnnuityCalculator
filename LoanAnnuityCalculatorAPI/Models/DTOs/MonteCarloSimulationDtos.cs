namespace LoanAnnuityCalculatorAPI.Models.DTOs
{
    public class MonteCarloSimulationRequest
    {
        public int DebtorId { get; set; }
        
        // Option 1: Use actual loans from database (preferred)
        public bool UseActualLoans { get; set; } = true;
        public List<int>? LoanIds { get; set; } // If null, use all loans for the debtor
        
        // Control whether to include first lien loan (external mortgage)
        public bool IncludeFirstLien { get; set; } = true;
        
        // Option 2: Use hypothetical loan (legacy)
        public decimal LoanAmount { get; set; }
        public decimal InterestRate { get; set; }
        
        public int SimulationYears { get; set; } = 10;
        public int NumberOfSimulations { get; set; } = 1000;
        
        // Initial balance sheet items
        public decimal InitialRevenue { get; set; }
        public decimal InitialEbitdaMargin { get; set; } // DEPRECATED: Will be replaced by cost-based modeling
        public decimal InitialOperatingCosts { get; set; } // NEW: Operating costs (excluding interest)
        public decimal InitialAssets { get; set; } // Total assets (for display/balance)
        public decimal InitialLiquidAssets { get; set; } // Current assets (liquid, available for cash flow)
        public decimal InitialEquity { get; set; }
        public decimal InitialDebt { get; set; }
        
        // Growth parameters
        public decimal RevenueGrowthRate { get; set; } = 0.00m; // 0% default (no growth assumption)
        public decimal OperatingCostGrowthRate { get; set; } = 0.02m; // 2% default (costs grow faster than revenue)
        
        // Volatility parameters (annual standard deviation)
        public decimal RevenueVolatility { get; set; } = 0.15m; // 15% default
        public decimal EbitdaMarginVolatility { get; set; } = 0.05m; // 5% default - DEPRECATED
        public decimal OperatingCostVolatility { get; set; } = 0.10m; // 10% default - NEW
        
        // Tax parameters
        public decimal CorporateTaxRate { get; set; } = 0.21m; // 21% default (Netherlands standard rate)
        
        // Collateral modeling parameters
        public decimal CollateralExpectedReturn { get; set; } = 0.02m; // 2% annual appreciation
        public decimal CollateralVolatility { get; set; } = 0.10m; // 10% volatility
        public decimal CollateralCorrelation { get; set; } = 0.30m; // Correlation with revenue
        
        // Correlation matrix parameters
        public CorrelationMatrix? Correlations { get; set; }
    }

    public class CorrelationMatrix
    {
        public decimal RevenueToGdp { get; set; } = 0.7m;
        public decimal EbitdaToRevenue { get; set; } = 0.5m;
        public decimal InterestRateToGdp { get; set; } = -0.3m;
    }

    public class MonteCarloSimulationResponse
    {
        public int DebtorId { get; set; }
        public List<SimulatedLoanInfo> SimulatedLoans { get; set; } = new List<SimulatedLoanInfo>();
        public int TotalSimulations { get; set; }
        public int SimulationYears { get; set; }
        public SimulationStatistics Statistics { get; set; } = new SimulationStatistics();
        public List<YearlyStatistics> YearlyResults { get; set; } = new List<YearlyStatistics>();
        public List<SimulationPath> SamplePaths { get; set; } = new List<SimulationPath>(); // Store a few sample paths for visualization
        public string SimulatedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public class SimulatedLoanInfo
    {
        public int LoanId { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal InterestRate { get; set; }
        public int TenorMonths { get; set; }
        public string RedemptionSchedule { get; set; } = string.Empty;
        public decimal CollateralValue { get; set; }
        public decimal LiquidityHaircut { get; set; } // % haircut on collateral value
        public decimal Subordination { get; set; } // Amount of subordinated debt ahead of this loan
        public List<YearlyLoanPayment> YearlyPayments { get; set; } = new List<YearlyLoanPayment>();
    }

    public class YearlyLoanPayment
    {
        public int Year { get; set; }
        public decimal InterestExpense { get; set; }
        public decimal RedemptionAmount { get; set; }
        public decimal OutstandingBalance { get; set; }
    }

    public class SimulationStatistics
    {
        public decimal ProbabilityOfDefault { get; set; } // % chance of not being able to pay interest
        public decimal ExpectedRevenue { get; set; }
        public decimal ExpectedEbitda { get; set; }
        public decimal ExpectedEquity { get; set; }
        public decimal MedianEquity { get; set; } // Median equity (better measure for skewed distributions)
        public decimal MedianRevenue { get; set; }
        public decimal MedianEbitda { get; set; }
        public decimal Percentile5Revenue { get; set; } // 5th percentile (worst case)
        public decimal Percentile95Revenue { get; set; } // 95th percentile (best case)
        
        // LGD statistics
        public decimal AverageLGD { get; set; } // Average Loss Given Default in EUR (for paths with default)
        public decimal AverageLGDPercentage { get; set; } // Average LGD as % of outstanding debt (for paths with default)
        public decimal MedianLGD { get; set; }
        public decimal ExpectedLoss { get; set; } // PD × LGD × EAD (expected loss across all simulations)
        
        // ROI statistics (Return on Investment)
        public decimal NominalLoanAmount { get; set; } // Original portfolio loan amount
        public decimal InterestPaidBeforeSimulation { get; set; } // Interest paid from inception to simulation start
        public decimal MedianCumulativeInterestDuringSimulation { get; set; } // Median interest paid during simulation period
        public decimal MedianTotalInterest { get; set; } // Total interest (before + during simulation)
        public decimal MedianROI { get; set; } // (Total Interest - Expected Loss) / Nominal Amount
    }

    public class YearlyStatistics
    {
        public int Year { get; set; }
        public decimal AveragRevenue { get; set; }
        public decimal AverageEbitda { get; set; }
        public decimal AverageInterestExpense { get; set; } // Average interest expense across all simulations
        public decimal AverageInterestCoverage { get; set; } // EBITDA / Interest Expense
        public decimal ProbabilityCannotPayInterest { get; set; } // % of simulations where EBITDA < Interest in this year
        public decimal CumulativeDefaultProbability { get; set; } // % of simulations that have defaulted by this year
        public decimal AverageEquity { get; set; }
        public decimal AverageDebt { get; set; }
        
        // Cash flow breakdown
        public decimal AverageNetProfit { get; set; } // Net profit after interest
        public decimal AverageRedemptionAmount { get; set; } // Capital repayment
        public decimal AverageLiquidAssets { get; set; } // Current assets (liquid buffer)
        public decimal AverageLiquidAssetsChange { get; set; } // Change in liquid assets
        public decimal ProbabilityNegativeCashFlow { get; set; } // % with negative cash flow (drawing from liquid assets)
        
        // Median values (better for skewed distributions)
        public decimal MedianEbitda { get; set; }
        public decimal MedianInterestExpense { get; set; }
        public decimal MedianNetProfit { get; set; }
        public decimal MedianRedemptionAmount { get; set; }
        public decimal MedianLiquidAssets { get; set; }
        public decimal MedianLiquidAssetsChange { get; set; }
        public decimal MedianInterestCoverage { get; set; }
        
        // Percentiles for charting
        public decimal MedianRevenue { get; set; }
        public decimal Percentile10Revenue { get; set; }
        public decimal Percentile90Revenue { get; set; }
        
        public decimal Percentile10Ebitda { get; set; }
        public decimal Percentile90Ebitda { get; set; }
        
        public decimal MedianEquity { get; set; }
        public decimal Percentile10Equity { get; set; }
        public decimal Percentile90Equity { get; set; }
        
        public decimal MedianDebt { get; set; }
        public decimal Percentile10Debt { get; set; }
        public decimal Percentile90Debt { get; set; }
        
        public decimal Percentile10LiquidAssets { get; set; }
        public decimal Percentile90LiquidAssets { get; set; }
        
        // Legacy fields (keeping for backwards compatibility)
        public decimal Percentile5Revenue { get; set; }
        public decimal Percentile95Revenue { get; set; }
    }

    public class SimulationPath
    {
        public int SimulationNumber { get; set; }
        public List<YearResult> Years { get; set; } = new List<YearResult>();
        public bool DefaultOccurred { get; set; }
        public int? DefaultYear { get; set; }
        
        // LGD calculation (only populated if default occurred)
        public decimal? CollateralValueAtDefault { get; set; }
        public decimal? RecoveryAmount { get; set; } // Collateral value after haircut and subordination
        public decimal? OutstandingDebtAtDefault { get; set; }
        public decimal? LossGivenDefault { get; set; } // Outstanding - Recovery
        public decimal? LGDPercentage { get; set; } // (Loss / Outstanding) × 100
        
        // ROI calculation
        public decimal CumulativeInterestPaid { get; set; } // Total interest paid during simulation period
    }

    public class YearResult
    {
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public decimal OperatingCosts { get; set; } // NEW: Operating costs (ex interest)
        public decimal EbitdaMargin { get; set; } // DEPRECATED: For backward compatibility
        public decimal Ebitda { get; set; } // Revenue - OperatingCosts
        public decimal InterestExpense { get; set; }
        public decimal CorporateTax { get; set; } // NEW: Tax on (EBITDA - Interest)
        public decimal NetIncome { get; set; } // EBITDA - Interest - Tax
        public decimal Assets { get; set; }
        public decimal Equity { get; set; }
        public decimal Debt { get; set; }
        public decimal InterestCoverage { get; set; }
        public bool CanPayInterest { get; set; }
        
        // Cash flow tracking
        public decimal RedemptionAmount { get; set; }
        public decimal LiquidAssets { get; set; } // Current assets
        public decimal LiquidAssetsChange { get; set; } // Change from previous year
    }

    // Model settings DTOs moved to LoanAnnuityCalculatorAPI.Models.Settings namespace
    // Kept here as DTOs for API responses
    public class PropertyTypeParametersDto
    {
        public string PropertyType { get; set; } = string.Empty;
        public decimal ExpectedReturn { get; set; } = 0.02m;
        public decimal Volatility { get; set; } = 0.10m;
        public decimal CorrelationWithRevenue { get; set; } = 0.30m;
    }

    public class ModelSettingsDto
    {
        public int Id { get; set; }
        public string SettingName { get; set; } = "Default";
        public decimal DefaultRevenueGrowthRate { get; set; } = 0.00m;
        public decimal DefaultOperatingCostGrowthRate { get; set; } = 0.02m;
        public decimal DefaultRevenueVolatility { get; set; } = 0.15m;
        public decimal DefaultEbitdaMarginVolatility { get; set; } = 0.05m;
        public decimal DefaultOperatingCostVolatility { get; set; } = 0.10m;
        public decimal DefaultCorporateTaxRate { get; set; } = 0.21m;
        public decimal DefaultCollateralExpectedReturn { get; set; } = 0.02m;
        public decimal DefaultCollateralVolatility { get; set; } = 0.10m;
        public decimal DefaultCollateralCorrelation { get; set; } = 0.30m;
        public List<PropertyTypeParametersDto> PropertyTypeParameters { get; set; } = new List<PropertyTypeParametersDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ModelSettingsResponse
    {
        public ModelSettingsDto Settings { get; set; } = new ModelSettingsDto();
    }
}
