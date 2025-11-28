using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Models.DTOs;
using LoanAnnuityCalculatorAPI.Services;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models.Loan;
using LoanAnnuityCalculatorAPI.Models.Settings;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonteCarloController : ControllerBase
    {
        private readonly MonteCarloSimulationService _simulationService;
        private readonly ILogger<MonteCarloController> _logger;
        private readonly LoanDbContext _dbContext;
        private readonly LoanFinancialCalculatorService _loanFinancialCalculator;
        private readonly PaymentCalculatorService _paymentCalculator;
        private readonly LoanDateHelper _loanDateHelper;
        private readonly BalanceSheetCalculationService _balanceSheetCalculation;
        private readonly SectorCorrelationSeedService _sectorCorrelationSeedService;

        public MonteCarloController(
            MonteCarloSimulationService simulationService,
            ILogger<MonteCarloController> logger,
            LoanDbContext dbContext,
            PaymentCalculatorService paymentCalculator,
            LoanDateHelper loanDateHelper,
            BalanceSheetCalculationService balanceSheetCalculation,
            SectorCorrelationSeedService sectorCorrelationSeedService)
        {
            _simulationService = simulationService;
            _logger = logger;
            _dbContext = dbContext;
            _loanFinancialCalculator = new LoanFinancialCalculatorService();
            _paymentCalculator = paymentCalculator;
            _loanDateHelper = loanDateHelper;
            _balanceSheetCalculation = balanceSheetCalculation;
            _sectorCorrelationSeedService = sectorCorrelationSeedService;
        }

        /// <summary>
        /// Run Monte Carlo simulation for debtor P&L and balance sheet
        /// </summary>
        [HttpPost("simulate")]
        public async Task<ActionResult<MonteCarloSimulationResponse>> RunSimulation([FromBody] MonteCarloSimulationRequest request)
        {
            try
            {
                _logger.LogInformation("Running Monte Carlo simulation for debtor {DebtorId} with {Simulations} simulations over {Years} years",
                    request.DebtorId, request.NumberOfSimulations, request.SimulationYears);

                // If using actual loans, load them and calculate initial financial state
                List<SimulatedLoanInfo> simulatedLoans = new List<SimulatedLoanInfo>();
                
                // Load external loans from balance sheet line items (if requested)
                if (request.IncludeFirstLien)
                {
                    var balanceSheetCalc = await _balanceSheetCalculation.CalculateFromLineItems(request.DebtorId);
                    
                    if (balanceSheetCalc?.ExternalLoans != null && balanceSheetCalc.ExternalLoans.Any())
                    {
                        _logger.LogInformation("Found {Count} external loan(s) from line items", balanceSheetCalc.ExternalLoans.Count);
                        
                        foreach (var externalLoan in balanceSheetCalc.ExternalLoans)
                        {
                            // Only include if we have complete data
                            if (externalLoan.InterestRate.HasValue && externalLoan.TenorMonths.HasValue)
                            {
                                var firstLienLoan = new SimulatedLoanInfo
                                {
                                    LoanId = -externalLoan.LineItemId, // Negative ID to distinguish from actual loans
                                    LoanAmount = externalLoan.Amount,
                                    InterestRate = externalLoan.InterestRate.Value,
                                    TenorMonths = externalLoan.TenorMonths.Value,
                                    RedemptionSchedule = externalLoan.RedemptionSchedule ?? "Annuity",
                                    CollateralValue = 0, // First lien collateral not tracked separately
                                    LiquidityHaircut = 0,
                                    Subordination = 0
                                };
                                
                                // Calculate yearly payment schedule
                                firstLienLoan.YearlyPayments = CalculateYearlyPaymentScheduleFromInfo(firstLienLoan, request.SimulationYears);
                                simulatedLoans.Add(firstLienLoan);
                                
                                _logger.LogInformation("External loan added: Amount={Amount}, Rate={Rate}%, Tenor={Tenor} months",
                                    externalLoan.Amount, externalLoan.InterestRate, externalLoan.TenorMonths);
                            }
                            else
                            {
                                _logger.LogWarning("External loan found but missing rate or tenor: {Label}", externalLoan.Label);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No external loans found in balance sheet line items");
                    }
                }
                else
                {
                    _logger.LogInformation("External loans excluded from simulation (IncludeFirstLien=false)");
                }
                
                if (request.UseActualLoans)
                {
                    // Load financial data from database to ensure consistency (SAME AS PORTFOLIO MODE)
                    var balanceSheet = await _balanceSheetCalculation.CalculateFromLineItems(request.DebtorId);
                    var latestPL = await _dbContext.DebtorPLs
                        .Where(pl => pl.DebtorID == request.DebtorId)
                        .OrderByDescending(pl => pl.BookYear)
                        .FirstOrDefaultAsync();
                    
                    if (balanceSheet != null && latestPL != null)
                    {
                        request.InitialRevenue = latestPL.Revenue;
                        request.InitialOperatingCosts = latestPL.OperatingExpenses;
                        request.InitialEquity = balanceSheet.Equity;
                        request.InitialAssets = balanceSheet.TotalAssets;
                        request.InitialLiquidAssets = balanceSheet.CurrentAssets;
                    }
                    
                    var loansQuery = _dbContext.Loans
                        .Include(l => l.LoanCollaterals)
                            .ThenInclude(lc => lc.Collateral)
                        .Where(l => l.DebtorID == request.DebtorId);
                    
                    // Filter by specific loan IDs if provided
                    if (request.LoanIds != null && request.LoanIds.Any())
                    {
                        loansQuery = loansQuery.Where(l => request.LoanIds.Contains(l.LoanID));
                    }
                    
                    var loans = await loansQuery.ToListAsync();
                    
                    // Only return error if no loans AND no external loans exist
                    if (!loans.Any() && !simulatedLoans.Any())
                    {
                        return BadRequest(new { message = "No loans found for the specified debtor." });
                    }

                    // Get outstanding amounts from balance sheet line items
                    var loanIds = loans.Select(l => l.LoanID).ToList();
                    var outstandingAmounts = await _balanceSheetCalculation.GetLoanOutstandingAmounts(request.DebtorId, loanIds);

                    // Calculate total debt and interest for simulation base year
                    int baseYear = DateTime.Now.Year;
                    decimal totalDebt = 0;
                    decimal totalAnnualInterest = 0;
                    decimal totalCollateral = 0;

                    foreach (var loan in loans)
                    {
                        // Use outstanding amount from balance sheet if available, otherwise calculate
                        decimal outstanding = outstandingAmounts.ContainsKey(loan.LoanID) 
                            ? outstandingAmounts[loan.LoanID]
                            : _loanFinancialCalculator.CalculateOutstandingBalanceAtYear(loan, baseYear);
                        
                        var interest = _loanFinancialCalculator.CalculateInterestForYear(loan, baseYear);
                        var collateral = _loanFinancialCalculator.GetTotalCollateralValue(loan);
                        
                        // Calculate weighted average liquidity haircut and total subordination
                        decimal liquidityHaircut = 0;
                        decimal subordination = 0;
                        
                        if (loan.LoanCollaterals != null && loan.LoanCollaterals.Any())
                        {
                            decimal totalCollateralValue = 0;
                            decimal weightedHaircut = 0;
                            
                            foreach (var lc in loan.LoanCollaterals)
                            {
                                if (lc.Collateral != null)
                                {
                                    var collateralValue = lc.Collateral.AppraisalValue ?? 0;
                                    totalCollateralValue += collateralValue;
                                    weightedHaircut += collateralValue * lc.Collateral.LiquidityHaircut;
                                    subordination += lc.Collateral.FirstMortgageAmount ?? 0;
                                }
                            }
                            
                            if (totalCollateralValue > 0)
                            {
                                liquidityHaircut = weightedHaircut / totalCollateralValue;
                            }
                        }

                        totalDebt += outstanding;
                        totalAnnualInterest += interest;
                        totalCollateral += collateral;

                        // Calculate yearly payment schedule for this loan
                        var yearlyPayments = CalculateYearlyPaymentSchedule(loan, request.SimulationYears);

                        // Determine primary property type for this loan's collateral
                        string? propertyType = loan.LoanCollaterals
                            .Where(lc => lc.Collateral != null && !string.IsNullOrEmpty(lc.Collateral.PropertyType))
                            .Select(lc => lc.Collateral!.PropertyType)
                            .FirstOrDefault();

                        simulatedLoans.Add(new SimulatedLoanInfo
                        {
                            LoanId = loan.LoanID,
                            LoanAmount = loan.LoanAmount,
                            InterestRate = loan.AnnualInterestRate,
                            TenorMonths = loan.TenorMonths,
                            RedemptionSchedule = loan.RedemptionSchedule,
                            CollateralValue = collateral,
                            LiquidityHaircut = liquidityHaircut,
                            Subordination = subordination,
                            CollateralPropertyType = propertyType,
                            YearlyPayments = yearlyPayments
                        });
                    }

                    // Override request parameters with actual loan data
                    request.InitialDebt = 0; // Set to 0 since we're using actual loan debt
                    request.LoanAmount = totalDebt; // Total outstanding balance from actual loans
                    
                    // Calculate weighted average interest rate
                    if (totalDebt > 0)
                    {
                        request.InterestRate = (totalAnnualInterest / totalDebt) * 100m;
                    }
                    
                    _logger.LogInformation("Using {LoanCount} actual loans with total debt: {TotalDebt}, weighted avg rate: {AvgRate}%, annual interest: {AnnualInterest}",
                        loans.Count, totalDebt, request.InterestRate, totalAnnualInterest);
                }
                else
                {
                    // Legacy mode: use hypothetical loan
                    simulatedLoans.Add(new SimulatedLoanInfo
                    {
                        LoanId = 0,
                        LoanAmount = request.LoanAmount,
                        InterestRate = request.InterestRate,
                        TenorMonths = request.SimulationYears * 12,
                        RedemptionSchedule = "Annuity",
                        CollateralValue = 0
                    });
                }

                // Log total simulated loans before running simulation
                _logger.LogInformation("Total simulated loans for Monte Carlo: {LoanCount}", simulatedLoans.Count);
                foreach (var loan in simulatedLoans)
                {
                    _logger.LogInformation("  Loan ID {LoanId}: Amount={Amount}, Rate={Rate}%, Payments={PaymentCount}", 
                        loan.LoanId, loan.LoanAmount, loan.InterestRate, loan.YearlyPayments?.Count ?? 0);
                }

                // Enrich request with sector correlation data from database
                // This loads sector-sector and sector-collateral correlations from the model settings tables
                // Uses model settings ID 1 by default (can be made configurable later)
                await EnrichRequestWithSectorData(request, modelSettingsId: 1, simulatedLoans);

                var result = _simulationService.RunSimulation(request, simulatedLoans);

                _logger.LogInformation("Simulation completed. Probability of default: {ProbabilityOfDefault}%",
                    result.Statistics.ProbabilityOfDefault);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running Monte Carlo simulation");
                return StatusCode(500, new { message = "Error running simulation", error = ex.Message });
            }
        }

        /// <summary>
        /// Get current model settings
        /// </summary>
        [HttpGet("settings")]
        public async Task<ActionResult<ModelSettingsResponse>> GetModelSettings()
        {
            var settings = await _dbContext.ModelSettings
                .Include(s => s.PropertyTypeParameters)
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                // Create default settings if none exist
                settings = new ModelSettings();
                _dbContext.ModelSettings.Add(settings);
                await _dbContext.SaveChangesAsync();
            }

            var dto = new ModelSettingsDto
            {
                Id = settings.Id,
                SettingName = settings.SettingName,
                DefaultRevenueGrowthRate = settings.DefaultRevenueGrowthRate,
                DefaultOperatingCostGrowthRate = settings.DefaultOperatingCostGrowthRate,
                DefaultRevenueVolatility = settings.DefaultRevenueVolatility,
                DefaultEbitdaMarginVolatility = settings.DefaultEbitdaMarginVolatility,
                DefaultOperatingCostVolatility = settings.DefaultOperatingCostVolatility,
                DefaultCorporateTaxRate = settings.DefaultCorporateTaxRate,
                DefaultCollateralExpectedReturn = settings.DefaultCollateralExpectedReturn,
                DefaultCollateralVolatility = settings.DefaultCollateralVolatility,
                DefaultCollateralCorrelation = settings.DefaultCollateralCorrelation,
                PropertyTypeParameters = settings.PropertyTypeParameters.Select(p => new PropertyTypeParametersDto
                {
                    PropertyType = p.PropertyType,
                    ExpectedReturn = p.ExpectedReturn,
                    Volatility = p.Volatility,
                    CorrelationWithRevenue = p.CorrelationWithRevenue
                }).ToList(),
                CreatedAt = settings.CreatedAt,
                UpdatedAt = settings.UpdatedAt
            };

            return Ok(new ModelSettingsResponse { Settings = dto });
        }

        /// <summary>
        /// Update model settings
        /// </summary>
        [HttpPut("settings")]
        public async Task<ActionResult<ModelSettingsResponse>> UpdateModelSettings([FromBody] ModelSettingsDto settingsDto)
        {
            try
            {
                var settings = await _dbContext.ModelSettings
                    .Include(s => s.PropertyTypeParameters)
                    .FirstOrDefaultAsync();

                if (settings == null)
                {
                    // Create new settings
                    settings = new ModelSettings();
                    _dbContext.ModelSettings.Add(settings);
                }

                // Update settings from DTO
                settings.SettingName = settingsDto.SettingName;
                settings.DefaultRevenueGrowthRate = settingsDto.DefaultRevenueGrowthRate;
                settings.DefaultOperatingCostGrowthRate = settingsDto.DefaultOperatingCostGrowthRate;
                settings.DefaultRevenueVolatility = settingsDto.DefaultRevenueVolatility;
                settings.DefaultEbitdaMarginVolatility = settingsDto.DefaultEbitdaMarginVolatility;
                settings.DefaultOperatingCostVolatility = settingsDto.DefaultOperatingCostVolatility;
                settings.DefaultCorporateTaxRate = settingsDto.DefaultCorporateTaxRate;
                settings.DefaultCollateralExpectedReturn = settingsDto.DefaultCollateralExpectedReturn;
                settings.DefaultCollateralVolatility = settingsDto.DefaultCollateralVolatility;
                settings.DefaultCollateralCorrelation = settingsDto.DefaultCollateralCorrelation;
                settings.UpdatedAt = DateTime.UtcNow;

                // Update property type parameters
                // Remove existing ones
                var existingParams = settings.PropertyTypeParameters.ToList();
                foreach (var param in existingParams)
                {
                    _dbContext.PropertyTypeParameters.Remove(param);
                }

                // Add new ones
                settings.PropertyTypeParameters = settingsDto.PropertyTypeParameters.Select(dto => new PropertyTypeParameters
                {
                    PropertyType = dto.PropertyType,
                    ExpectedReturn = dto.ExpectedReturn,
                    Volatility = dto.Volatility,
                    CorrelationWithRevenue = dto.CorrelationWithRevenue
                }).ToList();

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Model settings updated");

                // Return updated settings as DTO
                var responseDto = new ModelSettingsDto
                {
                    Id = settings.Id,
                    SettingName = settings.SettingName,
                    DefaultRevenueGrowthRate = settings.DefaultRevenueGrowthRate,
                    DefaultOperatingCostGrowthRate = settings.DefaultOperatingCostGrowthRate,
                    DefaultRevenueVolatility = settings.DefaultRevenueVolatility,
                    DefaultEbitdaMarginVolatility = settings.DefaultEbitdaMarginVolatility,
                    DefaultOperatingCostVolatility = settings.DefaultOperatingCostVolatility,
                    DefaultCorporateTaxRate = settings.DefaultCorporateTaxRate,
                    DefaultCollateralExpectedReturn = settings.DefaultCollateralExpectedReturn,
                    DefaultCollateralVolatility = settings.DefaultCollateralVolatility,
                    DefaultCollateralCorrelation = settings.DefaultCollateralCorrelation,
                    PropertyTypeParameters = settings.PropertyTypeParameters.Select(p => new PropertyTypeParametersDto
                    {
                        PropertyType = p.PropertyType,
                        ExpectedReturn = p.ExpectedReturn,
                        Volatility = p.Volatility,
                        CorrelationWithRevenue = p.CorrelationWithRevenue
                    }).ToList(),
                    CreatedAt = settings.CreatedAt,
                    UpdatedAt = settings.UpdatedAt
                };

                return Ok(new ModelSettingsResponse { Settings = responseDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating model settings");
                return StatusCode(500, new { message = "Error updating settings", error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate yearly payment schedule for a loan
        /// </summary>
        private List<YearlyLoanPayment> CalculateYearlyPaymentSchedule(Loan loan, int simulationYears)
        {
            var yearlyPayments = new List<YearlyLoanPayment>();
            
            try
            {
                // Calculate how many months have already elapsed since loan start
                int monthsElapsed = _loanDateHelper.CalculateMonthsDifference(loan.StartDate);
                if (monthsElapsed < 0) monthsElapsed = 0; // Future loans start at month 0
                
                // Calculate remaining months in the loan
                int remainingMonths = loan.TenorMonths - monthsElapsed;
                
                _logger.LogInformation("Loan {LoanId}: Started {StartDate}, Elapsed={Elapsed} months, Tenor={Tenor}, Remaining={Remaining}",
                    loan.LoanID, loan.StartDate.ToString("yyyy-MM-dd"), monthsElapsed, loan.TenorMonths, remainingMonths);
                
                // Get monthly payment schedule for entire tenor
                var monthlyPayments = _paymentCalculator.CalculateForEntireTenor(
                    loan.LoanAmount,
                    loan.AnnualInterestRate,
                    loan.TenorMonths,
                    loan.InterestOnlyMonths,
                    loan.RedemptionSchedule
                );

                // Aggregate monthly payments into yearly totals
                // First, add year 0 for historical interest (from loan inception to simulation start)
                if (monthsElapsed > 0)
                {
                    decimal historicalInterest = 0;
                    decimal historicalRedemption = 0;
                    decimal startingBalance = loan.LoanAmount;
                    
                    // Sum up all payments made before simulation starts (months 1 to monthsElapsed)
                    for (int month = 1; month <= monthsElapsed && month <= loan.TenorMonths; month++)
                    {
                        var payment = monthlyPayments.ElementAtOrDefault(month - 1);
                        if (payment != default)
                        {
                            historicalInterest += payment.interestComponent;
                            historicalRedemption += payment.capitalComponent;
                            startingBalance = payment.remainingLoan;
                        }
                    }
                    
                    yearlyPayments.Add(new YearlyLoanPayment
                    {
                        Year = 0, // Historical period
                        InterestExpense = historicalInterest,
                        RedemptionAmount = historicalRedemption,
                        OutstandingBalance = startingBalance
                    });
                    
                    _logger.LogInformation("Loan {LoanId}: Historical interest (year 0) = {HistoricalInterest}, months={MonthsElapsed}",
                        loan.LoanID, historicalInterest, monthsElapsed);
                }
                
                // Then add future years (simulation period)
                // Start from the CURRENT position in the loan schedule (monthsElapsed)
                for (int year = 1; year <= simulationYears; year++)
                {
                    decimal yearlyInterest = 0;
                    decimal yearlyRedemption = 0;
                    decimal endOfYearBalance = 0;
                    
                    // Calculate which months of the loan schedule correspond to this simulation year
                    // Simulation year 1 = months (monthsElapsed+1) to (monthsElapsed+12)
                    int startMonth = monthsElapsed + (year - 1) * 12 + 1;
                    int endMonth = monthsElapsed + year * 12;
                    
                    for (int month = startMonth; month <= endMonth && month <= loan.TenorMonths; month++)
                    {
                        var payment = monthlyPayments.ElementAtOrDefault(month - 1);
                        if (payment != default)
                        {
                            yearlyInterest += payment.interestComponent;
                            yearlyRedemption += payment.capitalComponent;
                            endOfYearBalance = payment.remainingLoan;
                        }
                    }

                    yearlyPayments.Add(new YearlyLoanPayment
                    {
                        Year = year,
                        InterestExpense = yearlyInterest,
                        RedemptionAmount = yearlyRedemption,
                        OutstandingBalance = endOfYearBalance
                    });
                    
                    // If loan is fully paid off, remaining years have zero payments
                    if (endOfYearBalance == 0 && year < simulationYears)
                    {
                        _logger.LogInformation("Loan {LoanId} fully paid off at end of simulation year {Year}", loan.LoanID, year);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating yearly payment schedule for loan {LoanId}", loan.LoanID);
            }

            return yearlyPayments;
        }

        /// <summary>
        /// Calculate yearly payment schedule from SimulatedLoanInfo (for first lien loans without a Loan entity)
        /// </summary>
        private List<YearlyLoanPayment> CalculateYearlyPaymentScheduleFromInfo(SimulatedLoanInfo loanInfo, int simulationYears)
        {
            var yearlyPayments = new List<YearlyLoanPayment>();
            
            try
            {
                _logger.LogInformation("First Lien Loan: Amount={Amount}, Rate={Rate}%, Tenor={Tenor} months",
                    loanInfo.LoanAmount, loanInfo.InterestRate, loanInfo.TenorMonths);
                
                // Get monthly payment schedule for entire tenor
                var monthlyPayments = _paymentCalculator.CalculateForEntireTenor(
                    loanInfo.LoanAmount,
                    loanInfo.InterestRate,
                    loanInfo.TenorMonths,
                    0, // No interest-only period for first lien
                    loanInfo.RedemptionSchedule
                );

                // Aggregate monthly payments into yearly totals
                for (int year = 1; year <= simulationYears; year++)
                {
                    decimal yearlyInterest = 0;
                    decimal yearlyRedemption = 0;
                    decimal endOfYearBalance = 0;
                    
                    int startMonth = (year - 1) * 12 + 1;
                    int endMonth = year * 12;
                    
                    for (int month = startMonth; month <= endMonth && month <= loanInfo.TenorMonths; month++)
                    {
                        var payment = monthlyPayments.ElementAtOrDefault(month - 1);
                        if (payment != default)
                        {
                            yearlyInterest += payment.interestComponent;
                            yearlyRedemption += payment.capitalComponent;
                            endOfYearBalance = payment.remainingLoan;
                        }
                    }
                    
                    yearlyPayments.Add(new YearlyLoanPayment
                    {
                        Year = year,
                        InterestExpense = yearlyInterest,
                        RedemptionAmount = yearlyRedemption,
                        OutstandingBalance = endOfYearBalance
                    });
                    
                    if (endOfYearBalance <= 0)
                    {
                        _logger.LogInformation("First lien loan fully paid off at end of simulation year {Year}", year);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating first lien payment schedule");
            }
            
            return yearlyPayments;
        }

        /// <summary>
        /// Get balance sheet calculations from line items for Monte Carlo simulation initialization
        /// </summary>
        [HttpGet("balance-sheet-calculation/{debtorId}")]
        public async Task<ActionResult<BalanceSheetCalculation>> GetBalanceSheetCalculation(int debtorId, [FromQuery] int? bookYear = null)
        {
            try
            {
                var calculation = await _balanceSheetCalculation.CalculateFromLineItems(debtorId, bookYear);
                
                if (calculation == null)
                {
                    return NotFound(new { message = "No balance sheet found for the specified debtor." });
                }
                
                return Ok(calculation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating balance sheet for debtor {DebtorId}", debtorId);
                return StatusCode(500, new { message = "Error calculating balance sheet", error = ex.Message });
            }
        }
        
        /// <summary>
        /// Enrich simulation request with sector correlation data from database
        /// This automatically loads sector-sector and sector-collateral correlations for the specified model settings
        /// </summary>
        private async Task EnrichRequestWithSectorData(MonteCarloSimulationRequest request, int modelSettingsId, List<SimulatedLoanInfo> simulatedLoans)
        {
            // If sector weights are already provided and correlation matrix exists, skip database lookup
            if (request.SectorWeights != null && request.SectorWeights.Any() && request.SectorCorrelationMatrix != null)
            {
                _logger.LogInformation("Sector data already provided in request, skipping database enrichment");
                return;
            }
            
            try
            {
                // Load sector definitions to get volatilities
                var sectors = await _dbContext.SectorDefinitions
                    .Where(s => s.ModelSettingsId == modelSettingsId && s.IsActive)
                    .ToListAsync();
                
                if (!sectors.Any())
                {
                    _logger.LogWarning("No active sectors found for model settings {ModelSettingsId}, using legacy approach", modelSettingsId);
                    return;
                }
                
                // Build sector volatilities dictionary
                request.SectorVolatilities = new Dictionary<Models.Sector, decimal>();
                foreach (var sector in sectors)
                {
                    if (Enum.TryParse<Models.Sector>(sector.SectorCode, out var sectorEnum))
                    {
                        request.SectorVolatilities[sectorEnum] = sector.DefaultVolatility;
                    }
                }
                
                // Build correlation matrix from database
                var correlationMatrix = await _sectorCorrelationSeedService.BuildCorrelationMatrixAsync(modelSettingsId);
                if (correlationMatrix != null)
                {
                    request.SectorCorrelationMatrix = correlationMatrix;
                    _logger.LogInformation("Loaded {Size}x{Size} sector correlation matrix from database", 
                        correlationMatrix.GetLength(0), correlationMatrix.GetLength(1));
                }
                
                // Load sector weights from latest P&L if available
                if (request.SectorWeights == null || !request.SectorWeights.Any())
                {
                    var latestPL = await _dbContext.DebtorPLs
                        .Where(pl => pl.DebtorID == request.DebtorId)
                        .OrderByDescending(pl => pl.BookYear)
                        .FirstOrDefaultAsync();
                    
                    if (latestPL != null && !string.IsNullOrEmpty(latestPL.RevenueSectorBreakdown))
                    {
                        try
                        {
                            // Parse sector breakdown and calculate weights
                            var sectorBreakdown = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, decimal>>(latestPL.RevenueSectorBreakdown);
                            if (sectorBreakdown != null && sectorBreakdown.Any())
                            {
                                var totalRevenue = sectorBreakdown.Values.Sum();
                                request.SectorWeights = new Dictionary<Models.Sector, decimal>();
                                
                                foreach (var kvp in sectorBreakdown)
                                {
                                    if (Enum.TryParse<Models.Sector>(kvp.Key, out var sectorEnum))
                                    {
                                        request.SectorWeights[sectorEnum] = totalRevenue > 0 ? kvp.Value / totalRevenue : 0;
                                    }
                                }
                                
                                _logger.LogInformation("Loaded sector weights from P&L (BookYear {Year}): {Count} sectors", 
                                    latestPL.BookYear, request.SectorWeights.Count);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error parsing sector breakdown from P&L");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No sector breakdown found in latest P&L, using legacy revenue volatility approach");
                    }
                }
                
                // Load sector-collateral correlations from database
                var sectorCollateralCorrelations = await _dbContext.SectorCollateralCorrelations
                    .Where(sc => sc.ModelSettingsId == modelSettingsId)
                    .ToListAsync();
                
                if (sectorCollateralCorrelations.Any())
                {
                    request.SectorCollateralCorrelations = new Dictionary<(Models.Sector, string), decimal>();
                    
                    foreach (var sc in sectorCollateralCorrelations)
                    {
                        if (Enum.TryParse<Models.Sector>(sc.Sector, out var sectorEnum))
                        {
                            request.SectorCollateralCorrelations[(sectorEnum, sc.PropertyType)] = sc.CorrelationCoefficient;
                        }
                    }
                    
                    _logger.LogInformation("Loaded {Count} sector-collateral correlations from database", 
                        request.SectorCollateralCorrelations.Count);
                }
                
                // Determine primary collateral property type from balance sheet line items
                // This ensures we use the same property types that appear on the balance sheet
                List<string> collateralTypes = new List<string>();
                
                if (simulatedLoans.Any() && request.DebtorId > 0)
                {
                    // First try: Get property types from balance sheet line items
                    var balanceSheet = await _dbContext.DebtorBalanceSheets
                        .Where(bs => bs.DebtorID == request.DebtorId)
                        .OrderByDescending(bs => bs.BookYear)
                        .FirstOrDefaultAsync();
                    
                    if (balanceSheet != null)
                    {
                        var lineItems = await _dbContext.BalanceSheetLineItems
                            .Include(li => li.Collateral)
                            .Where(li => li.BalanceSheetId == balanceSheet.Id && li.Collateral != null)
                            .ToListAsync();
                        
                        collateralTypes = lineItems
                            .Where(li => !string.IsNullOrEmpty(li.Collateral!.PropertyType))
                            .Select(li => li.Collateral!.PropertyType)
                            .ToList();
                        
                        if (collateralTypes.Any())
                        {
                            _logger.LogInformation("Found {Count} collateral property types from balance sheet (BookYear {Year}) for debtor {DebtorId}", 
                                collateralTypes.Count, balanceSheet.BookYear, request.DebtorId);
                        }
                    }
                    
                    // Fallback: If no property types found in balance sheet, get from loans table
                    if (!collateralTypes.Any())
                    {
                        _logger.LogInformation("No property types in balance sheet, falling back to loans table for debtor {DebtorId}", 
                            request.DebtorId);
                        
                        var loans = await _dbContext.Loans
                            .Include(l => l.LoanCollaterals)
                                .ThenInclude(lc => lc.Collateral)
                            .Where(l => l.DebtorID == request.DebtorId)
                            .ToListAsync();
                        
                        collateralTypes = loans
                            .SelectMany(l => l.LoanCollaterals)
                            .Where(lc => lc.Collateral != null && !string.IsNullOrEmpty(lc.Collateral.PropertyType))
                            .Select(lc => lc.Collateral!.PropertyType)
                            .ToList();
                    }
                    
                    if (collateralTypes.Any())
                    {
                        // Group by property type and get the one with highest count
                        var primaryPropertyType = collateralTypes
                            .GroupBy(pt => pt)
                            .OrderByDescending(g => g.Count())
                            .First()
                            .Key;
                        
                        // Translate Dutch property types to English (standardized for correlation table)
                        var propertyTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "Industrie", "Industrial" },
                            { "Kantoor", "Office" },
                            { "Winkels", "Retail" },
                            { "Woningen", "Residential" },
                            { "Residential", "Residential" },
                            { "Commercial", "Commercial" },
                            { "Industrial", "Industrial" },
                            { "Office", "Office" },
                            { "Retail", "Retail" }
                        };
                        
                        // Translate property type if mapping exists
                        var translatedPropertyType = propertyTypeMap.ContainsKey(primaryPropertyType) 
                            ? propertyTypeMap[primaryPropertyType] 
                            : primaryPropertyType;
                        
                        _logger.LogInformation("Primary collateral property type for debtor {DebtorId}: {PropertyType} (translated to: {TranslatedType})", 
                            request.DebtorId, primaryPropertyType, translatedPropertyType);
                        
                        // Filter sector-collateral correlations to only include this property type
                        if (request.SectorCollateralCorrelations != null)
                        {
                            var filteredCorrelations = request.SectorCollateralCorrelations
                                .Where(kvp => kvp.Key.Item2.Equals(translatedPropertyType, StringComparison.OrdinalIgnoreCase))
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                            
                            request.SectorCollateralCorrelations = filteredCorrelations;
                            
                            _logger.LogInformation("Filtered to {Count} correlations for property type {PropertyType}", 
                                filteredCorrelations.Count, translatedPropertyType);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No collateral property types found for debtor {DebtorId}, will use legacy correlation approach", 
                            request.DebtorId);
                    }
                }
                
                _logger.LogInformation("Successfully enriched request with sector correlation data from database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching request with sector data, falling back to legacy approach");
            }
        }

        [HttpPost("simulate-portfolio")]
        public async Task<ActionResult<PortfolioMonteCarloResponse>> RunPortfolioSimulation([FromBody] PortfolioMonteCarloRequest request)
        {
            try
            {
                _logger.LogInformation("Running portfolio MC for {Count} debtors", request.DebtorIds.Count);
                int modelSettingsId = 1;
                
                // Load model settings from database (same as individual mode and frontend)
                var modelSettings = await _dbContext.ModelSettings.FindAsync(modelSettingsId);
                decimal defaultRevenueGrowthRate = modelSettings?.DefaultRevenueGrowthRate ?? 0.00m;
                decimal defaultOperatingCostGrowthRate = modelSettings?.DefaultOperatingCostGrowthRate ?? 0.02m;
                decimal defaultRevenueVolatility = modelSettings?.DefaultRevenueVolatility ?? 0.15m;
                decimal defaultOperatingCostVolatility = modelSettings?.DefaultOperatingCostVolatility ?? 0.10m;
                decimal defaultCollateralExpectedReturn = modelSettings?.DefaultCollateralExpectedReturn ?? 0.02m;
                decimal defaultCollateralVolatility = modelSettings?.DefaultCollateralVolatility ?? 0.10m;
                decimal defaultCollateralCorrelation = modelSettings?.DefaultCollateralCorrelation ?? 0.30m;
                decimal defaultCorporateTaxRate = modelSettings?.DefaultCorporateTaxRate ?? 0.21m;
                
                _logger.LogInformation("Loaded model settings: RevenueGrowth={Growth:P2}, CollateralReturn={Return:P2}, CollateralVol={Vol:P2}",
                    defaultRevenueGrowthRate, defaultCollateralExpectedReturn, defaultCollateralVolatility);
                
                var sectors = await _dbContext.SectorDefinitions.Where(s => s.ModelSettingsId == modelSettingsId && s.IsActive).ToListAsync();
                var correlationMatrix = await _sectorCorrelationSeedService.BuildCorrelationMatrixAsync(modelSettingsId);
                var sectorVolatilities = new Dictionary<Models.Sector, decimal>();
                foreach (var sector in sectors)
                {
                    if (Enum.TryParse<Models.Sector>(sector.SectorCode, out var sectorEnum))
                        sectorVolatilities[sectorEnum] = sector.DefaultVolatility;
                }
                var debtorData = new List<(int, string, MonteCarloSimulationRequest, List<SimulatedLoanInfo>)>();
                foreach (var debtorId in request.DebtorIds)
                {
                    var debtor = await _dbContext.DebtorDetails.FindAsync(debtorId);
                    if (debtor == null) continue;
                    
                    // Create request with ALL simulation parameters from database settings
                    var debtorRequest = new MonteCarloSimulationRequest 
                    { 
                        DebtorId = debtorId, 
                        UseActualLoans = true, 
                        IncludeFirstLien = request.IncludeFirstLien, 
                        SimulationYears = request.SimulationYears, 
                        NumberOfSimulations = request.NumberOfSimulations, 
                        CorporateTaxRate = defaultCorporateTaxRate,
                        // Use model settings from database (same as individual mode)
                        RevenueGrowthRate = defaultRevenueGrowthRate,
                        OperatingCostGrowthRate = defaultOperatingCostGrowthRate,
                        RevenueVolatility = defaultRevenueVolatility,
                        OperatingCostVolatility = defaultOperatingCostVolatility,
                        CollateralExpectedReturn = defaultCollateralExpectedReturn,
                        CollateralVolatility = defaultCollateralVolatility,
                        CollateralCorrelation = defaultCollateralCorrelation
                    };
                    
                    var balanceSheet = await _balanceSheetCalculation.CalculateFromLineItems(debtorId);
                    var latestPL = await _dbContext.DebtorPLs.Where(pl => pl.DebtorID == debtorId).OrderByDescending(pl => pl.BookYear).FirstOrDefaultAsync();
                    if (balanceSheet == null || latestPL == null) continue;
                    debtorRequest.InitialRevenue = latestPL.Revenue;
                    debtorRequest.InitialOperatingCosts = latestPL.OperatingExpenses;
                    debtorRequest.InitialEquity = balanceSheet.Equity;
                    debtorRequest.InitialDebt = 0; // Set to 0 since we're using actual loan debt (SAME AS INDIVIDUAL MODE)
                    debtorRequest.InitialAssets = balanceSheet.TotalAssets;
                    debtorRequest.InitialLiquidAssets = balanceSheet.CurrentAssets;
                    
                    var simulatedLoans = new List<SimulatedLoanInfo>();
                    
                    // Load external loans from balance sheet line items (if requested) - SAME AS INDIVIDUAL MODE
                    if (request.IncludeFirstLien)
                    {
                        if (balanceSheet?.ExternalLoans != null && balanceSheet.ExternalLoans.Any())
                        {
                            _logger.LogInformation("Portfolio: Found {Count} external loan(s) for debtor {DebtorId}", 
                                balanceSheet.ExternalLoans.Count, debtorId);
                            
                            foreach (var externalLoan in balanceSheet.ExternalLoans)
                            {
                                if (externalLoan.InterestRate.HasValue && externalLoan.TenorMonths.HasValue)
                                {
                                    var firstLienLoan = new SimulatedLoanInfo
                                    {
                                        LoanId = -externalLoan.LineItemId,
                                        LoanAmount = externalLoan.Amount,
                                        InterestRate = externalLoan.InterestRate.Value,
                                        TenorMonths = externalLoan.TenorMonths.Value,
                                        RedemptionSchedule = externalLoan.RedemptionSchedule ?? "Annuity",
                                        CollateralValue = 0,
                                        LiquidityHaircut = 0,
                                        Subordination = 0
                                    };
                                    
                                    firstLienLoan.YearlyPayments = CalculateYearlyPaymentScheduleFromInfo(firstLienLoan, request.SimulationYears);
                                    simulatedLoans.Add(firstLienLoan);
                                    
                                    _logger.LogInformation("Portfolio: External loan added for debtor {DebtorId}: Amount={Amount}, Rate={Rate}%", 
                                        debtorId, externalLoan.Amount, externalLoan.InterestRate);
                                }
                            }
                        }
                    }
                    
                    var loans = await _dbContext.Loans.Include(l => l.LoanCollaterals).ThenInclude(lc => lc.Collateral).Where(l => l.DebtorID == debtorId).ToListAsync();
                    if (!loans.Any() && !simulatedLoans.Any()) continue;
                    
                    // Get outstanding amounts from balance sheet (SAME AS INDIVIDUAL MODE)
                    var loanIds = loans.Select(l => l.LoanID).ToList();
                    var outstandingAmounts = await _balanceSheetCalculation.GetLoanOutstandingAmounts(debtorId, loanIds);
                    int baseYear = DateTime.Now.Year;
                    
                    foreach (var loan in loans)
                    {
                        // Use outstanding amount from balance sheet if available (SAME AS INDIVIDUAL MODE)
                        decimal outstanding = outstandingAmounts.ContainsKey(loan.LoanID) 
                            ? outstandingAmounts[loan.LoanID]
                            : _loanFinancialCalculator.CalculateOutstandingBalanceAtYear(loan, baseYear);
                        
                        var collateral = _loanFinancialCalculator.GetTotalCollateralValue(loan);
                        
                        // Calculate weighted average liquidity haircut and total subordination (SAME AS INDIVIDUAL MODE)
                        decimal liquidityHaircut = 0;
                        decimal subordination = 0;
                        
                        if (loan.LoanCollaterals != null && loan.LoanCollaterals.Any())
                        {
                            decimal totalCollateralValue = 0;
                            decimal weightedHaircut = 0;
                            
                            foreach (var lc in loan.LoanCollaterals)
                            {
                                if (lc.Collateral != null)
                                {
                                    var collateralValue = lc.Collateral.AppraisalValue ?? 0;
                                    totalCollateralValue += collateralValue;
                                    weightedHaircut += collateralValue * lc.Collateral.LiquidityHaircut;
                                    subordination += lc.Collateral.FirstMortgageAmount ?? 0;
                                }
                            }
                            
                            if (totalCollateralValue > 0)
                            {
                                liquidityHaircut = weightedHaircut / totalCollateralValue;
                            }
                        }

                        // Determine primary property type for this loan's collateral (SAME AS INDIVIDUAL MODE)
                        string? propertyType = loan.LoanCollaterals
                            .Where(lc => lc.Collateral != null && !string.IsNullOrEmpty(lc.Collateral.PropertyType))
                            .Select(lc => lc.Collateral!.PropertyType)
                            .FirstOrDefault();
                        
                        simulatedLoans.Add(new SimulatedLoanInfo 
                        { 
                            LoanId = loan.LoanID, 
                            LoanAmount = outstanding,  // Use outstanding balance, not original amount
                            InterestRate = loan.AnnualInterestRate, 
                            TenorMonths = loan.TenorMonths, 
                            RedemptionSchedule = loan.RedemptionSchedule, 
                            CollateralValue = collateral,
                            LiquidityHaircut = liquidityHaircut,
                            Subordination = subordination,
                            CollateralPropertyType = propertyType,
                            YearlyPayments = CalculateYearlyPaymentSchedule(loan, request.SimulationYears) 
                        });
                    }
                    debtorRequest.LoanAmount = simulatedLoans.Sum(l => l.LoanAmount);
                    await EnrichRequestWithSectorData(debtorRequest, modelSettingsId, simulatedLoans);
                    debtorData.Add((debtorId, debtor.DebtorName, debtorRequest, simulatedLoans));
                }
                if (!debtorData.Any()) return BadRequest(new { message = "No valid debtors" });
                var result = _simulationService.RunPortfolioSimulation(debtorData, correlationMatrix, sectorVolatilities);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in portfolio simulation");
                return StatusCode(500, new { message = ex.Message });
            }
        }

    }
}
