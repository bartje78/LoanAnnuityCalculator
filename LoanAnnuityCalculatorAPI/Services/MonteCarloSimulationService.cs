using LoanAnnuityCalculatorAPI.Models.DTOs;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class MonteCarloSimulationService
    {
        private readonly Random _random;

        public MonteCarloSimulationService()
        {
            _random = new Random();
        }

        /// <summary>
        /// Run Monte Carlo simulation for debtor P&L and balance sheet
        /// </summary>
        public MonteCarloSimulationResponse RunSimulation(MonteCarloSimulationRequest request, List<SimulatedLoanInfo> simulatedLoans)
        {
            var response = new MonteCarloSimulationResponse
            {
                DebtorId = request.DebtorId,
                TotalSimulations = request.NumberOfSimulations,
                SimulationYears = request.SimulationYears,
                SimulatedLoans = simulatedLoans
            };

            // Store all simulation results for statistics
            var allSimulations = new List<SimulationPath>();
            int defaultCount = 0;

            // Console.WriteLine($"[MONTE CARLO] Starting {request.NumberOfSimulations} simulations...");

            // Run simulations
            for (int sim = 0; sim < request.NumberOfSimulations; sim++)
            {
                var path = RunSingleSimulation(request, simulatedLoans, sim + 1);
                allSimulations.Add(path);
                
                if (path.DefaultOccurred)
                    defaultCount++;
            }

            // Console.WriteLine($"[MONTE CARLO] Completed {allSimulations.Count} simulations. Defaults: {defaultCount}");

            // Calculate statistics
            response.Statistics.ProbabilityOfDefault = (decimal)defaultCount / request.NumberOfSimulations * 100;
            
            // Add Year 0 statistics (initial state before simulation starts)
            // All simulations have the same initial values, so we can use the request values directly
            
            // Calculate initial EBITDA from costs if provided, otherwise use margin
            decimal initialEbitda = request.InitialOperatingCosts > 0
                ? request.InitialRevenue - request.InitialOperatingCosts
                : request.InitialRevenue * request.InitialEbitdaMargin;
            
            var year0Stats = new YearlyStatistics
            {
                Year = 0,
                AveragRevenue = request.InitialRevenue,
                AverageEbitda = initialEbitda,
                AverageInterestExpense = 0, // Not yet calculated for year 0
                AverageInterestCoverage = 0,
                ProbabilityCannotPayInterest = 0,
                CumulativeDefaultProbability = 0,
                AverageEquity = request.InitialEquity,
                AverageDebt = request.InitialDebt + request.LoanAmount, // Base liabilities + our loans
                AverageNetProfit = 0,
                AverageRedemptionAmount = 0,
                AverageLiquidAssets = request.InitialLiquidAssets,
                AverageLiquidAssetsChange = 0,
                ProbabilityNegativeCashFlow = 0,
                
                // All simulations start with same values, so percentiles are all the same
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
                
                MedianDebt = request.InitialDebt + request.LoanAmount, // Base liabilities + our loans
                Percentile10Debt = request.InitialDebt + request.LoanAmount,
                Percentile90Debt = request.InitialDebt + request.LoanAmount,
                
                MedianLiquidAssets = request.InitialLiquidAssets,
                Percentile10LiquidAssets = request.InitialLiquidAssets,
                Percentile90LiquidAssets = request.InitialLiquidAssets
            };
            response.YearlyResults.Add(year0Stats);
            
            // Calculate yearly statistics
            for (int year = 1; year <= request.SimulationYears; year++)
            {
                var yearResults = allSimulations
                    .Where(s => s.Years.Any(y => y.Year == year))
                    .Select(s => s.Years.First(y => y.Year == year))
                    .ToList();

                // Skip this year if no simulations reached it (all defaulted earlier)
                if (yearResults.Count == 0)
                {
            // Console.WriteLine($"[STATS] Year {year}: No simulations reached this year (all defaulted earlier)");
                    continue;
                }

                // Count how many simulations have defaulted by this year (cumulative)
                int defaultsByThisYear = allSimulations.Count(s => s.DefaultOccurred && s.DefaultYear.HasValue && s.DefaultYear.Value <= year);

                // Pre-calculate sorted lists for percentiles to avoid multiple sorts
                var sortedRevenue = yearResults.Select(y => y.Revenue).OrderBy(r => r).ToList();
                var sortedEbitda = yearResults.Select(y => y.Ebitda).OrderBy(e => e).ToList();
                var sortedEquity = yearResults.Select(y => y.Equity).OrderBy(e => e).ToList();
                var sortedDebt = yearResults.Select(y => y.Debt).OrderBy(d => d).ToList();
                var sortedLiquidAssets = yearResults.Select(y => y.LiquidAssets).OrderBy(la => la).ToList();
                var sortedInterestExpense = yearResults.Select(y => y.InterestExpense).OrderBy(i => i).ToList();
                var sortedNetIncome = yearResults.Select(y => y.NetIncome).OrderBy(n => n).ToList();
                var sortedRedemption = yearResults.Select(y => y.RedemptionAmount).OrderBy(r => r).ToList();
                var sortedLiquidAssetsChange = yearResults.Select(y => y.LiquidAssetsChange).OrderBy(c => c).ToList();
                var sortedInterestCoverage = yearResults.Select(y => y.InterestCoverage).OrderBy(ic => ic).ToList();

                var yearlyStats = new YearlyStatistics
                {
                    Year = year,
                    AveragRevenue = yearResults.Average(y => y.Revenue),
                    AverageEbitda = yearResults.Average(y => y.Ebitda),
                    AverageInterestExpense = yearResults.Average(y => y.InterestExpense),
                    AverageInterestCoverage = yearResults.Average(y => y.InterestCoverage),
                    ProbabilityCannotPayInterest = (decimal)yearResults.Count(y => !y.CanPayInterest) / yearResults.Count * 100,
                    CumulativeDefaultProbability = (decimal)defaultsByThisYear / request.NumberOfSimulations * 100,
                    AverageEquity = yearResults.Average(y => y.Equity),
                    AverageDebt = yearResults.Average(y => y.Debt),
                    
                    // Cash flow breakdown
                    AverageNetProfit = yearResults.Average(y => y.NetIncome),
                    AverageRedemptionAmount = yearResults.Average(y => y.RedemptionAmount),
                    AverageLiquidAssets = yearResults.Average(y => y.LiquidAssets),
                    AverageLiquidAssetsChange = yearResults.Average(y => y.LiquidAssetsChange),
                    ProbabilityNegativeCashFlow = (decimal)yearResults.Count(y => y.LiquidAssetsChange < 0) / yearResults.Count * 100,
                    
                    // Median values for table display
                    MedianEbitda = CalculatePercentile(sortedEbitda, 50),
                    MedianInterestExpense = CalculatePercentile(sortedInterestExpense, 50),
                    MedianNetProfit = CalculatePercentile(sortedNetIncome, 50),
                    MedianRedemptionAmount = CalculatePercentile(sortedRedemption, 50),
                    MedianLiquidAssets = CalculatePercentile(sortedLiquidAssets, 50),
                    MedianLiquidAssetsChange = CalculatePercentile(sortedLiquidAssetsChange, 50),
                    MedianInterestCoverage = CalculatePercentile(sortedInterestCoverage, 50),
                    
                    // Revenue percentiles
                    MedianRevenue = CalculatePercentile(sortedRevenue, 50),
                    Percentile10Revenue = CalculatePercentile(sortedRevenue, 10),
                    Percentile90Revenue = CalculatePercentile(sortedRevenue, 90),
                    Percentile5Revenue = CalculatePercentile(sortedRevenue, 5),
                    Percentile95Revenue = CalculatePercentile(sortedRevenue, 95),
                    
                    // EBITDA percentiles
                    Percentile10Ebitda = CalculatePercentile(sortedEbitda, 10),
                    Percentile90Ebitda = CalculatePercentile(sortedEbitda, 90),
                    
                    // Equity percentiles
                    MedianEquity = CalculatePercentile(sortedEquity, 50),
                    Percentile10Equity = CalculatePercentile(sortedEquity, 10),
                    Percentile90Equity = CalculatePercentile(sortedEquity, 90),
                    
                    // Debt percentiles
                    MedianDebt = CalculatePercentile(sortedDebt, 50),
                    Percentile10Debt = CalculatePercentile(sortedDebt, 10),
                    Percentile90Debt = CalculatePercentile(sortedDebt, 90),
                    
                    // Liquid Assets percentiles
                    Percentile10LiquidAssets = CalculatePercentile(sortedLiquidAssets, 10),
                    Percentile90LiquidAssets = CalculatePercentile(sortedLiquidAssets, 90)
                };

                response.YearlyResults.Add(yearlyStats);
            }

            // Calculate overall statistics
            var allFinalYearResults = allSimulations.Select(s => s.Years.Last()).ToList();
            response.Statistics.ExpectedRevenue = allFinalYearResults.Average(y => y.Revenue);
            response.Statistics.ExpectedEbitda = allFinalYearResults.Average(y => y.Ebitda);
            response.Statistics.ExpectedEquity = allFinalYearResults.Average(y => y.Equity);
            
            // DEBUG: Log equity statistics
            var sortedFinalEquity = allFinalYearResults.Select(y => y.Equity).OrderBy(e => e).ToList();
            var medianEquity = CalculatePercentile(sortedFinalEquity, 50);
            response.Statistics.MedianEquity = medianEquity; // Add to response
            
            // Console.WriteLine($"[EQUITY DEBUG] Final Year Statistics:");
            // Console.WriteLine($"  Expected (Average) Equity: {response.Statistics.ExpectedEquity:N2}");
            // Console.WriteLine($"  Median Equity: {medianEquity:N2}");
            // Console.WriteLine($"  Min Equity: {sortedFinalEquity.First():N2}");
            // Console.WriteLine($"  Max Equity: {sortedFinalEquity.Last():N2}");
            // Console.WriteLine($"  10th Percentile: {CalculatePercentile(sortedFinalEquity, 10):N2}");
            // Console.WriteLine($"  90th Percentile: {CalculatePercentile(sortedFinalEquity, 90):N2}");
            // Console.WriteLine($"  Defaulted simulations: {allSimulations.Count(s => s.DefaultOccurred)}");
            
            // Calculate medians and percentiles for final year
            response.Statistics.MedianRevenue = CalculatePercentile(allFinalYearResults.Select(y => y.Revenue).OrderBy(r => r).ToList(), 50);
            response.Statistics.MedianEbitda = CalculatePercentile(allFinalYearResults.Select(y => y.Ebitda).OrderBy(e => e).ToList(), 50);
            response.Statistics.Percentile5Revenue = CalculatePercentile(allFinalYearResults.Select(y => y.Revenue).OrderBy(r => r).ToList(), 5);
            response.Statistics.Percentile95Revenue = CalculatePercentile(allFinalYearResults.Select(y => y.Revenue).OrderBy(r => r).ToList(), 95);
            
            // Calculate LGD statistics for defaulted simulations
            var defaultedPaths = allSimulations.Where(s => s.DefaultOccurred && s.LossGivenDefault.HasValue).ToList();
            // Console.WriteLine($"[LGD STATS] Total defaults with LGD: {defaultedPaths.Count} out of {allSimulations.Count} simulations");
            
            if (defaultedPaths.Any())
            {
                response.Statistics.AverageLGD = defaultedPaths.Average(s => s.LossGivenDefault!.Value);
                var sortedLGD = defaultedPaths.Select(s => s.LossGivenDefault!.Value).OrderBy(lgd => lgd).ToList();
                response.Statistics.MedianLGD = CalculatePercentile(sortedLGD, 50);
                
                // Expected Loss = PD × Average LGD Amount
                // Since LossGivenDefault already represents the actual loss in EUR, 
                // we just multiply by probability of default to get expected loss across ALL simulations
                decimal probabilityOfDefault = response.Statistics.ProbabilityOfDefault / 100m; // Convert % to decimal
                response.Statistics.ExpectedLoss = probabilityOfDefault * response.Statistics.AverageLGD;
                
                // Calculate average LGD percentage (loss as % of outstanding debt at default)
                decimal averageLGDRate = defaultedPaths.Average(s => s.LGDPercentage ?? 0);
                response.Statistics.AverageLGDPercentage = averageLGDRate; // Already in percentage form
                
                // Additional stats for debugging
                decimal averageExposureAtDefault = defaultedPaths.Average(s => s.OutstandingDebtAtDefault ?? 0);
                
            // Console.WriteLine($"[LGD STATS] Average LGD: {response.Statistics.AverageLGD:N2}");
            // Console.WriteLine($"[LGD STATS] Median LGD: {response.Statistics.MedianLGD:N2}");
            // Console.WriteLine($"[LGD STATS] Expected Loss: {response.Statistics.ExpectedLoss:N2}");
            // Console.WriteLine($"[LGD STATS] PD: {probabilityOfDefault:P2}, Avg LGD Amount: {response.Statistics.AverageLGD:N2}");
            // Console.WriteLine($"[LGD STATS] Avg LGD%: {averageLGDRate:N2}%, Avg EAD: {averageExposureAtDefault:N2}");
            }
            else
            {
                // No defaults occurred - explicitly set to zero
                response.Statistics.AverageLGD = 0;
                response.Statistics.MedianLGD = 0;
                response.Statistics.ExpectedLoss = 0;
            // Console.WriteLine($"[LGD STATS] No defaults with LGD occurred");
            }

            // Calculate ROI statistics
            // 1. Get nominal loan amount (exclude external loans with negative LoanId)
            decimal nominalLoanAmount = simulatedLoans
                .Where(loan => loan.LoanId >= 0) // Only portfolio loans (positive IDs)
                .Sum(loan => loan.LoanAmount);
            response.Statistics.NominalLoanAmount = nominalLoanAmount;
            
            // 2. Calculate interest paid BEFORE simulation (from loan inception to simulation start)
            // Sum all interest from year 0 and earlier (historical period before simulation)
            decimal interestPaidBeforeSimulation = 0;
            foreach (var loan in simulatedLoans.Where(l => l.LoanId >= 0)) // Only portfolio loans
            {
                // Sum interest from all years before simulation starts (year < 1)
                interestPaidBeforeSimulation += loan.YearlyPayments
                    .Where(yp => yp.Year < 1)
                    .Sum(yp => yp.InterestExpense);
            }
            response.Statistics.InterestPaidBeforeSimulation = interestPaidBeforeSimulation;
            
            // 3. Calculate median cumulative interest during simulation
            var sortedCumulativeInterest = allSimulations
                .Select(s => s.CumulativeInterestPaid)
                .OrderBy(i => i)
                .ToList();
            response.Statistics.MedianCumulativeInterestDuringSimulation = CalculatePercentile(sortedCumulativeInterest, 50);
            
            // 4. Calculate median total interest (before + during)
            response.Statistics.MedianTotalInterest = interestPaidBeforeSimulation + response.Statistics.MedianCumulativeInterestDuringSimulation;
            
            // 5. Calculate ROI = (Total Interest - Expected Loss) / Nominal Amount
            if (nominalLoanAmount > 0)
            {
                decimal netReturn = response.Statistics.MedianTotalInterest - response.Statistics.ExpectedLoss;
                response.Statistics.MedianROI = (netReturn / nominalLoanAmount) * 100; // Express as percentage
                
            // Console.WriteLine($"[ROI STATS] Nominal Loan Amount: {nominalLoanAmount:N2}");
            // Console.WriteLine($"[ROI STATS] Interest Before Simulation: {interestPaidBeforeSimulation:N2}");
            // Console.WriteLine($"[ROI STATS] Median Interest During Simulation: {response.Statistics.MedianCumulativeInterestDuringSimulation:N2}");
            // Console.WriteLine($"[ROI STATS] Median Total Interest: {response.Statistics.MedianTotalInterest:N2}");
            // Console.WriteLine($"[ROI STATS] Expected Loss: {response.Statistics.ExpectedLoss:N2}");
            // Console.WriteLine($"[ROI STATS] Net Return: {netReturn:N2}");
            // Console.WriteLine($"[ROI STATS] Median ROI: {response.Statistics.MedianROI:N2}%");
            }
            else
            {
                response.Statistics.MedianROI = 0;
            // Console.WriteLine($"[ROI STATS] No portfolio loans to calculate ROI");
            }

            // Store sample paths for visualization
            // Strategy: Prioritize by loan loss (LGD), then by equity performance
            
            // Add worst case: Highest LGD if exists, otherwise worst equity scenario
            var defaultsWithLoss = allSimulations.Where(s => s.DefaultOccurred && s.LossGivenDefault.HasValue && s.LossGivenDefault.Value > 0).ToList();
            if (defaultsWithLoss.Any())
            {
                // Pick scenario with HIGHEST loan loss (worst for lender)
                var worstLoss = defaultsWithLoss.OrderByDescending(s => s.LossGivenDefault!.Value).First();
                response.SamplePaths.Add(worstLoss);
            // Console.WriteLine($"[SAMPLE PATHS] Added worst loss: Sim #{worstLoss.SimulationNumber}, " +
            //         $"DefaultYear={worstLoss.DefaultYear}, LGD={worstLoss.LossGivenDefault:N2}, " +
            //         $"LGD%={worstLoss.LGDPercentage:N2}%, Collateral={worstLoss.CollateralValueAtDefault:N2}");
            }
            else
            {
                // No loan losses - pick default with lowest equity or worst revenue if no defaults
                var defaultScenarios = allSimulations.Where(s => s.DefaultOccurred).ToList();
                if (defaultScenarios.Any())
                {
                    var worstEquityDefault = defaultScenarios.OrderBy(s => s.Years.Last().Equity).First();
                    response.SamplePaths.Add(worstEquityDefault);
            // Console.WriteLine($"[SAMPLE PATHS] No loan loss - added worst equity default: Sim #{worstEquityDefault.SimulationNumber}, " +
            //             $"DefaultYear={worstEquityDefault.DefaultYear}, Equity={worstEquityDefault.Years.Last().Equity:N2}");
                }
                else
                {
                    // No defaults at all - pick worst equity scenario
                    var worstEquity = allSimulations.OrderBy(s => s.Years.Last().Equity).First();
                    response.SamplePaths.Add(worstEquity);
            // Console.WriteLine($"[SAMPLE PATHS] No defaults - added worst equity scenario: Sim #{worstEquity.SimulationNumber}, " +
            //             $"FinalEquity={worstEquity.Years.Last().Equity:N2}");
                }
            }
            
            // Add median scenario (by final equity - most representative)
            var sortedByFinalEquity = allSimulations.OrderBy(s => s.Years.Last().Equity).ToList();
            var medianScenario = sortedByFinalEquity[sortedByFinalEquity.Count / 2];
            response.SamplePaths.Add(medianScenario);
            // Console.WriteLine($"[SAMPLE PATHS] Added median equity: Sim #{medianScenario.SimulationNumber}, " +
            //     $"FinalEquity={medianScenario.Years.Last().Equity:N2}, DefaultOccurred={medianScenario.DefaultOccurred}");
            
            // Add best case: Highest equity (best performance)
            var bestScenario = sortedByFinalEquity.Last();
            response.SamplePaths.Add(bestScenario);
            // Console.WriteLine($"[SAMPLE PATHS] Added best equity: Sim #{bestScenario.SimulationNumber}, " +
            //     $"FinalEquity={bestScenario.Years.Last().Equity:N2}, DefaultOccurred={bestScenario.DefaultOccurred}");
            
            // Console.WriteLine($"[SAMPLE PATHS] Total sample paths: {response.SamplePaths.Count}");

            return response;
        }

        /// <summary>
        /// Run a single simulation path
        /// </summary>
        private SimulationPath RunSingleSimulation(MonteCarloSimulationRequest request, List<SimulatedLoanInfo> simulatedLoans, int simulationNumber)
        {
            var path = new SimulationPath { SimulationNumber = simulationNumber };

            decimal currentRevenue = request.InitialRevenue;
            decimal currentEbitdaMargin = request.InitialEbitdaMargin; // Keep for backward compatibility
            decimal currentOperatingCosts = request.InitialOperatingCosts > 0 
                ? request.InitialOperatingCosts 
                : request.InitialRevenue * (1 - request.InitialEbitdaMargin); // Derive from margin if not provided
            decimal currentEquity = request.InitialEquity;
            
            // Track cumulative interest paid during simulation for ROI calculation
            decimal cumulativeInterestPaid = 0;
            
            // InitialDebt is from the uploaded balance sheet liabilities (may not include our loans)
            // LoanAmount represents outstanding balance of loans we're tracking
            // We add them together to get the complete debt picture
            decimal currentDebt = request.InitialDebt + request.LoanAmount;
            
            // Track portfolio loans vs external first lien separately for LGD calculation
            decimal portfolioDebt = 0;
            decimal externalFirstLienDebt = 0;
            foreach (var loan in simulatedLoans)
            {
                var initialPayment = loan.YearlyPayments.FirstOrDefault();
                if (loan.LoanId < 0) // External loan (negative ID)
                {
                    externalFirstLienDebt = initialPayment?.OutstandingBalance ?? 0;
                }
                else // Portfolio loan (positive ID)
                {
                    portfolioDebt += initialPayment?.OutstandingBalance ?? 0;
                }
            }
            
            // Track current assets separately as liquid cash buffer
            // InitialLiquidAssets = current assets only (liquid, can pay debts)
            // InitialAssets = total assets including collateral (for balance sheet display)
            decimal currentAssets = request.InitialLiquidAssets > 0 ? request.InitialLiquidAssets : request.InitialAssets; 
            
            // Total assets should not include the loan amount - that shifts balance from liability to asset
            // The loan increases both assets (cash received) and liabilities (debt owed) by the same amount
            // So it's net neutral to equity: Equity = Assets - Debt should remain unchanged
            decimal totalAssets = request.InitialAssets;
            
            // Calculate fixed assets (long-term assets) - these remain constant throughout simulation
            // Fixed assets = Total assets - Current assets (liquid)
            decimal fixedAssets = totalAssets - currentAssets;
            decimal previousLiquidAssets = currentAssets; // Track for change calculation
            
            // Initialize collateral values for each loan (will evolve over time)
            var collateralValues = new Dictionary<int, decimal>();
            foreach (var loan in simulatedLoans)
            {
                collateralValues[loan.LoanId] = loan.CollateralValue;
            }

            // Log first simulation to debug
            if (simulationNumber == 1)
            {
            // Console.WriteLine($"[MONTE CARLO DEBUG] Simulation #1:");
            // Console.WriteLine($"  InitialRevenue: {request.InitialRevenue}");
            // Console.WriteLine($"  InitialOperatingCosts: {currentOperatingCosts}");
            // Console.WriteLine($"  InitialEBITDA: {request.InitialRevenue - currentOperatingCosts}");
            // Console.WriteLine($"  InitialDebt: {request.InitialDebt}");
            // Console.WriteLine($"  LoanAmount: {request.LoanAmount}");
            // Console.WriteLine($"  currentDebt: {currentDebt}");
            // Console.WriteLine($"  InterestRate: {request.InterestRate}");
            // Console.WriteLine($"  CorporateTaxRate: {request.CorporateTaxRate}");
            // Console.WriteLine($"  Current Assets (liquid): {currentAssets}");
            // Console.WriteLine($"  Fixed Assets: {fixedAssets}");
            // Console.WriteLine($"  Total Assets: {totalAssets}");
            // Console.WriteLine($"  InitialEquity: {request.InitialEquity}");
            // Console.WriteLine($"  Accounting Check: Assets - Debt = {totalAssets - currentDebt}");
            // Console.WriteLine($"  Simulating with {simulatedLoans.Count} loans");
            }

            for (int year = 1; year <= request.SimulationYears; year++)
            {
                // If already defaulted, freeze values from default year and skip calculations
                if (path.DefaultOccurred && path.Years.Any())
                {
                    // Get the last year's result (the default year) and copy it
                    var defaultYearResult = path.Years.Last();
                    
                    // Create a copy for this year with updated year number
                    var frozenYearResult = new YearResult
                    {
                        Year = year,
                        Revenue = defaultYearResult.Revenue,
                        OperatingCosts = defaultYearResult.OperatingCosts,
                        Ebitda = defaultYearResult.Ebitda,
                        EbitdaMargin = defaultYearResult.EbitdaMargin,
                        InterestExpense = defaultYearResult.InterestExpense,
                        CorporateTax = defaultYearResult.CorporateTax,
                        RedemptionAmount = defaultYearResult.RedemptionAmount,
                        NetIncome = defaultYearResult.NetIncome,
                        Assets = defaultYearResult.Assets,
                        Debt = defaultYearResult.Debt,
                        Equity = defaultYearResult.Equity,
                        InterestCoverage = defaultYearResult.InterestCoverage,
                        CanPayInterest = defaultYearResult.CanPayInterest,
                        LiquidAssets = defaultYearResult.LiquidAssets,
                        LiquidAssetsChange = defaultYearResult.LiquidAssetsChange
                    };
                    
                    path.Years.Add(frozenYearResult);
                    
                    // Log freeze for first 3 simulations
                    if (simulationNumber <= 3)
                    {
            // Console.WriteLine($"[FREEZE] Simulation #{simulationNumber}, Year {year}: Copying frozen values from default year {path.DefaultYear}");
                    }
                    
                    continue; // Skip all calculations, move to next year
                }
                
                // Normal simulation logic continues below...
                
                // Generate correlated random shocks
                var revenueShock = GenerateNormalRandom(0, (double)request.RevenueVolatility);
                var ebitdaMarginShock = GenerateNormalRandom(0, (double)request.EbitdaMarginVolatility); // Keep for backward compatibility
                
                // Generate operating cost shock (independent from revenue)
                var operatingCostVolatility = request.OperatingCostVolatility > 0 
                    ? request.OperatingCostVolatility 
                    : request.EbitdaMarginVolatility; // Fallback to margin volatility
                var operatingCostShock = GenerateNormalRandom(0, (double)operatingCostVolatility);
                
                // Generate collateral shock (correlated with revenue)
                var independentCollateralShock = GenerateNormalRandom(0, (double)request.CollateralVolatility);
                var collateralShock = (double)request.CollateralCorrelation * revenueShock + 
                                     Math.Sqrt(1 - Math.Pow((double)request.CollateralCorrelation, 2)) * independentCollateralShock;

                // Apply growth rate and shocks to revenue and costs
                // Formula: Revenue_t = Revenue_(t-1) * (1 + growth + shock)
                currentRevenue = currentRevenue * (1 + request.RevenueGrowthRate + (decimal)revenueShock);
                
                // Operating costs grow independently (can grow faster than revenue = margin compression)
                currentOperatingCosts = currentOperatingCosts * (1 + request.OperatingCostGrowthRate + (decimal)operatingCostShock);
                
                // Keep margin for backward compatibility (calculated from actual values)
                currentEbitdaMargin = currentRevenue > 0 ? Math.Max(0, (currentRevenue - currentOperatingCosts) / currentRevenue) : 0;
                
                // Update collateral values with expected return and shock
                foreach (var loanId in collateralValues.Keys.ToList())
                {
                    var oldValue = collateralValues[loanId];
                    var newValue = oldValue * (1 + request.CollateralExpectedReturn + (decimal)collateralShock);
                    collateralValues[loanId] = Math.Max(0, newValue); // Can't go negative
                }

                // Calculate P&L items
                // EBITDA = Revenue - Operating Costs (can now be NEGATIVE!)
                decimal ebitda = currentRevenue - currentOperatingCosts;
                
                // Calculate actual loan payments for this year (interest + redemption)
                decimal totalInterestExpense = 0;
                decimal totalRedemptionAmount = 0;
                
                // Track portfolio interest separately for ROI calculation (exclude first lien)
                decimal portfolioInterestExpense = 0;
                
                foreach (var loan in simulatedLoans)
                {
                    var yearPayment = loan.YearlyPayments.FirstOrDefault(yp => yp.Year == year);
                    if (yearPayment != null)
                    {
                        totalInterestExpense += yearPayment.InterestExpense;
                        totalRedemptionAmount += yearPayment.RedemptionAmount;
                        
                        // Only track interest for portfolio loans (exclude external loans with negative IDs)
                        if (loan.LoanId >= 0)
                        {
                            portfolioInterestExpense += yearPayment.InterestExpense;
                        }
                    }
                }
                
                // Track cumulative interest for ROI calculation (portfolio loans only)
                cumulativeInterestPaid += portfolioInterestExpense;
                
                decimal totalLoanPayments = totalInterestExpense + totalRedemptionAmount;
                
                // Calculate taxable income (EBITDA - Interest, but not negative)
                decimal taxableIncome = Math.Max(0, ebitda - totalInterestExpense);
                decimal corporateTax = taxableIncome * request.CorporateTaxRate;
                
                // Net Income = EBITDA - Interest - Tax
                decimal netIncome = ebitda - totalInterestExpense - corporateTax;

                // Log year 1 calculation
                if (simulationNumber == 1 && year == 1)
                {
            // Console.WriteLine($"[MONTE CARLO DEBUG] Year 1 Calculation:");
            // Console.WriteLine($"  Revenue: {currentRevenue}");
            // Console.WriteLine($"  Operating Costs: {currentOperatingCosts}");
            // Console.WriteLine($"  EBITDA: {ebitda}");
            // Console.WriteLine($"  currentDebt: {currentDebt}");
            // Console.WriteLine($"  totalInterestExpense: {totalInterestExpense}");
            // Console.WriteLine($"  Taxable Income: {taxableIncome}");
            // Console.WriteLine($"  Corporate Tax ({request.CorporateTaxRate:P0}): {corporateTax}");
            // Console.WriteLine($"  Net Income: {netIncome}");
            // Console.WriteLine($"  totalRedemptionAmount: {totalRedemptionAmount}");
            // Console.WriteLine($"  totalLoanPayments: {totalLoanPayments}");
                }
                
                // Check if EBITDA can cover loan payments - use CURRENT ASSETS as liquid buffer
                decimal actualRedemptionPaid = 0; // Track what redemption was actually paid
                
                if (ebitda < totalLoanPayments)
                {
                    decimal shortage = totalLoanPayments - ebitda;
                    currentAssets -= shortage; // Draw from CURRENT assets (liquid) to cover shortage
                    
                    // If drawing from reserves, can only pay interest (no redemption)
                    // The shortage covers interest first, any remaining goes to redemption
                    if (ebitda >= totalInterestExpense)
                    {
                        // EBITDA covers interest, shortage is for redemption
                        actualRedemptionPaid = shortage <= totalRedemptionAmount ? shortage : totalRedemptionAmount;
                    }
                    else
                    {
                        // EBITDA doesn't even cover interest, no redemption paid
                        actualRedemptionPaid = 0;
                    }
                    
                    if (simulationNumber == 1 && year <= 3)
                    {
            // Console.WriteLine($"[CASH FLOW] Year {year}: EBITDA shortfall of {shortage:N2}");
            // Console.WriteLine($"  EBITDA: {ebitda:N2}, Payments: {totalLoanPayments:N2}");
            // Console.WriteLine($"  Interest: {totalInterestExpense:N2}, Redemption due: {totalRedemptionAmount:N2}");
            // Console.WriteLine($"  Actual redemption paid: {actualRedemptionPaid:N2}");
            // Console.WriteLine($"  Current Assets (liquid) after drawing: {currentAssets:N2}");
                    }
                }
                else
                {
                    // EBITDA covers all payments
                    actualRedemptionPaid = totalRedemptionAmount; // All redemption paid when EBITDA is sufficient
                    
                    // Cash flow impact: EBITDA - interest expense - redemption
                    // Interest expense reduces cash (operating expense)
                    // Redemption reduces cash AND debt (financing activity)
                    // Net effect on current assets = EBITDA - interest - redemption
                    decimal netCashFlow = ebitda - totalInterestExpense - totalRedemptionAmount;
                    currentAssets += netCashFlow;
                    
                    if (simulationNumber == 1 && year <= 3)
                    {
            // Console.WriteLine($"[CASH FLOW] Year {year}: EBITDA covers all payments");
            // Console.WriteLine($"  EBITDA: {ebitda:N2}, Interest: {totalInterestExpense:N2}, Redemption: {totalRedemptionAmount:N2}");
            // Console.WriteLine($"  Net cash flow: {netCashFlow:N2}");
            // Console.WriteLine($"  Current Assets after: {currentAssets:N2}");
                    }
                }
                
                // Calculate liquid assets change AFTER cash flow movements
                decimal liquidAssetsChange = currentAssets - previousLiquidAssets;
                
                // Update balance sheet after cash flows
                // Debt is reduced ONLY by the redemption amount that was actually paid
                currentDebt -= actualRedemptionPaid;
                
                // Update total assets: current assets (liquid) + fixed assets
                // Fixed assets remain constant (no depreciation/capex in this simple model)
                totalAssets = currentAssets + fixedAssets;
                
                // Calculate equity from accounting equation: Assets = Equity + Debt
                // This properly accounts for asset depletion from payments
                decimal oldEquity = currentEquity;
                currentEquity = totalAssets - currentDebt;
                
                // Debug equity changes for first simulation
                if (simulationNumber == 1 && year <= 3)
                {
            // Console.WriteLine($"[EQUITY DEBUG] Year {year}:");
            // Console.WriteLine($"  Old Equity: {oldEquity:N2}");
            // Console.WriteLine($"  Total Assets: {totalAssets:N2} (Current: {currentAssets:N2} + Fixed: {fixedAssets:N2})");
            // Console.WriteLine($"  Current Debt: {currentDebt:N2}");
            // Console.WriteLine($"  New Equity: {currentEquity:N2}");
            // Console.WriteLine($"  Equity Change: {(currentEquity - oldEquity):N2}");
            // Console.WriteLine($"  Liquid Assets Change: {liquidAssetsChange:N2}");
                }
                
                // For consistency, use the accounting equation for total assets reporting
                // This doesn't affect currentAssets tracking which stays independent
                decimal reportedTotalAssets = currentEquity + currentDebt;

                // Calculate coverage
                decimal interestCoverage = totalInterestExpense > 0 ? ebitda / totalInterestExpense : 999;
                bool canPayInterest = ebitda >= totalInterestExpense;

                var yearResult = new YearResult
                {
                    Year = year,
                    Revenue = Math.Round(currentRevenue, 2),
                    OperatingCosts = Math.Round(currentOperatingCosts, 2),
                    EbitdaMargin = Math.Round(currentEbitdaMargin, 4),
                    Ebitda = Math.Round(ebitda, 2),
                    InterestExpense = Math.Round(totalInterestExpense, 2),
                    CorporateTax = Math.Round(corporateTax, 2),
                    NetIncome = Math.Round(netIncome, 2),
                    Assets = Math.Round(reportedTotalAssets, 2), // Report total assets from accounting equation
                    Equity = Math.Round(currentEquity, 2),
                    Debt = Math.Round(currentDebt, 2),
                    InterestCoverage = Math.Round(interestCoverage, 2),
                    CanPayInterest = canPayInterest,
                    RedemptionAmount = Math.Round(actualRedemptionPaid, 2), // Use actual redemption paid, not scheduled
                    LiquidAssets = Math.Round(currentAssets, 2),
                    LiquidAssetsChange = Math.Round(liquidAssetsChange, 2)
                };

                path.Years.Add(yearResult);
                
                // Update previousLiquidAssets for next iteration
                previousLiquidAssets = currentAssets;

                // Check for default - only when both EBITDA < payments AND current assets (liquid) depleted
                if (ebitda < totalLoanPayments && currentAssets <= 0 && !path.DefaultOccurred)
                {
                    path.DefaultOccurred = true;
                    path.DefaultYear = year;
                    
                    // Calculate LGD (Loss Given Default)
                    // Use actual current debt from simulation, not pre-calculated schedule
                    // because the company may not have made all scheduled payments
                    decimal totalCollateralValue = collateralValues.Values.Sum();
                    
                    // Calculate portfolio debt only (exclude external loans with negative IDs)
                    decimal externalFirstLienOutstanding = 0;
                    foreach (var loan in simulatedLoans)
                    {
                        if (loan.LoanId < 0) // External loan (negative ID)
                        {
                            var yearPayment = loan.YearlyPayments.FirstOrDefault(yp => yp.Year == year);
                            externalFirstLienOutstanding += yearPayment?.OutstandingBalance ?? 0;
                        }
                    }
                    
                    // Total outstanding for LGD calc should only include portfolio loans
                    decimal totalOutstanding = currentDebt - externalFirstLienOutstanding;
                    
                    // Debug: Log collateral dictionary contents
                    if (simulationNumber <= 3)
                    {
            // Console.WriteLine($"[DEFAULT - LIQUIDITY DEBUG] Simulation #{simulationNumber}, Year {year}:");
            // Console.WriteLine($"  Collateral dictionary has {collateralValues.Count} entries:");
                        foreach (var kvp in collateralValues)
                        {
            // Console.WriteLine($"    LoanId={kvp.Key}, CollateralValue={kvp.Value:N2}");
                        }
            // Console.WriteLine($"  Total Debt: {currentDebt:N2}");
            // Console.WriteLine($"  External First Lien Outstanding: {externalFirstLienOutstanding:N2}");
            // Console.WriteLine($"  Portfolio Outstanding (for LGD): {totalOutstanding:N2}");
            // Console.WriteLine($"  Processing {simulatedLoans.Count} loans:");
                    }
                    
                    // Calculate total collateral value after haircut and subordination
                    decimal totalCollateralAfterHaircut = 0;
                    decimal totalSubordination = 0;
                    
                    foreach (var loan in simulatedLoans)
                    {
                        if (collateralValues.ContainsKey(loan.LoanId))
                        {
                            decimal collateralValue = collateralValues[loan.LoanId];
                            decimal valueAfterHaircut = collateralValue * (1 - loan.LiquidityHaircut / 100m);
                            totalCollateralAfterHaircut += valueAfterHaircut;
                            
                            // Subordination should only be counted once per unique collateral
                            // Assuming all loans share the same subordination amount, take it from first loan
                            if (totalSubordination == 0)
                            {
                                totalSubordination = loan.Subordination;
                            }
                        }
                    }
                    
                    // Calculate available recovery pool after subtracting senior debt (subordination)
                    decimal availableRecoveryPool = Math.Max(0, totalCollateralAfterHaircut - totalSubordination);
                    
                    // Total recovery is the available pool, capped at total outstanding debt
                    decimal totalRecovery = Math.Min(availableRecoveryPool, totalOutstanding);
                    
                    decimal lossAmount = Math.Max(0, totalOutstanding - totalRecovery);
                    decimal lgdPercentage = totalOutstanding > 0 ? (lossAmount / totalOutstanding) * 100m : 0;
                    
                    path.CollateralValueAtDefault = totalCollateralValue;
                    path.RecoveryAmount = totalRecovery;
                    path.OutstandingDebtAtDefault = totalOutstanding;
                    path.LossGivenDefault = lossAmount;
                    path.LGDPercentage = lgdPercentage;
                    
                    // Log first 3 defaults for debugging
                    if (simulationNumber <= 3)
                    {
            // Console.WriteLine($"[DEFAULT - LIQUIDITY] Simulation #{simulationNumber}, Year {year}:");
            // Console.WriteLine($"  EBITDA: {ebitda:N2}, Total Payments: {totalLoanPayments:N2}");
            // Console.WriteLine($"  Current Assets (liquid): {currentAssets:N2} (depleted)");
            // Console.WriteLine($"  Revenue: {currentRevenue:N2}, Margin: {currentEbitdaMargin:P2}");
            // Console.WriteLine($"  Total Collateral Value: {totalCollateralValue:N2}");
            // Console.WriteLine($"  Total Collateral After Haircut: {totalCollateralAfterHaircut:N2}");
            // Console.WriteLine($"  Total Subordination (First Lien): {totalSubordination:N2}");
            // Console.WriteLine($"  Available Recovery Pool: {availableRecoveryPool:N2}");
            // Console.WriteLine($"  Total Portfolio Outstanding: {totalOutstanding:N2}");
            // Console.WriteLine($"  Total Recovery: {totalRecovery:N2}");
            // Console.WriteLine($"  LGD: {lossAmount:N2} ({lgdPercentage:N1}%)");
                        
                        // Show per-loan breakdown
            // Console.WriteLine($"  Loan details:");
                        foreach (var loan in simulatedLoans)
                        {
                            var yearPayment = loan.YearlyPayments.FirstOrDefault(yp => yp.Year == year);
                            decimal outstandingBalance = yearPayment?.OutstandingBalance ?? 0;
                            string loanType = loan.LoanId < 0 ? " (External Loan - NOT in portfolio)" : "";
                            
                            if (collateralValues.ContainsKey(loan.LoanId))
                            {
                                decimal collateralValue = collateralValues[loan.LoanId];
            // Console.WriteLine($"    Loan {loan.LoanId}{loanType}: Outstanding={outstandingBalance:N2}, Collateral={collateralValue:N2}, " +
            //                     $"Haircut={loan.LiquidityHaircut:N1}%, Subordination={loan.Subordination:N2}");
                            }
                        }
                    }
                    
                    // DON'T break - continue simulation but freeze values at default state
                    // For remaining years, we'll just copy the default year values
                }

                // INSOLVENCY CHECK DISABLED: We only consider liquidity default (can't pay debts)
                // Negative equity alone does not trigger default as long as they can pay interest/redemptions
                /*
                // If equity becomes negative, company is insolvent
                if (currentEquity < 0 && !path.DefaultOccurred)
                {
                    path.DefaultOccurred = true;
                    path.DefaultYear = year;
                    
                    // Calculate LGD
                    decimal totalCollateralValue = collateralValues.Values.Sum();
                    decimal totalRecovery = 0;
                    decimal totalOutstanding = 0;
                    
                    foreach (var loan in simulatedLoans)
                    {
                        var yearPayment = loan.YearlyPayments.FirstOrDefault(yp => yp.Year == year);
                        decimal outstandingBalance = yearPayment?.OutstandingBalance ?? 0;
                        
                        // Only include portfolio loans in outstanding debt (not external first lien)
                        // First lien loan has LoanId = -1 and represents external debt
                        if (loan.LoanId != -1)
                        {
                            totalOutstanding += outstandingBalance;
                        }
                        
                        if (collateralValues.ContainsKey(loan.LoanId))
                        {
                            decimal collateralValue = collateralValues[loan.LoanId];
                            decimal valueAfterHaircut = collateralValue * (1 - loan.LiquidityHaircut / 100m);
                            decimal recoveryValue = Math.Max(0, valueAfterHaircut - loan.Subordination);
                            totalRecovery += recoveryValue;
                        }
                    }
                    
                    decimal lossAmount = Math.Max(0, totalOutstanding - totalRecovery);
                    decimal lgdPercentage = totalOutstanding > 0 ? (lossAmount / totalOutstanding) * 100m : 0;
                    
                    path.CollateralValueAtDefault = totalCollateralValue;
                    path.RecoveryAmount = totalRecovery;
                    path.OutstandingDebtAtDefault = totalOutstanding;
                    path.LossGivenDefault = lossAmount;
                    path.LGDPercentage = lgdPercentage;
                    
                    // Log insolvency
                    if (simulationNumber <= 3)
                    {
            // Console.WriteLine($"[INSOLVENCY] Simulation #{simulationNumber}, Year {year}:");
            // Console.WriteLine($"  Equity: {currentEquity:N2}");
            // Console.WriteLine($"  LGD: {path.LossGivenDefault:N2} ({path.LGDPercentage:N1}%)");
                    }
                    
                    // DON'T break - continue simulation but freeze values at default state
                }
                */
            }

            // Store cumulative interest paid during simulation period
            path.CumulativeInterestPaid = cumulativeInterestPaid;

            return path;
        }

        /// <summary>
        /// Generate a random number from normal distribution using Box-Muller transform
        /// </summary>
        private double GenerateNormalRandom(double mean, double stdDev)
        {
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        /// <summary>
        /// Calculate percentile from sorted list
        /// </summary>
        private decimal CalculatePercentile(List<decimal> sortedValues, int percentile)
        {
            if (sortedValues.Count == 0) return 0;
            
            double index = (percentile / 100.0) * (sortedValues.Count - 1);
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);
            
            if (lowerIndex == upperIndex)
                return sortedValues[lowerIndex];
            
            double weight = index - lowerIndex;
            return sortedValues[lowerIndex] * (1 - (decimal)weight) + sortedValues[upperIndex] * (decimal)weight;
        }
    }
}
