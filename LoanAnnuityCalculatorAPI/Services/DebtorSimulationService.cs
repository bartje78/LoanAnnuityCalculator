using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Models.DTOs;

namespace LoanAnnuityCalculatorAPI.Services
{
    /// <summary>
    /// Service responsible for simulating a single debtor's financial performance over time.
    /// Uses pre-generated shocks and existing financial calculation services.
    /// </summary>
    public class DebtorSimulationService
    {
        private readonly LoanFinancialCalculatorService _loanCalculator;

        public DebtorSimulationService(LoanFinancialCalculatorService loanCalculator)
        {
            _loanCalculator = loanCalculator;
        }

        /// <summary>
        /// Information needed to simulate a single loan
        /// </summary>
        public class SimulatedLoanInfo
        {
            public int LoanId { get; set; }
            public decimal LoanAmount { get; set; }
            public decimal InterestRate { get; set; }
            public int TenorMonths { get; set; }
            public string RedemptionSchedule { get; set; } = "Linear";
            public decimal CollateralValue { get; set; }
            public decimal LiquidityHaircut { get; set; }
            public decimal Subordination { get; set; }
            public string CollateralPropertyType { get; set; } = "";
            public List<YearlyPaymentInfo> YearlyPayments { get; set; } = new();
        }

        /// <summary>
        /// Information about loan payments for a specific year
        /// </summary>
        public class YearlyPaymentInfo
        {
            public int Year { get; set; }
            public decimal InterestExpense { get; set; }
            public decimal RedemptionAmount { get; set; }
            public decimal OutstandingBalance { get; set; }
        }

        /// <summary>
        /// Result of a single simulation path for a debtor
        /// </summary>
        public class SimulationPath
        {
            public int SimulationNumber { get; set; }
            public List<YearResult> Years { get; set; } = new();
            public bool DefaultOccurred { get; set; }
            public int DefaultYear { get; set; }
            public decimal? LossGivenDefault { get; set; }
            public decimal CollateralValueAtDefault { get; set; }
        }

        /// <summary>
        /// Financial results for a single year in the simulation
        /// </summary>
        public class YearResult
        {
            public int Year { get; set; }
            public decimal Revenue { get; set; }
            public decimal OperatingCosts { get; set; }
            public decimal Ebitda { get; set; }
            public decimal EbitdaMargin { get; set; }
            public decimal InterestExpense { get; set; }
            public decimal CorporateTax { get; set; }
            public decimal RedemptionAmount { get; set; }
            public decimal NetIncome { get; set; }
            public decimal Assets { get; set; }
            public decimal Debt { get; set; }
            public decimal Equity { get; set; }
            public decimal InterestCoverage { get; set; }
            public bool CanPayInterest { get; set; }
            public decimal LiquidAssets { get; set; }
            public decimal LiquidAssetsChange { get; set; }
        }

        /// <summary>
        /// Simulate a single debtor for one Monte Carlo path using pre-generated shocks
        /// </summary>
        public SimulationPath SimulateDebtor(
            MonteCarloSimulationRequest request,
            List<SimulatedLoanInfo> loans,
            ShockGenerationService.SimulationShockSet shockSet,
            int simulationNumber)
        {
            // Log parameters for debugging (first simulation only)
            if (simulationNumber == 1)
            {
                Console.WriteLine($"[DEBTOR SIM] Debtor {request.DebtorId}:");
                Console.WriteLine($"  InitialRevenue: {request.InitialRevenue:N2}");
                Console.WriteLine($"  InitialOperatingCosts: {request.InitialOperatingCosts:N2}");
                Console.WriteLine($"  InitialLiquidAssets: {request.InitialLiquidAssets:N2}");
                Console.WriteLine($"  RevenueGrowthRate: {request.RevenueGrowthRate:P2}");
                Console.WriteLine($"  RevenueVolatility: {request.RevenueVolatility:P2}");
                Console.WriteLine($"  OperatingCostVolatility: {request.OperatingCostVolatility:P2}");
                Console.WriteLine($"  CollateralExpectedReturn: {request.CollateralExpectedReturn:P2}");
                Console.WriteLine($"  CollateralVolatility: {request.CollateralVolatility:P2}");
                Console.WriteLine($"  Loans: {loans.Count}");
            }
            
            var path = new SimulationPath { SimulationNumber = simulationNumber };

            // Initialize financial state
            decimal currentRevenue = request.InitialRevenue;
            decimal currentOperatingCosts = request.InitialOperatingCosts > 0 
                ? request.InitialOperatingCosts 
                : request.InitialRevenue * (1 - request.InitialEbitdaMargin);
            decimal currentEquity = request.InitialEquity;
            decimal currentDebt = request.InitialDebt + request.LoanAmount;
            decimal currentAssets = request.InitialLiquidAssets > 0 ? request.InitialLiquidAssets : request.InitialAssets;
            decimal totalAssets = request.InitialAssets;
            decimal fixedAssets = totalAssets - currentAssets;
            decimal previousLiquidAssets = currentAssets;

            // Initialize collateral values
            var collateralValues = new Dictionary<int, decimal>();
            foreach (var loan in loans)
            {
                collateralValues[loan.LoanId] = loan.CollateralValue;
            }

            // Simulate each year
            for (int year = 1; year <= request.SimulationYears; year++)
            {
                // If already defaulted, freeze values
                if (path.DefaultOccurred)
                {
                    var frozenYear = ClonePreviousYear(path.Years.Last(), year);
                    path.Years.Add(frozenYear);
                    continue;
                }

                // Get pre-generated shocks for this year
                var sectorShocks = shockSet.GetSectorShocks(simulationNumber, year);
                var collateralShocks = shockSet.GetCollateralShocks(simulationNumber, year);

                // Calculate revenue shock from sector exposures
                double revenueShock = CalculateRevenueShock(
                    sectorShocks, 
                    request.SectorWeights, 
                    request.RevenueVolatility);

                // Calculate operating cost shock
                double operatingCostShock = GenerateIndependentShock(
                    request.OperatingCostVolatility > 0 
                        ? request.OperatingCostVolatility 
                        : request.EbitdaMarginVolatility);

                // Apply shocks to revenue and costs
                currentRevenue *= (1 + request.RevenueGrowthRate + (decimal)revenueShock);
                currentOperatingCosts *= (1 + request.OperatingCostGrowthRate + (decimal)operatingCostShock);

                // Update collateral values using pre-generated shocks
                UpdateCollateralValues(
                    collateralValues, 
                    loans, 
                    collateralShocks, 
                    request.CollateralExpectedReturn,
                    simulationNumber,
                    year);

                // Calculate P&L
                decimal ebitda = currentRevenue - currentOperatingCosts;
                decimal ebitdaMargin = currentRevenue > 0 ? ebitda / currentRevenue : 0;

                // Calculate loan payments using pre-calculated schedule
                var (totalInterest, totalRedemption, portfolioInterest) = CalculateLoanPayments(loans, year);
                decimal totalPayments = totalInterest + totalRedemption;

                // Calculate tax
                decimal taxableIncome = Math.Max(0, ebitda - totalInterest);
                decimal corporateTax = taxableIncome * request.CorporateTaxRate;
                decimal netIncome = ebitda - totalInterest - corporateTax;

                // Check liquidity and handle shortfall
                decimal actualRedemption = totalRedemption;
                if (ebitda < totalPayments)
                {
                    decimal shortage = totalPayments - ebitda;
                    currentAssets -= shortage;
                    
                    // If drawing from reserves, prioritize interest over redemption
                    actualRedemption = ebitda >= totalInterest 
                        ? Math.Min(shortage, totalRedemption) 
                        : 0;
                }
                else
                {
                    // EBITDA covers payments, add surplus to liquid assets
                    decimal surplus = ebitda - totalPayments - corporateTax;
                    currentAssets += surplus;
                }

                // Update balance sheet
                currentDebt -= actualRedemption;
                
                // Total assets = liquid assets + fixed assets
                // Note: Collateral values are already part of fixed assets on the balance sheet
                // They are tracked separately only for LGD calculation purposes
                totalAssets = currentAssets + fixedAssets;
                currentEquity = totalAssets - currentDebt;
                
                decimal liquidAssetsChange = currentAssets - previousLiquidAssets;
                previousLiquidAssets = currentAssets;

                // Calculate interest coverage
                decimal interestCoverage = totalInterest > 0 ? ebitda / totalInterest : 999;
                bool canPayInterest = ebitda >= totalInterest;

                // Store year result
                var yearResult = new YearResult
                {
                    Year = year,
                    Revenue = currentRevenue,
                    OperatingCosts = currentOperatingCosts,
                    Ebitda = ebitda,
                    EbitdaMargin = ebitdaMargin,
                    InterestExpense = totalInterest,
                    CorporateTax = corporateTax,
                    RedemptionAmount = actualRedemption,
                    NetIncome = netIncome,
                    Assets = totalAssets,
                    Debt = currentDebt,
                    Equity = currentEquity,
                    InterestCoverage = interestCoverage,
                    CanPayInterest = canPayInterest,
                    LiquidAssets = currentAssets,
                    LiquidAssetsChange = liquidAssetsChange
                };

                path.Years.Add(yearResult);

                // Check for default (EBITDA insufficient AND liquid assets depleted)
                if (ebitda < totalPayments && currentAssets <= 0)
                {
                    MarkAsDefaulted(path, year, loans, collateralValues, currentDebt, simulationNumber);
                    
                    // Fill remaining years with frozen values
                    for (int remainingYear = year + 1; remainingYear <= request.SimulationYears; remainingYear++)
                    {
                        var frozenYear = ClonePreviousYear(yearResult, remainingYear);
                        path.Years.Add(frozenYear);
                    }
                    
                    break;
                }
            }

            return path;
        }

        /// <summary>
        /// Calculate revenue shock from sector exposures
        /// </summary>
        private double CalculateRevenueShock(
            Dictionary<Sector, double>? sectorShocks,
            Dictionary<Sector, decimal>? sectorWeights,
            decimal revenueVolatility)
        {
            if (sectorShocks == null || sectorWeights == null || !sectorWeights.Any())
            {
                // Fallback to simple independent shock
                return GenerateIndependentShock(revenueVolatility);
            }

            double weightedShock = 0;
            foreach (var kvp in sectorWeights)
            {
                if (sectorShocks.TryGetValue(kvp.Key, out double shock))
                {
                    weightedShock += (double)kvp.Value * shock;
                }
            }

            return weightedShock;
        }

        /// <summary>
        /// Generate an independent random shock
        /// </summary>
        private double GenerateIndependentShock(decimal volatility)
        {
            var random = new Random();
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            double standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return standardNormal * (double)volatility;
        }

        /// <summary>
        /// Update collateral values using pre-generated shocks
        /// </summary>
        private void UpdateCollateralValues(
            Dictionary<int, decimal> collateralValues,
            List<SimulatedLoanInfo> loans,
            Dictionary<string, double>? collateralShocks,
            decimal expectedReturn,
            int simulationNumber,
            int year)
        {
            foreach (var loan in loans)
            {
                if (!collateralValues.ContainsKey(loan.LoanId))
                    continue;

                double shock = 0;
                if (collateralShocks != null && 
                    !string.IsNullOrEmpty(loan.CollateralPropertyType) &&
                    collateralShocks.TryGetValue(loan.CollateralPropertyType, out double propertyShock))
                {
                    shock = propertyShock;
                }

                var oldValue = collateralValues[loan.LoanId];
                var newValue = oldValue * (1 + expectedReturn + (decimal)shock);
                collateralValues[loan.LoanId] = Math.Max(0, newValue);

                if (simulationNumber == 1 && year <= 3)
                {
                    Console.WriteLine($"[COLLATERAL UPDATE] Sim#{simulationNumber}, Year {year}, Loan {loan.LoanId}: " +
                        $"{oldValue:N2} → {collateralValues[loan.LoanId]:N2} (shock: {shock:P2})");
                }
            }
        }

        /// <summary>
        /// Calculate loan payments for a specific year
        /// </summary>
        private (decimal totalInterest, decimal totalRedemption, decimal portfolioInterest) CalculateLoanPayments(
            List<SimulatedLoanInfo> loans,
            int year)
        {
            decimal totalInterest = 0;
            decimal totalRedemption = 0;
            decimal portfolioInterest = 0;

            foreach (var loan in loans)
            {
                var yearPayment = loan.YearlyPayments.FirstOrDefault(yp => yp.Year == year);
                if (yearPayment != null)
                {
                    totalInterest += yearPayment.InterestExpense;
                    totalRedemption += yearPayment.RedemptionAmount;

                    // Only track interest for portfolio loans (exclude external loans with negative IDs)
                    if (loan.LoanId >= 0)
                    {
                        portfolioInterest += yearPayment.InterestExpense;
                    }
                }
            }

            return (totalInterest, totalRedemption, portfolioInterest);
        }

        /// <summary>
        /// Mark simulation path as defaulted and calculate LGD
        /// </summary>
        private void MarkAsDefaulted(
            SimulationPath path,
            int year,
            List<SimulatedLoanInfo> loans,
            Dictionary<int, decimal> collateralValues,
            decimal currentDebt,
            int simulationNumber)
        {
            path.DefaultOccurred = true;
            path.DefaultYear = year;

            // Calculate total collateral value
            decimal totalCollateralValue = collateralValues.Values.Sum();
            path.CollateralValueAtDefault = totalCollateralValue;

            // Calculate external first lien outstanding
            decimal externalFirstLien = 0;
            foreach (var loan in loans)
            {
                if (loan.LoanId < 0) // External loan
                {
                    var yearPayment = loan.YearlyPayments.FirstOrDefault(yp => yp.Year == year);
                    externalFirstLien += yearPayment?.OutstandingBalance ?? 0;
                }
            }

            // Portfolio outstanding (exclude external first lien)
            decimal portfolioOutstanding = currentDebt - externalFirstLien;

            // Calculate collateral recovery after haircuts and subordination
            decimal totalCollateralAfterHaircut = 0;
            decimal totalSubordination = 0;

            foreach (var loan in loans)
            {
                if (collateralValues.ContainsKey(loan.LoanId))
                {
                    decimal collateralValue = collateralValues[loan.LoanId];
                    decimal valueAfterHaircut = collateralValue * (1 - loan.LiquidityHaircut / 100m);
                    totalCollateralAfterHaircut += valueAfterHaircut;

                    // Subordination counted once
                    if (totalSubordination == 0)
                    {
                        totalSubordination = loan.Subordination;
                    }
                }
            }

            // Available recovery pool after subtracting senior debt
            decimal availableRecoveryPool = Math.Max(0, totalCollateralAfterHaircut - totalSubordination);
            decimal totalRecovery = Math.Min(availableRecoveryPool, portfolioOutstanding);
            decimal lossAmount = Math.Max(0, portfolioOutstanding - totalRecovery);

            path.LossGivenDefault = lossAmount;

            if (simulationNumber <= 3)
            {
                Console.WriteLine($"[DEFAULT] Sim#{simulationNumber}, Year {year}:");
                Console.WriteLine($"  Collateral value: {totalCollateralValue:N2}");
                Console.WriteLine($"  After haircut: {totalCollateralAfterHaircut:N2}");
                Console.WriteLine($"  Subordination: {totalSubordination:N2}");
                Console.WriteLine($"  Recovery pool: {availableRecoveryPool:N2}");
                Console.WriteLine($"  Portfolio outstanding: {portfolioOutstanding:N2}");
                Console.WriteLine($"  Loss Given Default: {lossAmount:N2}");
            }
        }

        /// <summary>
        /// Clone the previous year's results with updated year number (for post-default frozen state)
        /// </summary>
        private YearResult ClonePreviousYear(YearResult previous, int newYear)
        {
            return new YearResult
            {
                Year = newYear,
                Revenue = previous.Revenue,
                OperatingCosts = previous.OperatingCosts,
                Ebitda = previous.Ebitda,
                EbitdaMargin = previous.EbitdaMargin,
                InterestExpense = previous.InterestExpense,
                CorporateTax = previous.CorporateTax,
                RedemptionAmount = previous.RedemptionAmount,
                NetIncome = previous.NetIncome,
                Assets = previous.Assets,
                Debt = previous.Debt,
                Equity = previous.Equity,
                InterestCoverage = previous.InterestCoverage,
                CanPayInterest = previous.CanPayInterest,
                LiquidAssets = previous.LiquidAssets,
                LiquidAssetsChange = previous.LiquidAssetsChange
            };
        }
    }
}
