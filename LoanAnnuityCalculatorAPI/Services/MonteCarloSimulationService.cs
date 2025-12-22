using LoanAnnuityCalculatorAPI.Models.DTOs;
using LoanAnnuityCalculatorAPI.Models;

namespace LoanAnnuityCalculatorAPI.Services
{
    /// <summary>
    /// Orchestrates Monte Carlo simulations for both individual debtors and portfolios.
    /// Uses ShockGenerationService for consistent shock generation and DebtorSimulationService for execution.
    /// </summary>
    public class MonteCarloSimulationService
    {
        private readonly ShockGenerationService _shockGenerationService;
        private readonly DebtorSimulationService _debtorSimulationService;

        public MonteCarloSimulationService(
            ShockGenerationService shockGenerationService,
            DebtorSimulationService debtorSimulationService)
        {
            _shockGenerationService = shockGenerationService;
            _debtorSimulationService = debtorSimulationService;
        }

        /// <summary>
        /// Run Monte Carlo simulation for a single debtor
        /// </summary>
        public MonteCarloSimulationResponse RunSimulation(
            MonteCarloSimulationRequest request, 
            List<SimulatedLoanInfo> simulatedLoans)
        {
            Console.WriteLine($"[MONTE CARLO] Starting simulation for debtor {request.DebtorId}: " +
                $"{request.NumberOfSimulations} simulations, {request.SimulationYears} years");

            // Step 1: Collect sectors and collateral types
            var sectors = request.SectorWeights?.Keys.ToList() ?? new List<Sector>();
            var collateralTypes = simulatedLoans
                .Select(l => l.CollateralPropertyType)
                .Distinct()
                .Where(pt => !string.IsNullOrEmpty(pt))
                .ToList();

            // Step 2: Generate ALL shocks upfront
            ShockGenerationService.SimulationShockSet shockSet;
            
            Console.WriteLine($"[MONTE CARLO SHOCK DEBUG] collateralTypes.Any() = {collateralTypes.Any()}");
            Console.WriteLine($"[MONTE CARLO SHOCK DEBUG] sectors.Any() = {sectors.Any()}");
            Console.WriteLine($"[MONTE CARLO SHOCK DEBUG] request.CollateralVolatility = {request.CollateralVolatility}");
            
            // Always generate shocks if we have collateral types, even without sector data
            if (collateralTypes.Any() && 
                (sectors.Any() || request.CollateralVolatility > 0))
            {
                var collateralVolatilities = BuildCollateralVolatilities(collateralTypes, request.CollateralVolatility);
                
                // Use sector-based generation if we have sector data, otherwise independent shocks
                if (sectors.Any() && request.SectorCorrelationMatrix != null && request.SectorVolatilities != null)
                {
                    shockSet = _shockGenerationService.GenerateShocks(
                        request.NumberOfSimulations,
                        request.SimulationYears,
                        sectors,
                        collateralTypes,
                        request.SectorCorrelationMatrix,
                        request.SectorVolatilities,
                        request.SectorCollateralCorrelations ?? new Dictionary<(Sector, string), decimal>(),
                        collateralVolatilities);
                    
                    Console.WriteLine($"[MONTE CARLO] Using sector-based shock generation");
                }
                else
                {
                    // Generate independent collateral shocks (no sector correlations)
                    shockSet = _shockGenerationService.GenerateIndependentCollateralShocks(
                        request.NumberOfSimulations,
                        request.SimulationYears,
                        collateralTypes,
                        collateralVolatilities);
                    
                    Console.WriteLine($"[MONTE CARLO] Using independent collateral shock generation (no sector data)");
                }
            }
            else
            {
                // Create empty shock set for legacy mode (no volatility)
                shockSet = new ShockGenerationService.SimulationShockSet();
                Console.WriteLine($"[MONTE CARLO] No shocks generated (no collateral or volatility)");
            }

            // Step 3: Convert loan format
            var debtorLoans = ConvertToDebtorServiceLoans(simulatedLoans);

            // Step 4: Run simulations
            var allSimulations = new List<DebtorSimulationService.SimulationPath>();
            
            for (int sim = 1; sim <= request.NumberOfSimulations; sim++)
            {
                var path = _debtorSimulationService.SimulateDebtor(request, debtorLoans, shockSet, sim);
                allSimulations.Add(path);
            }

            // Step 5: Build response with statistics
            var response = BuildIndividualResponse(request, simulatedLoans, allSimulations);
            
            Console.WriteLine($"[MONTE CARLO] Completed. PD: {response.Statistics.ProbabilityOfDefault:F2}%, " +
                $"Expected Loss: €{response.Statistics.ExpectedLoss:N2}");

            return response;
        }

        /// <summary>
        /// Run portfolio-level Monte Carlo simulation across multiple debtors with correlated shocks
        /// </summary>
        public PortfolioMonteCarloResponse RunPortfolioSimulation(
            List<(int debtorId, string debtorName, MonteCarloSimulationRequest request, List<SimulatedLoanInfo> loans)> debtorData,
            double[,] correlationMatrix,
            Dictionary<Sector, decimal> sectorVolatilities)
        {
            int numberOfSimulations = debtorData.First().request.NumberOfSimulations;
            int simulationYears = debtorData.First().request.SimulationYears;

            Console.WriteLine($"[PORTFOLIO MC] Starting simulation for {debtorData.Count} debtors: " +
                $"{numberOfSimulations} simulations, {simulationYears} years");

            // Step 1: Collect ALL sectors and collateral types across portfolio
            var allSectors = debtorData
                .SelectMany(d => d.request.SectorWeights?.Keys ?? Enumerable.Empty<Sector>())
                .Distinct()
                .ToList();
            
            var allCollateralTypes = debtorData
                .SelectMany(d => d.loans.Select(l => l.CollateralPropertyType))
                .Where(pt => !string.IsNullOrEmpty(pt))
                .Distinct()
                .ToList();

            Console.WriteLine($"[PORTFOLIO MC] Portfolio has {allSectors.Count} sectors, " +
                $"{allCollateralTypes.Count} collateral types");

            // Step 2: Aggregate sector-collateral correlations and collateral volatilities
            var aggregatedCorrelations = AggregateCollateralCorrelations(debtorData);
            var aggregatedVolatilities = AggregateCollateralVolatilities(debtorData, allCollateralTypes);

            // Step 3: Generate SHARED shocks for entire portfolio
            ShockGenerationService.SimulationShockSet shockSet;
            
            if (allSectors.Any() && correlationMatrix != null && sectorVolatilities != null)
            {
                shockSet = _shockGenerationService.GenerateShocks(
                    numberOfSimulations,
                    simulationYears,
                    allSectors,
                    allCollateralTypes,
                    correlationMatrix,
                    sectorVolatilities,
                    aggregatedCorrelations,
                    aggregatedVolatilities);
                
                Console.WriteLine($"[PORTFOLIO MC] Generated shared sector-based shocks for portfolio");
            }
            else if (allCollateralTypes.Any())
            {
                // Generate independent collateral shocks when no sector data available
                shockSet = _shockGenerationService.GenerateIndependentCollateralShocks(
                    numberOfSimulations,
                    simulationYears,
                    allCollateralTypes,
                    aggregatedVolatilities);
                
                Console.WriteLine($"[PORTFOLIO MC] Generated independent collateral shocks (no sector data in portfolio)");
            }
            else
            {
                // Create empty shock set as fallback
                shockSet = new ShockGenerationService.SimulationShockSet();
                Console.WriteLine($"[PORTFOLIO MC] No shocks generated (no collateral types)");
            }

            // Step 4: Run simulations for each debtor using SHARED shocks
            var allDebtorResults = new Dictionary<int, List<DebtorSimulationService.SimulationPath>>();
            
            foreach (var (debtorId, debtorName, request, loans) in debtorData)
            {
                var debtorLoans = ConvertToDebtorServiceLoans(loans);
                var debtorSimulations = new List<DebtorSimulationService.SimulationPath>();
                
                for (int sim = 1; sim <= numberOfSimulations; sim++)
                {
                    var path = _debtorSimulationService.SimulateDebtor(request, debtorLoans, shockSet, sim);
                    debtorSimulations.Add(path);
                }
                
                allDebtorResults[debtorId] = debtorSimulations;
                
                int defaults = debtorSimulations.Count(s => s.DefaultOccurred);
                Console.WriteLine($"[PORTFOLIO MC] {debtorName}: {defaults} defaults " +
                    $"({(decimal)defaults / numberOfSimulations * 100:F1}% PD)");
            }

            // Step 5: Aggregate portfolio statistics
            var response = BuildPortfolioResponse(debtorData, allDebtorResults, numberOfSimulations, simulationYears);
            
            Console.WriteLine($"[PORTFOLIO MC] Completed. Portfolio PD: {response.PortfolioStats.AveragePortfolioPD:F2}%, " +
                $"Expected Loss: €{response.PortfolioStats.PortfolioExpectedLoss:N2}");

            return response;
        }

        #region Individual Response Building

        private MonteCarloSimulationResponse BuildIndividualResponse(
            MonteCarloSimulationRequest request,
            List<SimulatedLoanInfo> simulatedLoans,
            List<DebtorSimulationService.SimulationPath> allSimulations)
        {
            var response = new MonteCarloSimulationResponse
            {
                DebtorId = request.DebtorId,
                TotalSimulations = request.NumberOfSimulations,
                SimulationYears = request.SimulationYears,
                SimulatedLoans = simulatedLoans
            };

            // Calculate PD
            int defaultCount = allSimulations.Count(s => s.DefaultOccurred);
            response.Statistics.ProbabilityOfDefault = (decimal)defaultCount / request.NumberOfSimulations * 100;

            // Add Year 0 (initial state)
            response.YearlyResults.Add(BuildYear0Statistics(request));

            // Calculate yearly statistics
            for (int year = 1; year <= request.SimulationYears; year++)
            {
                var yearStats = CalculateYearStatistics(allSimulations, year, request.NumberOfSimulations);
                if (yearStats != null)
                {
                    response.YearlyResults.Add(yearStats);
                }
            }

            // Calculate expected loss and final statistics
            CalculateExpectedLoss(response, allSimulations);
            CalculateFinalYearStatistics(response, allSimulations);
            CalculateROI(response, allSimulations, simulatedLoans);

            // Add sample paths for visualization
            AddSamplePaths(response, allSimulations);

            return response;
        }

        private YearlyStatistics BuildYear0Statistics(MonteCarloSimulationRequest request)
        {
            decimal initialEbitda = request.InitialOperatingCosts > 0
                ? request.InitialRevenue - request.InitialOperatingCosts
                : request.InitialRevenue * request.InitialEbitdaMargin;

            return new YearlyStatistics
            {
                Year = 0,
                AveragRevenue = request.InitialRevenue,
                AverageEbitda = initialEbitda,
                AverageEquity = request.InitialEquity,
                AverageDebt = request.InitialDebt + request.LoanAmount,
                AverageLiquidAssets = request.InitialLiquidAssets,
                MedianRevenue = request.InitialRevenue,
                Percentile10Revenue = request.InitialRevenue,
                Percentile90Revenue = request.InitialRevenue,
                Percentile5Revenue = request.InitialRevenue,
                Percentile95Revenue = request.InitialRevenue,
                MedianEbitda = initialEbitda,
                Percentile10Ebitda = initialEbitda,
                Percentile90Ebitda = initialEbitda,
                MedianEquity = request.InitialEquity,
                Percentile10Equity = request.InitialEquity,
                Percentile90Equity = request.InitialEquity,
                MedianDebt = request.InitialDebt + request.LoanAmount,
                Percentile10Debt = request.InitialDebt + request.LoanAmount,
                Percentile90Debt = request.InitialDebt + request.LoanAmount,
                MedianLiquidAssets = request.InitialLiquidAssets,
                Percentile10LiquidAssets = request.InitialLiquidAssets,
                Percentile90LiquidAssets = request.InitialLiquidAssets
            };
        }

        private YearlyStatistics? CalculateYearStatistics(
            List<DebtorSimulationService.SimulationPath> allSimulations,
            int year,
            int totalSimulations)
        {
            var yearResults = allSimulations
                .Where(s => s.Years.Any(y => y.Year == year))
                .Select(s => s.Years.First(y => y.Year == year))
                .ToList();

            if (!yearResults.Any()) return null;

            int defaultsByThisYear = allSimulations.Count(s => s.DefaultOccurred && s.DefaultYear <= year);

            return new YearlyStatistics
            {
                Year = year,
                AveragRevenue = yearResults.Average(y => y.Revenue),
                AverageEbitda = yearResults.Average(y => y.Ebitda),
                AverageInterestExpense = yearResults.Average(y => y.InterestExpense),
                AverageInterestCoverage = yearResults.Average(y => y.InterestCoverage),
                ProbabilityCannotPayInterest = (decimal)yearResults.Count(y => !y.CanPayInterest) / yearResults.Count * 100,
                CumulativeDefaultProbability = (decimal)defaultsByThisYear / totalSimulations * 100,
                AverageEquity = yearResults.Average(y => y.Equity),
                AverageDebt = yearResults.Average(y => y.Debt),
                AverageNetProfit = yearResults.Average(y => y.NetIncome),
                AverageRedemptionAmount = yearResults.Average(y => y.RedemptionAmount),
                AverageLiquidAssets = yearResults.Average(y => y.LiquidAssets),
                AverageLiquidAssetsChange = yearResults.Average(y => y.LiquidAssetsChange),
                ProbabilityNegativeCashFlow = (decimal)yearResults.Count(y => y.LiquidAssetsChange < 0) / yearResults.Count * 100,
                
                // Percentiles
                MedianRevenue = CalculatePercentile(yearResults.Select(y => y.Revenue).OrderBy(v => v).ToList(), 50),
                Percentile10Revenue = CalculatePercentile(yearResults.Select(y => y.Revenue).OrderBy(v => v).ToList(), 10),
                Percentile90Revenue = CalculatePercentile(yearResults.Select(y => y.Revenue).OrderBy(v => v).ToList(), 90),
                Percentile5Revenue = CalculatePercentile(yearResults.Select(y => y.Revenue).OrderBy(v => v).ToList(), 5),
                Percentile95Revenue = CalculatePercentile(yearResults.Select(y => y.Revenue).OrderBy(v => v).ToList(), 95),
                MedianEbitda = CalculatePercentile(yearResults.Select(y => y.Ebitda).OrderBy(v => v).ToList(), 50),
                Percentile10Ebitda = CalculatePercentile(yearResults.Select(y => y.Ebitda).OrderBy(v => v).ToList(), 10),
                Percentile90Ebitda = CalculatePercentile(yearResults.Select(y => y.Ebitda).OrderBy(v => v).ToList(), 90),
                MedianEquity = CalculatePercentile(yearResults.Select(y => y.Equity).OrderBy(v => v).ToList(), 50),
                Percentile10Equity = CalculatePercentile(yearResults.Select(y => y.Equity).OrderBy(v => v).ToList(), 10),
                Percentile90Equity = CalculatePercentile(yearResults.Select(y => y.Equity).OrderBy(v => v).ToList(), 90),
                MedianDebt = CalculatePercentile(yearResults.Select(y => y.Debt).OrderBy(v => v).ToList(), 50),
                Percentile10Debt = CalculatePercentile(yearResults.Select(y => y.Debt).OrderBy(v => v).ToList(), 10),
                Percentile90Debt = CalculatePercentile(yearResults.Select(y => y.Debt).OrderBy(v => v).ToList(), 90),
                MedianLiquidAssets = CalculatePercentile(yearResults.Select(y => y.LiquidAssets).OrderBy(v => v).ToList(), 50),
                Percentile10LiquidAssets = CalculatePercentile(yearResults.Select(y => y.LiquidAssets).OrderBy(v => v).ToList(), 10),
                Percentile90LiquidAssets = CalculatePercentile(yearResults.Select(y => y.LiquidAssets).OrderBy(v => v).ToList(), 90),
                MedianInterestExpense = CalculatePercentile(yearResults.Select(y => y.InterestExpense).OrderBy(v => v).ToList(), 50),
                MedianNetProfit = CalculatePercentile(yearResults.Select(y => y.NetIncome).OrderBy(v => v).ToList(), 50),
                MedianRedemptionAmount = CalculatePercentile(yearResults.Select(y => y.RedemptionAmount).OrderBy(v => v).ToList(), 50),
                MedianLiquidAssetsChange = CalculatePercentile(yearResults.Select(y => y.LiquidAssetsChange).OrderBy(v => v).ToList(), 50),
                MedianInterestCoverage = CalculatePercentile(yearResults.Select(y => y.InterestCoverage).OrderBy(v => v).ToList(), 50)
            };
        }

        private void CalculateExpectedLoss(
            MonteCarloSimulationResponse response,
            List<DebtorSimulationService.SimulationPath> allSimulations)
        {
            var totalDefaults = allSimulations.Count(s => s.DefaultOccurred);
            var defaultedPaths = allSimulations.Where(s => s.DefaultOccurred && s.LossGivenDefault.HasValue).ToList();
            
            Console.WriteLine($"[EXPECTED LOSS DEBUG] Total simulations: {allSimulations.Count}, " +
                $"Defaults: {totalDefaults}, Defaults with LGD: {defaultedPaths.Count}");
            
            if (totalDefaults > 0 && !defaultedPaths.Any())
            {
                Console.WriteLine($"[EXPECTED LOSS WARNING] {totalDefaults} defaults occurred but none have LGD values!");
            }
            
            if (defaultedPaths.Any())
            {
                var lgdValues = defaultedPaths.Select(s => s.LossGivenDefault!.Value).OrderBy(lgd => lgd).ToList();
                
                // Calculate comprehensive LGD statistics
                response.Statistics.AverageLGD = lgdValues.Average();
                response.Statistics.MedianLGD = CalculatePercentile(lgdValues, 50);
                
                // Add percentiles for LGD distribution
                response.Statistics.LGD_P10 = CalculatePercentile(lgdValues, 10);
                response.Statistics.LGD_P90 = CalculatePercentile(lgdValues, 90);
                response.Statistics.LGD_P95 = CalculatePercentile(lgdValues, 95);
                response.Statistics.LGD_P99 = CalculatePercentile(lgdValues, 99);
                response.Statistics.LGD_Min = lgdValues.First();
                response.Statistics.LGD_Max = lgdValues.Last();
                
                // Count how many have zero loss (fully covered by collateral)
                response.Statistics.LGD_ZeroLossCount = lgdValues.Count(lgd => lgd == 0);
                response.Statistics.LGD_ZeroLossPercent = (decimal)response.Statistics.LGD_ZeroLossCount / lgdValues.Count * 100;
                
                decimal probabilityOfDefault = response.Statistics.ProbabilityOfDefault / 100m;
                response.Statistics.ExpectedLoss = probabilityOfDefault * response.Statistics.AverageLGD;
                
                Console.WriteLine($"[EXPECTED LOSS] PD: {response.Statistics.ProbabilityOfDefault:F2}%");
                Console.WriteLine($"[LGD DISTRIBUTION] Mean: €{response.Statistics.AverageLGD:N0}, Median: €{response.Statistics.MedianLGD:N0}");
                Console.WriteLine($"[LGD PERCENTILES] P10: €{response.Statistics.LGD_P10:N0}, P90: €{response.Statistics.LGD_P90:N0}, P95: €{response.Statistics.LGD_P95:N0}, P99: €{response.Statistics.LGD_P99:N0}");
                Console.WriteLine($"[LGD RANGE] Min: €{response.Statistics.LGD_Min:N0}, Max: €{response.Statistics.LGD_Max:N0}");
                Console.WriteLine($"[LGD COVERAGE] {response.Statistics.LGD_ZeroLossCount}/{lgdValues.Count} defaults ({response.Statistics.LGD_ZeroLossPercent:F1}%) have zero loss (collateral fully covers)");
                Console.WriteLine($"[EXPECTED LOSS] EL = PD × Mean(LGD) = {response.Statistics.ProbabilityOfDefault:F2}% × €{response.Statistics.AverageLGD:N0} = €{response.Statistics.ExpectedLoss:N0}");
            }
            else
            {
                response.Statistics.AverageLGD = 0;
                response.Statistics.MedianLGD = 0;
                response.Statistics.ExpectedLoss = 0;
                Console.WriteLine($"[EXPECTED LOSS] No defaults with LGD, setting EL to 0");
            }
        }

        private void CalculateFinalYearStatistics(
            MonteCarloSimulationResponse response,
            List<DebtorSimulationService.SimulationPath> allSimulations)
        {
            var finalYearResults = allSimulations.Select(s => s.Years.Last()).ToList();
            
            response.Statistics.ExpectedRevenue = finalYearResults.Average(y => y.Revenue);
            response.Statistics.ExpectedEbitda = finalYearResults.Average(y => y.Ebitda);
            response.Statistics.ExpectedEquity = finalYearResults.Average(y => y.Equity);
            response.Statistics.MedianEquity = CalculatePercentile(
                finalYearResults.Select(y => y.Equity).OrderBy(e => e).ToList(), 50);
            response.Statistics.MedianRevenue = CalculatePercentile(
                finalYearResults.Select(y => y.Revenue).OrderBy(r => r).ToList(), 50);
            response.Statistics.MedianEbitda = CalculatePercentile(
                finalYearResults.Select(y => y.Ebitda).OrderBy(e => e).ToList(), 50);
            response.Statistics.Percentile5Revenue = CalculatePercentile(
                finalYearResults.Select(y => y.Revenue).OrderBy(r => r).ToList(), 5);
            response.Statistics.Percentile95Revenue = CalculatePercentile(
                finalYearResults.Select(y => y.Revenue).OrderBy(r => r).ToList(), 95);
        }

        private void CalculateROI(
            MonteCarloSimulationResponse response,
            List<DebtorSimulationService.SimulationPath> allSimulations,
            List<SimulatedLoanInfo> simulatedLoans)
        {
            // Only portfolio loans (positive IDs)
            decimal nominalLoanAmount = simulatedLoans.Where(l => l.LoanId >= 0).Sum(l => l.LoanAmount);
            response.Statistics.NominalLoanAmount = nominalLoanAmount;

            // Historical interest (before simulation)
            decimal interestPaidBeforeSimulation = simulatedLoans
                .Where(l => l.LoanId >= 0)
                .SelectMany(l => l.YearlyPayments)
                .Where(yp => yp.Year < 1)
                .Sum(yp => yp.InterestExpense);
            response.Statistics.InterestPaidBeforeSimulation = interestPaidBeforeSimulation;

            // Median interest during simulation
            var cumulativeInterests = allSimulations.Select(s => s.Years.Sum(y => y.InterestExpense)).ToList();
            response.Statistics.MedianCumulativeInterestDuringSimulation = CalculatePercentile(
                cumulativeInterests.OrderBy(i => i).ToList(), 50);

            response.Statistics.MedianTotalInterest = 
                interestPaidBeforeSimulation + response.Statistics.MedianCumulativeInterestDuringSimulation;

            if (nominalLoanAmount > 0)
            {
                decimal netReturn = response.Statistics.MedianTotalInterest - response.Statistics.ExpectedLoss;
                response.Statistics.MedianROI = (netReturn / nominalLoanAmount) * 100;
                
                Console.WriteLine($"[ROI] Nominal: €{nominalLoanAmount:N2}, " +
                    $"Total Interest: €{response.Statistics.MedianTotalInterest:N2}, " +
                    $"EL: €{response.Statistics.ExpectedLoss:N2}, ROI: {response.Statistics.MedianROI:F2}%");
            }
        }

        private void AddSamplePaths(
            MonteCarloSimulationResponse response,
            List<DebtorSimulationService.SimulationPath> allSimulations)
        {
            // Convert from DebtorSimulationService format to response format
            var convertedPaths = allSimulations.Select(ConvertSimulationPath).ToList();

            // Add worst loss scenario
            var defaultsWithLoss = convertedPaths.Where(s => s.DefaultOccurred && s.LossGivenDefault > 0).ToList();
            if (defaultsWithLoss.Any())
            {
                response.SamplePaths.Add(defaultsWithLoss.OrderByDescending(s => s.LossGivenDefault).First());
            }
            else
            {
                var defaults = convertedPaths.Where(s => s.DefaultOccurred).ToList();
                if (defaults.Any())
                {
                    response.SamplePaths.Add(defaults.OrderBy(s => s.Years.Last().Equity).First());
                }
                else
                {
                    response.SamplePaths.Add(convertedPaths.OrderBy(s => s.Years.Last().Revenue).First());
                }
            }

            // Add median and best scenarios
            var sortedByFinalEquity = convertedPaths.OrderBy(s => s.Years.Last().Equity).ToList();
            response.SamplePaths.Add(sortedByFinalEquity[sortedByFinalEquity.Count / 2]); // Median
            response.SamplePaths.Add(sortedByFinalEquity.Last()); // Best
        }

        #endregion

        #region Portfolio Response Building

        private PortfolioMonteCarloResponse BuildPortfolioResponse(
            List<(int debtorId, string debtorName, MonteCarloSimulationRequest request, List<SimulatedLoanInfo> loans)> debtorData,
            Dictionary<int, List<DebtorSimulationService.SimulationPath>> allDebtorResults,
            int numberOfSimulations,
            int simulationYears)
        {
            var response = new PortfolioMonteCarloResponse
            {
                DebtorIds = debtorData.Select(d => d.debtorId).ToList(),
                TotalSimulations = numberOfSimulations,
                SimulationYears = simulationYears
            };

            // Calculate per-debtor summaries
            foreach (var (debtorId, debtorName, request, loans) in debtorData)
            {
                var debtorSims = allDebtorResults[debtorId];
                int defaultCount = debtorSims.Count(s => s.DefaultOccurred);
                decimal pd = (decimal)defaultCount / numberOfSimulations * 100;
                decimal totalLoanAmount = loans.Sum(l => l.LoanAmount);

                // Calculate expected loss for this debtor
                var defaultedPaths = debtorSims.Where(s => s.DefaultOccurred && s.LossGivenDefault.HasValue).ToList();
                
                Console.WriteLine($"[PORTFOLIO DEBUG] {debtorName}: Total defaults: {defaultCount}, " +
                    $"Defaults with LGD: {defaultedPaths.Count}");
                
                if (defaultCount > 0 && !defaultedPaths.Any())
                {
                    Console.WriteLine($"[PORTFOLIO WARNING] {debtorName} has {defaultCount} defaults but none have LGD values!");
                }
                
                // Calculate comprehensive LGD statistics
                decimal averageLGD = 0;
                decimal medianLGD = 0;
                decimal lgd_P10 = 0, lgd_P90 = 0, lgd_P95 = 0, lgd_P99 = 0;
                decimal lgd_Min = 0, lgd_Max = 0;
                int lgd_ZeroCount = 0;
                decimal lgd_ZeroPercent = 0;
                
                if (defaultedPaths.Any())
                {
                    var lgdValues = defaultedPaths.Select(s => s.LossGivenDefault!.Value).OrderBy(lgd => lgd).ToList();
                    
                    averageLGD = lgdValues.Average();
                    medianLGD = CalculatePercentile(lgdValues, 50);
                    lgd_P10 = CalculatePercentile(lgdValues, 10);
                    lgd_P90 = CalculatePercentile(lgdValues, 90);
                    lgd_P95 = CalculatePercentile(lgdValues, 95);
                    lgd_P99 = CalculatePercentile(lgdValues, 99);
                    lgd_Min = lgdValues.First();
                    lgd_Max = lgdValues.Last();
                    lgd_ZeroCount = lgdValues.Count(lgd => lgd == 0);
                    lgd_ZeroPercent = (decimal)lgd_ZeroCount / lgdValues.Count * 100;
                    
                    // Enhanced console logging for portfolio
                    Console.WriteLine($"[PORTFOLIO LGD] {debtorName}:");
                    Console.WriteLine($"  PD: {pd:F2}%");
                    Console.WriteLine($"  LGD Distribution - Mean: €{averageLGD:N0}, Median: €{medianLGD:N0}");
                    Console.WriteLine($"  Percentiles - P10: €{lgd_P10:N0}, P90: €{lgd_P90:N0}, P95: €{lgd_P95:N0}, P99: €{lgd_P99:N0}");
                    Console.WriteLine($"  Range - Min: €{lgd_Min:N0}, Max: €{lgd_Max:N0}");
                    Console.WriteLine($"  Coverage - {lgd_ZeroCount}/{lgdValues.Count} defaults ({lgd_ZeroPercent:F1}%) have zero loss");
                }
                
                decimal expectedLoss = (pd / 100m) * averageLGD;
                
                Console.WriteLine($"  Expected Loss: PD ({pd:F2}%) × Mean LGD (€{averageLGD:N0}) = €{expectedLoss:N0}");

                // Determine primary property type
                string primaryPropertyType = DeterminePrimaryPropertyType(loans);

                var summary = new DebtorSimulationSummary
                {
                    DebtorId = debtorId,
                    DebtorName = debtorName,
                    LoanAmount = totalLoanAmount,
                    ProbabilityOfDefault = pd,
                    ExpectedLoss = expectedLoss,
                    ExpectedLossPercentage = totalLoanAmount > 0 ? (expectedLoss / totalLoanAmount) * 100 : 0,
                    SectorWeights = request.SectorWeights ?? new Dictionary<Sector, decimal>(),
                    PrimaryPropertyType = primaryPropertyType,
                    AverageLGD = averageLGD,
                    MedianLGD = medianLGD,
                    LGD_P10 = lgd_P10,
                    LGD_P90 = lgd_P90,
                    LGD_P95 = lgd_P95,
                    LGD_P99 = lgd_P99,
                    LGD_Min = lgd_Min,
                    LGD_Max = lgd_Max,
                    LGD_ZeroLossCount = lgd_ZeroCount,
                    LGD_ZeroLossPercent = lgd_ZeroPercent
                };

                response.DebtorResults.Add(summary);
                response.PortfolioStats.TotalLoanAmount += totalLoanAmount;
            }

            // Calculate portfolio-level metrics
            response.PortfolioStats.AveragePortfolioPD = response.DebtorResults.Average(d => d.ProbabilityOfDefault);
            response.PortfolioStats.PortfolioExpectedLoss = response.DebtorResults.Sum(d => d.ExpectedLoss);
            
            if (response.PortfolioStats.TotalLoanAmount > 0)
            {
                response.PortfolioStats.PortfolioExpectedLossPercentage = 
                    (response.PortfolioStats.PortfolioExpectedLoss / response.PortfolioStats.TotalLoanAmount) * 100;
            }

            // Calculate property type concentration
            foreach (var debtor in response.DebtorResults)
            {
                if (!string.IsNullOrEmpty(debtor.PrimaryPropertyType))
                {
                    decimal weight = debtor.LoanAmount / response.PortfolioStats.TotalLoanAmount;
                    
                    if (response.PortfolioStats.PropertyTypeConcentration.ContainsKey(debtor.PrimaryPropertyType))
                    {
                        response.PortfolioStats.PropertyTypeConcentration[debtor.PrimaryPropertyType] += weight;
                    }
                    else
                    {
                        response.PortfolioStats.PropertyTypeConcentration[debtor.PrimaryPropertyType] = weight;
                    }
                }
            }

            // Calculate yearly statistics across portfolio
            CalculatePortfolioYearlyStatistics(response, allDebtorResults);
            
            // Generate histogram bins
            GenerateHistogramBins(response, allDebtorResults);

            return response;
        }

        private void CalculatePortfolioYearlyStatistics(
            PortfolioMonteCarloResponse response,
            Dictionary<int, List<DebtorSimulationService.SimulationPath>> allDebtorResults)
        {
            for (int year = 0; year <= response.SimulationYears; year++)
            {
                var yearStat = new PortfolioYearlyStatistics { Year = year };
                
                var revenuesThisYear = new List<decimal>();
                var ebitdasThisYear = new List<decimal>();
                var debtsThisYear = new List<decimal>();
                var equitiesThisYear = new List<decimal>();
                var interestsThisYear = new List<decimal>();
                
                for (int sim = 0; sim < response.TotalSimulations; sim++)
                {
                    decimal simRevenue = 0;
                    decimal simEbitda = 0;
                    decimal simDebt = 0;
                    decimal simEquity = 0;
                    decimal simInterest = 0;
                    
                    foreach (var debtorId in response.DebtorIds)
                    {
                        var debtorPath = allDebtorResults[debtorId][sim];
                        if (debtorPath.Years.Count > year)
                        {
                            var yearResult = debtorPath.Years[year];
                            simRevenue += yearResult.Revenue;
                            simEbitda += yearResult.Ebitda;
                            simDebt += yearResult.Debt;
                            simEquity += yearResult.Equity;
                            simInterest += yearResult.InterestExpense;
                        }
                    }
                    
                    revenuesThisYear.Add(simRevenue);
                    ebitdasThisYear.Add(simEbitda);
                    debtsThisYear.Add(simDebt);
                    equitiesThisYear.Add(simEquity);
                    interestsThisYear.Add(simInterest);
                }
                
                yearStat.TotalRevenue = revenuesThisYear.Average();
                yearStat.TotalEbitda = ebitdasThisYear.Average();
                yearStat.TotalDebt = debtsThisYear.Average();
                yearStat.TotalEquity = equitiesThisYear.Average();
                yearStat.TotalInterestExpense = interestsThisYear.Average();
                
                yearStat.MedianTotalRevenue = CalculatePercentile(revenuesThisYear.OrderBy(r => r).ToList(), 50);
                yearStat.Percentile10Revenue = CalculatePercentile(revenuesThisYear.OrderBy(r => r).ToList(), 10);
                yearStat.Percentile90Revenue = CalculatePercentile(revenuesThisYear.OrderBy(r => r).ToList(), 90);
                
                response.YearlyResults.Add(yearStat);
            }
        }
        
        private void GenerateHistogramBins(
            PortfolioMonteCarloResponse response,
            Dictionary<int, List<DebtorSimulationService.SimulationPath>> allDebtorResults)
        {
            // Collect raw data for each simulation
            var defaultCounts = new List<int>();
            var totalLosses = new List<decimal>();
            
            for (int sim = 0; sim < response.TotalSimulations; sim++)
            {
                int defaultsInScenario = 0;
                decimal totalLossInScenario = 0;
                
                foreach (var debtorId in response.DebtorIds)
                {
                    var debtorPath = allDebtorResults[debtorId][sim];
                    
                    if (debtorPath.DefaultOccurred)
                    {
                        defaultsInScenario++;
                        if (debtorPath.LossGivenDefault.HasValue)
                        {
                            totalLossInScenario += debtorPath.LossGivenDefault.Value;
                        }
                    }
                }
                
                defaultCounts.Add(defaultsInScenario);
                totalLosses.Add(totalLossInScenario);
            }
            
            // Generate default count histogram bins
            response.PortfolioStats.Histograms.DefaultCountBins = GenerateDefaultCountBins(defaultCounts);
            
            // Generate total loss histogram bins
            response.PortfolioStats.Histograms.TotalLossBins = GenerateTotalLossBins(totalLosses);
            
            Console.WriteLine($"[PORTFOLIO HISTOGRAM] Generated {response.PortfolioStats.Histograms.DefaultCountBins.Count} default bins, " +
                $"{response.PortfolioStats.Histograms.TotalLossBins.Count} loss bins");
        }
        
        private List<HistogramBin> GenerateDefaultCountBins(List<int> values)
        {
            if (!values.Any()) return new List<HistogramBin>();
            
            var minValue = values.Min();
            var maxValue = values.Max();
            
            // For discrete values like default counts, create one bin per value
            var bins = new Dictionary<int, int>();
            
            for (int i = minValue; i <= maxValue; i++)
            {
                bins[i] = 0;
            }
            
            foreach (var value in values)
            {
                bins[value]++;
            }
            
            return bins.Select(kvp => new HistogramBin
            {
                Label = kvp.Key.ToString(),
                Count = kvp.Value
            }).ToList();
        }
        
        private List<HistogramBin> GenerateTotalLossBins(List<decimal> values)
        {
            if (!values.Any()) return new List<HistogramBin>();
            
            var minValue = values.Min();
            var maxValue = values.Max();
            
            Console.WriteLine($"[LOSS BINS DEBUG] Total scenarios: {values.Count}");
            Console.WriteLine($"[LOSS BINS DEBUG] Min loss: €{minValue:N2}, Max loss: €{maxValue:N2}");
            Console.WriteLine($"[LOSS BINS DEBUG] Non-zero losses: {values.Count(v => v > 0)}");
            
            // Handle edge case where all values are the same
            if (minValue == maxValue)
            {
                Console.WriteLine($"[LOSS BINS DEBUG] All values are the same: €{minValue:N2}");
                return new List<HistogramBin>
                {
                    new HistogramBin
                    {
                        Label = $"€{Math.Round(minValue / 1000)}k",
                        Count = values.Count
                    }
                };
            }
            
            // Create 20 bins with equal width from min to max
            const int numBins = 20;
            var binWidth = (maxValue - minValue) / numBins;
            var bins = new int[numBins];
            
            Console.WriteLine($"[LOSS BINS DEBUG] Creating {numBins} equal-width bins from €{minValue:N2} to €{maxValue:N2}");
            Console.WriteLine($"[LOSS BINS DEBUG] Bin width: €{binWidth:N2}");
            
            // Count values in each bin
            foreach (var value in values)
            {
                int binIndex = (int)Math.Floor((value - minValue) / binWidth);
                if (binIndex >= numBins) binIndex = numBins - 1;
                if (binIndex < 0) binIndex = 0;
                bins[binIndex]++;
            }
            
            // Create histogram bins with labels showing actual loss ranges
            var result = new List<HistogramBin>();
            for (int i = 0; i < numBins; i++)
            {
                var binStart = minValue + i * binWidth;
                var binEnd = binStart + binWidth;
                
                // Format label based on values
                string label;
                if (binEnd - binStart < 1000)
                {
                    // For small ranges, show decimal places
                    label = $"€{Math.Round(binStart / 1000, 1)}k-€{Math.Round(binEnd / 1000, 1)}k";
                }
                else
                {
                    label = $"€{Math.Round(binStart / 1000)}k-€{Math.Round(binEnd / 1000)}k";
                }
                
                result.Add(new HistogramBin
                {
                    Label = label,
                    Count = bins[i]
                });
                
                if (bins[i] > 0)
                {
                    Console.WriteLine($"[LOSS BINS DEBUG] Bin {i}: {label} has {bins[i]} scenarios");
                }
            }
            
            Console.WriteLine($"[LOSS BINS DEBUG] Non-empty bins: {result.Count(b => b.Count > 0)}");
            Console.WriteLine($"[LOSS BINS DEBUG] First bin: {result[0].Label} = {result[0].Count} scenarios");
            Console.WriteLine($"[LOSS BINS DEBUG] Last bin: {result[numBins-1].Label} = {result[numBins-1].Count} scenarios");
            
            return result;
        }

        #endregion

        #region Helper Methods

        private List<DebtorSimulationService.SimulatedLoanInfo> ConvertToDebtorServiceLoans(
            List<SimulatedLoanInfo> loans)
        {
            return loans.Select(l => new DebtorSimulationService.SimulatedLoanInfo
            {
                LoanId = l.LoanId,
                LoanAmount = l.LoanAmount,
                InterestRate = l.InterestRate,
                TenorMonths = l.TenorMonths,
                RedemptionSchedule = l.RedemptionSchedule,
                CollateralValue = l.CollateralValue,
                LiquidityHaircut = l.LiquidityHaircut,
                Subordination = l.Subordination,
                CollateralPropertyType = l.CollateralPropertyType ?? "",
                YearlyPayments = l.YearlyPayments.Select(yp => new DebtorSimulationService.YearlyPaymentInfo
                {
                    Year = yp.Year,
                    InterestExpense = yp.InterestExpense,
                    RedemptionAmount = yp.RedemptionAmount,
                    OutstandingBalance = yp.OutstandingBalance
                }).ToList()
            }).ToList();
        }

        private SimulationPath ConvertSimulationPath(DebtorSimulationService.SimulationPath source)
        {
            return new SimulationPath
            {
                SimulationNumber = source.SimulationNumber,
                Years = source.Years.Select(y => new YearResult
                {
                    Year = y.Year,
                    Revenue = y.Revenue,
                    OperatingCosts = y.OperatingCosts,
                    Ebitda = y.Ebitda,
                    EbitdaMargin = y.EbitdaMargin,
                    InterestExpense = y.InterestExpense,
                    CorporateTax = y.CorporateTax,
                    RedemptionAmount = y.RedemptionAmount,
                    NetIncome = y.NetIncome,
                    Assets = y.Assets,
                    Debt = y.Debt,
                    Equity = y.Equity,
                    InterestCoverage = y.InterestCoverage,
                    CanPayInterest = y.CanPayInterest,
                    LiquidAssets = y.LiquidAssets,
                    LiquidAssetsChange = y.LiquidAssetsChange
                }).ToList(),
                DefaultOccurred = source.DefaultOccurred,
                DefaultYear = source.DefaultYear,
                LossGivenDefault = source.LossGivenDefault,
                CollateralValueAtDefault = source.CollateralValueAtDefault
            };
        }

        private Dictionary<string, decimal> BuildCollateralVolatilities(
            List<string?> collateralTypes,
            decimal defaultVolatility)
        {
            var volatilities = new Dictionary<string, decimal>();
            foreach (var propertyType in collateralTypes)
            {
                if (propertyType != null)
                {
                    volatilities[propertyType] = defaultVolatility;
                }
            }
            return volatilities;
        }

        private Dictionary<(Sector, string), decimal> AggregateCollateralCorrelations(
            List<(int debtorId, string debtorName, MonteCarloSimulationRequest request, List<SimulatedLoanInfo> loans)> debtorData)
        {
            var aggregated = new Dictionary<(Sector, string), decimal>();
            
            foreach (var (_, _, request, _) in debtorData)
            {
                if (request.SectorCollateralCorrelations != null)
                {
                    foreach (var kvp in request.SectorCollateralCorrelations)
                    {
                        if (!aggregated.ContainsKey(kvp.Key))
                        {
                            aggregated[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            
            return aggregated;
        }

        private Dictionary<string, decimal> AggregateCollateralVolatilities(
            List<(int debtorId, string debtorName, MonteCarloSimulationRequest request, List<SimulatedLoanInfo> loans)> debtorData,
            List<string> allCollateralTypes)
        {
            var volatilities = new Dictionary<string, decimal>();
            
            foreach (var propertyType in allCollateralTypes)
            {
                // Use first debtor's collateral volatility as default
                var firstRequest = debtorData.FirstOrDefault().request;
                volatilities[propertyType] = firstRequest?.CollateralVolatility ?? 0.15m;
            }
            
            return volatilities;
        }

        private string DeterminePrimaryPropertyType(List<SimulatedLoanInfo> loans)
        {
            var propertyTypes = loans
                .Where(l => !string.IsNullOrEmpty(l.CollateralPropertyType))
                .GroupBy(l => l.CollateralPropertyType)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            
            return propertyTypes?.Key ?? "Unknown";
        }

        private decimal CalculatePercentile(List<decimal> sortedValues, int percentile)
        {
            if (!sortedValues.Any()) return 0;
            if (sortedValues.Count == 1) return sortedValues[0];
            
            double rank = (percentile / 100.0) * (sortedValues.Count - 1);
            int lowerIndex = (int)Math.Floor(rank);
            int upperIndex = (int)Math.Ceiling(rank);
            
            if (lowerIndex == upperIndex)
            {
                return sortedValues[lowerIndex];
            }
            
            decimal lowerValue = sortedValues[lowerIndex];
            decimal upperValue = sortedValues[upperIndex];
            double fraction = rank - lowerIndex;
            
            return lowerValue + (decimal)fraction * (upperValue - lowerValue);
        }

        #endregion
    }
}
