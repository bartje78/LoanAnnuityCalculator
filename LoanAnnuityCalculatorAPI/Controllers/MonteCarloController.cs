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

        public MonteCarloController(
            MonteCarloSimulationService simulationService,
            ILogger<MonteCarloController> logger,
            LoanDbContext dbContext,
            PaymentCalculatorService paymentCalculator,
            LoanDateHelper loanDateHelper,
            BalanceSheetCalculationService balanceSheetCalculation)
        {
            _simulationService = simulationService;
            _logger = logger;
            _dbContext = dbContext;
            _loanFinancialCalculator = new LoanFinancialCalculatorService();
            _paymentCalculator = paymentCalculator;
            _loanDateHelper = loanDateHelper;
            _balanceSheetCalculation = balanceSheetCalculation;
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

                // Note: Model settings (volatilities, growth rates, collateral parameters) 
                // are now passed directly in the request from the frontend

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

    }
}
