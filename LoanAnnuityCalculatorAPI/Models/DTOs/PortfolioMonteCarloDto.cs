namespace LoanAnnuityCalculatorAPI.Models.DTOs
{
    /// <summary>
    /// Request for portfolio-level Monte Carlo simulation across multiple debtors
    /// </summary>
    public class PortfolioMonteCarloRequest
    {
        public List<int> DebtorIds { get; set; } = new List<int>(); // List of debtors to include in portfolio
        
        public int SimulationYears { get; set; } = 10;
        public int NumberOfSimulations { get; set; } = 1000;
        
        // Global parameters (can be overridden per debtor if needed)
        public bool IncludeFirstLien { get; set; } = true;
        public decimal CorporateTaxRate { get; set; } = 0.21m;
        
        // Growth parameters per debtor (optional - will use P&L data if not provided)
        public Dictionary<int, decimal>? DebtorRevenueGrowthRates { get; set; }
        public Dictionary<int, decimal>? DebtorOperatingCostGrowthRates { get; set; }
    }

    /// <summary>
    /// Response for portfolio-level Monte Carlo simulation
    /// Aggregates results across all debtors in the portfolio
    /// </summary>
    public class PortfolioMonteCarloResponse
    {
        public List<int> DebtorIds { get; set; } = new List<int>();
        public int TotalSimulations { get; set; }
        public int SimulationYears { get; set; }
        
        // Portfolio-level aggregated statistics
        public PortfolioStatistics PortfolioStats { get; set; } = new PortfolioStatistics();
        
        // Per-debtor results
        public List<DebtorSimulationSummary> DebtorResults { get; set; } = new List<DebtorSimulationSummary>();
        
        // Yearly portfolio results
        public List<PortfolioYearlyStatistics> YearlyResults { get; set; } = new List<PortfolioYearlyStatistics>();
        
        // Sample paths (worst, median, best scenarios)
        public List<PortfolioSimulationPath> SamplePaths { get; set; } = new List<PortfolioSimulationPath>();
        
        public string SimulatedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public class PortfolioStatistics
    {
        // Overall portfolio metrics
        public decimal TotalLoanAmount { get; set; }
        public decimal AveragePortfolioPD { get; set; } // Average probability of default across portfolio
        public decimal PortfolioExpectedLoss { get; set; } // Total expected loss in EUR
        public decimal PortfolioExpectedLossPercentage { get; set; } // As % of total loan amount
        
        // ROI metrics
        public decimal MedianTotalInterest { get; set; }
        public decimal MedianROI { get; set; }
        
        // Concentration metrics
        public Dictionary<string, decimal> SectorConcentration { get; set; } = new Dictionary<string, decimal>(); // % exposure per sector
        public Dictionary<string, decimal> PropertyTypeConcentration { get; set; } = new Dictionary<string, decimal>(); // % exposure per property type
        
        // Correlation impact
        public decimal DiversificationBenefit { get; set; } // Reduction in risk due to diversification (%)
    }

    public class DebtorSimulationSummary
    {
        public int DebtorId { get; set; }
        public string DebtorName { get; set; } = string.Empty;
        public decimal LoanAmount { get; set; }
        public decimal ProbabilityOfDefault { get; set; }
        public decimal ExpectedLoss { get; set; }
        public decimal ExpectedLossPercentage { get; set; }
        public Dictionary<Models.Sector, decimal> SectorWeights { get; set; } = new Dictionary<Models.Sector, decimal>();
        public string PrimaryPropertyType { get; set; } = string.Empty;
    }

    public class PortfolioYearlyStatistics
    {
        public int Year { get; set; }
        
        // Aggregated portfolio metrics
        public decimal TotalRevenue { get; set; }
        public decimal TotalEbitda { get; set; }
        public decimal TotalInterestExpense { get; set; }
        public decimal TotalDebt { get; set; }
        public decimal TotalEquity { get; set; }
        
        // Risk metrics
        public decimal CumulativeDefaultProbability { get; set; } // % of debtors that have defaulted by this year
        public int NumberOfDefaults { get; set; } // Count of debtors in default
        public decimal TotalLossGivenDefault { get; set; } // Total LGD across all defaults
        
        // Percentiles
        public decimal MedianTotalRevenue { get; set; }
        public decimal Percentile10Revenue { get; set; }
        public decimal Percentile90Revenue { get; set; }
    }

    public class PortfolioSimulationPath
    {
        public int SimulationNumber { get; set; }
        public List<PortfolioYearResult> Years { get; set; } = new List<PortfolioYearResult>();
        
        // Summary statistics for this simulation path
        public int TotalDefaults { get; set; } // How many debtors defaulted in this simulation
        public decimal TotalLoss { get; set; } // Total loss across all defaults
        public List<int> DefaultedDebtorIds { get; set; } = new List<int>(); // Which debtors defaulted
    }

    public class PortfolioYearResult
    {
        public int Year { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalEbitda { get; set; }
        public decimal TotalInterestExpense { get; set; }
        public decimal TotalDebt { get; set; }
        public decimal TotalEquity { get; set; }
        public int ActiveDefaults { get; set; } // Number of debtors in default at end of year
    }
}
