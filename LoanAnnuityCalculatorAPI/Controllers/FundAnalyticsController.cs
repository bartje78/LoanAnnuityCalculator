using LoanAnnuityCalculatorAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/fund-analytics")]
    public class FundAnalyticsController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;

        public FundAnalyticsController(LoanDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Get comprehensive fund performance metrics including interest income, repayments, and loan statistics
        /// </summary>
        /// <param name="period">Time period for data aggregation: 'yearly', 'quarterly', or 'monthly'. Default is 'yearly'.</param>
        [HttpGet("performance")]
        public async Task<IActionResult> GetFundPerformance([FromQuery] string period = "yearly")
        {
            try
            {
                // Validate period parameter
                var validPeriods = new[] { "yearly", "quarterly", "monthly" };
                if (!validPeriods.Contains(period.ToLower()))
                {
                    return BadRequest(new { message = "Invalid period. Use 'yearly', 'quarterly', or 'monthly'." });
                }

                var loans = await _dbContext.Loans
                    .Include(l => l.DebtorDetails)
                    .ToListAsync();

                var result = new
                {
                    InterestIncome = await CalculateInterestIncome(period.ToLower()),
                    Repayments = await CalculateRepayments(period.ToLower()),
                    LoanStatistics = await CalculateLoanStatistics(period.ToLower()),
                    TenorAnalysis = await CalculateTenorAnalysis(),
                    DurationAnalysis = await CalculateDurationAnalysis(),
                    Period = period.ToLower(),
                    GeneratedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while calculating fund performance.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get payment discipline analysis by vintage (loan origination year)
        /// </summary>
        [HttpGet("payment-discipline")]
        public async Task<IActionResult> GetPaymentDiscipline()
        {
            try
            {
                var result = await CalculatePaymentDisciplineByVintage();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while calculating payment discipline.", error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate interest income by specified period and cumulative since inception
        /// </summary>
        private async Task<object> CalculateInterestIncome(string period = "yearly")
        {
            var loans = await _dbContext.Loans.ToListAsync();
            var interestByPeriod = new Dictionary<string, decimal>();

            // Calculate cumulative interest SEPARATELY from period aggregation
            // This should be the total interest earned by all loans since inception
            var totalCumulativeInterest = 0m;
            foreach (var loan in loans)
            {
                var loanStartDate = loan.StartDate;
                var loanEndDate = loanStartDate.AddMonths(loan.TenorMonths);
                var currentDate = DateTime.Now;
                
                // Calculate how long this loan has been active (until end date or current date)
                var actualEndDate = loanEndDate < currentDate ? loanEndDate : currentDate;
                
                if (actualEndDate > loanStartDate)
                {
                    var outstandingAmount = CalculateAverageOutstandingForPeriod(loan, loanStartDate, actualEndDate);
                    var yearlyRate = loan.AnnualInterestRate / 100m;
                    var monthlyRate = yearlyRate / 12m;
                    var monthsActive = CalculateMonthsInPeriod(loanStartDate, actualEndDate);
                    
                    totalCumulativeInterest += outstandingAmount * monthlyRate * monthsActive;
                }
            }

            // Get all period keys based on loan dates
            var allPeriodKeys = GetAllPeriodKeys(loans, period);

            foreach (var periodKey in allPeriodKeys)
            {
                decimal periodInterest = 0m;
                var (periodStart, periodEnd) = ParsePeriodKey(periodKey, period);

                foreach (var loan in loans)
                {
                    // Only include loans that were active during this period
                    if (loan.StartDate <= periodEnd)
                    {
                        // Determine the period this loan was active
                        var activePeriodStart = loan.StartDate > periodStart ? loan.StartDate : periodStart;
                        var activePeriodEnd = periodEnd;
                        
                        // Check if loan ended before period end
                        var loanEndDate = loan.StartDate.AddMonths(loan.TenorMonths);
                        if (loanEndDate < periodEnd)
                            activePeriodEnd = loanEndDate;

                        if (activePeriodStart <= activePeriodEnd)
                        {
                            // Calculate interest for the active period
                            var outstandingAmount = CalculateAverageOutstandingForPeriod(loan, activePeriodStart, activePeriodEnd);
                            var yearlyRate = loan.AnnualInterestRate / 100m;
                            var monthlyRate = yearlyRate / 12m;
                            
                            // Calculate number of months in the active period
                            var monthsActive = CalculateMonthsInPeriod(activePeriodStart, activePeriodEnd);
                            
                            periodInterest += outstandingAmount * monthlyRate * monthsActive;
                        }
                    }
                }

                interestByPeriod[periodKey] = periodInterest;
            }

            return new
            {
                ByPeriod = interestByPeriod.Select(kvp => new { 
                    Period = kvp.Key, 
                    Amount = Math.Round(kvp.Value, 2),
                    Label = FormatPeriodLabel(kvp.Key, period)
                }).OrderBy(x => x.Period),
                CumulativeSinceInception = Math.Round(totalCumulativeInterest, 2)
            };
        }

        /// <summary>
        /// Calculate repayments by specified period and cumulative since inception
        /// </summary>
        private async Task<object> CalculateRepayments(string period = "yearly")
        {
            var loans = await _dbContext.Loans.ToListAsync();
            var repaymentsByPeriod = new Dictionary<string, decimal>();

            // Calculate cumulative repayments SEPARATELY from period aggregation
            // This should be the total principal repaid by all loans since inception
            var totalCumulativeRepayments = 0m;
            foreach (var loan in loans)
            {
                var loanStartDate = loan.StartDate;
                var loanEndDate = loanStartDate.AddMonths(loan.TenorMonths);
                var currentDate = DateTime.Now;
                
                // Calculate how long this loan has been active (until end date or current date)
                var actualEndDate = loanEndDate < currentDate ? loanEndDate : currentDate;
                
                if (actualEndDate > loanStartDate)
                {
                    var totalPrincipal = loan.LoanAmount;
                    var monthlyPrincipal = totalPrincipal / loan.TenorMonths;
                    var monthsActive = CalculateMonthsInPeriod(loanStartDate, actualEndDate);
                    
                    totalCumulativeRepayments += monthlyPrincipal * monthsActive;
                }
            }

            // Get all period keys based on loan dates
            var allPeriodKeys = GetAllPeriodKeys(loans, period);

            foreach (var periodKey in allPeriodKeys)
            {
                decimal periodRepayments = 0m;
                var (periodStart, periodEnd) = ParsePeriodKey(periodKey, period);

                foreach (var loan in loans)
                {
                    // Only include loans that were active during this period
                    if (loan.StartDate <= periodEnd)
                    {
                        // Determine the period this loan was active
                        var activePeriodStart = loan.StartDate > periodStart ? loan.StartDate : periodStart;
                        var activePeriodEnd = periodEnd;
                        
                        // Check if loan ended before period end
                        var loanEndDate = loan.StartDate.AddMonths(loan.TenorMonths);
                        if (loanEndDate < periodEnd)
                            activePeriodEnd = loanEndDate;

                        if (activePeriodStart <= activePeriodEnd)
                        {
                            // Calculate principal repayments for the active period
                            var totalPrincipal = loan.LoanAmount;
                            var monthlyPrincipal = totalPrincipal / loan.TenorMonths;
                            var monthsActive = CalculateMonthsInPeriod(activePeriodStart, activePeriodEnd);
                            
                            periodRepayments += monthlyPrincipal * monthsActive;
                        }
                    }
                }

                repaymentsByPeriod[periodKey] = periodRepayments;
            }

            return new
            {
                ByPeriod = repaymentsByPeriod.Select(kvp => new { 
                    Period = kvp.Key, 
                    Amount = Math.Round(kvp.Value, 2),
                    Label = FormatPeriodLabel(kvp.Key, period)
                }).OrderBy(x => x.Period),
                CumulativeSinceInception = Math.Round(totalCumulativeRepayments, 2)
            };
        }

        /// <summary>
        /// Calculate loan statistics by specified period including new loans, ended loans, and defaults
        /// </summary>
        private async Task<object> CalculateLoanStatistics(string period = "yearly")
        {
            var loans = await _dbContext.Loans.ToListAsync();
            var statsByPeriod = new Dictionary<string, object>();

            // Get all period keys based on loan dates
            var allPeriodKeys = GetAllPeriodKeys(loans, period);

            foreach (var periodKey in allPeriodKeys)
            {
                var (periodStart, periodEnd) = ParsePeriodKey(periodKey, period);

                var newLoans = loans.Count(l => l.StartDate >= periodStart && l.StartDate <= periodEnd);
                var endedLoans = loans.Count(l => 
                {
                    var loanEndDate = l.StartDate.AddMonths(l.TenorMonths);
                    return loanEndDate >= periodStart && loanEndDate <= periodEnd;
                });
                
                // Count defaults that occurred during this period
                var defaultedLoans = loans.Count(l => 
                    (l.Status != null && l.Status.ToLower().Contains("default")) &&
                    l.StartDate <= periodEnd);

                // Calculate total loan amounts issued in this period
                var newLoanAmount = loans
                    .Where(l => l.StartDate >= periodStart && l.StartDate <= periodEnd)
                    .Sum(l => l.LoanAmount);

                statsByPeriod[periodKey] = new
                {
                    NewLoans = newLoans,
                    EndedLoans = endedLoans,
                    DefaultedLoans = defaultedLoans,
                    NewLoanAmount = newLoanAmount
                };
            }

            return new
            {
                ByPeriod = statsByPeriod.Select(kvp => new 
                { 
                    Period = kvp.Key,
                    Label = FormatPeriodLabel(kvp.Key, period),
                    NewLoans = ((dynamic)kvp.Value).NewLoans,
                    EndedLoans = ((dynamic)kvp.Value).EndedLoans,
                    DefaultedLoans = ((dynamic)kvp.Value).DefaultedLoans,
                    NewLoanAmount = ((dynamic)kvp.Value).NewLoanAmount
                }).OrderBy(x => x.Period),
                Total = new
                {
                    TotalLoansIssued = loans.Count,
                    TotalActiveLoans = loans.Count(l => l.Status == "Aktief" || l.Status == "Active"),
                    TotalEndedLoans = loans.Count(l => l.Status == "Afgelost" || l.Status == "Ended"),
                    TotalDefaultedLoans = loans.Count(l => l.Status != null && l.Status.ToLower().Contains("default"))
                }
            };
        }

        /// <summary>
        /// Calculate average outstanding amount for a loan in a given year
        /// </summary>
        private decimal CalculateAverageOutstandingForYear(Models.Loan.Loan loan, int year)
        {
            var yearStart = new DateTime(year, 1, 1);
            var yearEnd = new DateTime(year, 12, 31);
            
            // Calculate outstanding at start and end of year
            var monthsFromStartToYearStart = Math.Max(0, ((yearStart.Year - loan.StartDate.Year) * 12) + yearStart.Month - loan.StartDate.Month);
            var monthsFromStartToYearEnd = Math.Max(0, ((yearEnd.Year - loan.StartDate.Year) * 12) + yearEnd.Month - loan.StartDate.Month);
            
            // Simple linear amortization assumption
            var totalPrincipal = loan.LoanAmount;
            var monthlyPrincipal = totalPrincipal / loan.TenorMonths;
            
            var outstandingAtYearStart = Math.Max(0, totalPrincipal - (monthlyPrincipal * monthsFromStartToYearStart));
            var outstandingAtYearEnd = Math.Max(0, totalPrincipal - (monthlyPrincipal * monthsFromStartToYearEnd));
            
            return (outstandingAtYearStart + outstandingAtYearEnd) / 2;
        }

        /// <summary>
        /// Calculate loan tenor distribution and weighted average tenor
        /// </summary>
        private async Task<object> CalculateTenorAnalysis()
        {
            // Get all loans and calculate status like LoanController does
            var allLoans = await _dbContext.Loans.ToListAsync();
            
            // Apply the same status calculation logic as LoanController
            foreach (var loan in allLoans)
            {
                if (string.IsNullOrEmpty(loan.Status))
                {
                    var monthsDifference = (DateTime.Now.Year - loan.StartDate.Year) * 12 + DateTime.Now.Month - loan.StartDate.Month;
                    loan.Status = monthsDifference <= loan.TenorMonths ? "Aktief" : "Afgelost";
                }
            }
            
            var loans = allLoans
                .Where(l => l.Status == "Active" || l.Status == "Aktief")
                .ToList();

            if (!loans.Any())
            {
                return new
                {
                    TenorDistribution = new object[0],
                    WeightedAverageTenor = 0.0,
                    AverageRemainingTenor = 0.0
                };
            }

            // Create tenor buckets
            var tenorBuckets = new[]
            {
                new { Range = "0-24 maanden", Min = 0, Max = 24 },
                new { Range = "2-5 jaar", Min = 25, Max = 60 },
                new { Range = "5-10 jaar", Min = 61, Max = 120 },
                new { Range = "10-15 jaar", Min = 121, Max = 180 },
                new { Range = "15-20 jaar", Min = 181, Max = 240 },
                new { Range = "20-25 jaar", Min = 241, Max = 300 },
                new { Range = "25+ jaar", Min = 301, Max = int.MaxValue }
            };

            var tenorDistribution = tenorBuckets.Select(bucket => new
            {
                TenorRange = bucket.Range,
                LoanCount = loans.Count(l => l.TenorMonths >= bucket.Min && l.TenorMonths <= bucket.Max),
                TotalAmount = loans.Where(l => l.TenorMonths >= bucket.Min && l.TenorMonths <= bucket.Max)
                                  .Sum(l => l.OutstandingAmount),
                AverageLoanSize = loans.Where(l => l.TenorMonths >= bucket.Min && l.TenorMonths <= bucket.Max)
                                      .DefaultIfEmpty()
                                      .Average(l => l?.OutstandingAmount ?? 0)
            }).ToArray();

            // Calculate weighted average tenor (weighted by outstanding amount)
            var totalAmount = loans.Sum(l => l.OutstandingAmount);
            var weightedAverageTenor = totalAmount > 0 ? loans.Sum(l => (l.TenorMonths * l.OutstandingAmount)) / totalAmount : 0;

            // Calculate average remaining tenor
            var currentDate = DateTime.UtcNow;
            var averageRemainingTenor = loans.Average(l => {
                var monthsElapsed = ((currentDate.Year - l.StartDate.Year) * 12) + currentDate.Month - l.StartDate.Month;
                return Math.Max(0, l.TenorMonths - monthsElapsed);
            });

            return new
            {
                TenorDistribution = tenorDistribution,
                WeightedAverageTenor = Math.Round(weightedAverageTenor, 2),
                AverageRemainingTenor = Math.Round(averageRemainingTenor, 2),
                TotalActiveLoans = loans.Count,
                TotalPortfolioValue = totalAmount
            };
        }

        /// <summary>
        /// Calculate modified duration and interest rate sensitivity analysis
        /// </summary>
        private async Task<object> CalculateDurationAnalysis()
        {
            // Get all loans and calculate status like LoanController does
            var allLoans = await _dbContext.Loans.ToListAsync();
            
            // Apply the same status calculation logic as LoanController
            foreach (var loan in allLoans)
            {
                if (string.IsNullOrEmpty(loan.Status))
                {
                    var monthsDifference = (DateTime.Now.Year - loan.StartDate.Year) * 12 + DateTime.Now.Month - loan.StartDate.Month;
                    loan.Status = monthsDifference <= loan.TenorMonths ? "Aktief" : "Afgelost";
                }
            }
            
            var loans = allLoans
                .Where(l => l.Status == "Active" || l.Status == "Aktief")
                .ToList();

            if (!loans.Any())
            {
                return new
                {
                    WeightedAverageDuration = 0.0,
                    PortfolioModifiedDuration = 0.0,
                    InterestRateSensitivity = new { OnePercent = 0.0, TwoPercent = 0.0, FivePercent = 0.0 },
                    DurationDistribution = new object[0]
                };
            }

            var totalPortfolioValue = 0.0;
            var weightedDurationSum = 0.0;
            var currentDate = DateTime.UtcNow;

            var loanDurations = new List<(int LoanId, double OutstandingAmount, int RemainingMonths, double ModifiedDuration, decimal InterestRate)>();

            foreach (var loan in loans)
            {
                var outstandingAmount = (double)loan.OutstandingAmount;
                var remainingMonths = Math.Max(0, loan.TenorMonths - 
                    ((currentDate.Year - loan.StartDate.Year) * 12 + currentDate.Month - loan.StartDate.Month));

                // Calculate modified duration based on redemption schedule type
                var annualRate = (double)loan.AnnualInterestRate / 100.0;
                var monthlyRate = annualRate / 12.0;
                
                double modifiedDuration;
                
                if (monthlyRate > 0 && remainingMonths > 0)
                {
                    // Calculate Macaulay duration based on redemption schedule
                    double macaulayDuration;
                    
                    switch (loan.RedemptionSchedule)
                    {
                        case "Linear":
                            // For linear amortization: fixed principal + decreasing interest
                            // Approximate using weighted average of payment times
                            var linearPrincipal = outstandingAmount / remainingMonths;
                            var weightedTimeSum = 0.0;
                            var totalCashFlow = 0.0;
                            var remaining = outstandingAmount;
                            
                            for (int month = 1; month <= remainingMonths; month++)
                            {
                                var interest = remaining * monthlyRate;
                                var cashFlow = linearPrincipal + interest;
                                var pv = cashFlow / Math.Pow(1 + monthlyRate, month);
                                weightedTimeSum += month * pv;
                                totalCashFlow += pv;
                                remaining -= linearPrincipal;
                            }
                            macaulayDuration = weightedTimeSum / totalCashFlow;
                            break;
                            
                        case "Bullet":
                            // For bullet loans: only interest payments until maturity, then principal
                            // Duration is close to the maturity for bullet bonds
                            var bulletWeightedSum = 0.0;
                            var bulletTotalPV = 0.0;
                            
                            // Interest payments each month
                            for (int month = 1; month <= remainingMonths; month++)
                            {
                                var interestPayment = outstandingAmount * monthlyRate;
                                var pv = interestPayment / Math.Pow(1 + monthlyRate, month);
                                bulletWeightedSum += month * pv;
                                bulletTotalPV += pv;
                            }
                            
                            // Principal payment at maturity
                            var principalPV = outstandingAmount / Math.Pow(1 + monthlyRate, remainingMonths);
                            bulletWeightedSum += remainingMonths * principalPV;
                            bulletTotalPV += principalPV;
                            
                            macaulayDuration = bulletWeightedSum / bulletTotalPV;
                            break;
                            
                        case "Annuity":
                        default:
                            // For annuity: equal payments over time
                            // Using standard Macaulay duration formula for annuity
                            macaulayDuration = (1 + monthlyRate) * 
                                (1 - Math.Pow(1 + monthlyRate, -remainingMonths)) / monthlyRate;
                            break;
                    }
                    
                    modifiedDuration = macaulayDuration / (1 + monthlyRate);
                    // Convert to years
                    modifiedDuration = modifiedDuration / 12.0;
                }
                else
                {
                    // For zero-coupon or very short-term loans
                    modifiedDuration = remainingMonths / 12.0;
                }

                totalPortfolioValue += outstandingAmount;
                weightedDurationSum += modifiedDuration * outstandingAmount;

                loanDurations.Add((loan.LoanID, outstandingAmount, remainingMonths, 
                    modifiedDuration, loan.AnnualInterestRate));
            }

            var portfolioModifiedDuration = totalPortfolioValue > 0 ? 
                weightedDurationSum / totalPortfolioValue : 0;

            // Calculate interest rate sensitivity (price sensitivity to rate changes)
            var onePercentSensitivity = -portfolioModifiedDuration * 0.01 * totalPortfolioValue;
            var twoPercentSensitivity = -portfolioModifiedDuration * 0.02 * totalPortfolioValue;
            var fivePercentSensitivity = -portfolioModifiedDuration * 0.05 * totalPortfolioValue;

            // Duration distribution buckets
            var durationBuckets = new[]
            {
                new { Range = "0-2 jaar", Min = 0.0, Max = 2.0 },
                new { Range = "2-5 jaar", Min = 2.0, Max = 5.0 },
                new { Range = "5-10 jaar", Min = 5.0, Max = 10.0 },
                new { Range = "10-15 jaar", Min = 10.0, Max = 15.0 },
                new { Range = "15-20 jaar", Min = 15.0, Max = 20.0 },
                new { Range = "20-25 jaar", Min = 20.0, Max = 25.0 },
                new { Range = "25+ jaar", Min = 25.0, Max = double.MaxValue }
            };

            var durationDistribution = durationBuckets.Select(bucket => {
                var loansInBucket = loanDurations.Where(l => 
                    l.ModifiedDuration >= bucket.Min && 
                    (bucket.Max == double.MaxValue ? true : l.ModifiedDuration < bucket.Max)).ToList();
                
                var bucketAmount = loansInBucket.Sum(l => l.OutstandingAmount);
                
                return new
                {
                    DurationRange = bucket.Range,
                    LoanCount = loansInBucket.Count,
                    TotalAmount = Math.Round(bucketAmount, 2),
                    PercentageOfPortfolio = totalPortfolioValue > 0 ? 
                        Math.Round(bucketAmount / totalPortfolioValue * 100, 2) : 0
                };
            }).ToArray();

            return new
            {
                WeightedAverageDuration = Math.Round(portfolioModifiedDuration, 2),
                PortfolioModifiedDuration = Math.Round(portfolioModifiedDuration, 2),
                InterestRateSensitivity = new
                {
                    OnePercent = Math.Round(onePercentSensitivity, 2),
                    TwoPercent = Math.Round(twoPercentSensitivity, 2),
                    FivePercent = Math.Round(fivePercentSensitivity, 2)
                },
                DurationDistribution = durationDistribution,
                TotalPortfolioValue = Math.Round(totalPortfolioValue, 2),
                AveragePortfolioYield = loans.Any() ? Math.Round((double)loans.Average(l => l.AnnualInterestRate), 2) : 0
            };
        }

        /// <summary>
        /// Calculate current outstanding amount for a loan
        /// </summary>
        private double CalculateCurrentOutstanding(dynamic loan, DateTime currentDate)
        {
            var monthsElapsed = Math.Max(0, ((currentDate.Year - loan.StartDate.Year) * 12) + 
                currentDate.Month - loan.StartDate.Month);
            
            // Simple linear amortization
            var monthlyPrincipal = (double)loan.LoanAmount / (double)loan.TenorMonths;
            var principalRepaid = Math.Min(monthlyPrincipal * monthsElapsed, (double)loan.LoanAmount);
            
            return Math.Max(0, (double)loan.LoanAmount - principalRepaid);
        }

        /// <summary>
        /// Get all period keys for the specified period type based on loan dates
        /// </summary>
        private List<string> GetAllPeriodKeys(IEnumerable<dynamic> loans, string period)
        {
            var allKeys = new HashSet<string>();
            var currentDate = DateTime.Now;

            foreach (var loan in loans)
            {
                var startDate = loan.StartDate;
                var endDate = loan.StartDate.AddMonths(loan.TenorMonths);
                
                // Generate period keys from loan start to loan end (or current date if still active)
                var actualEndDate = endDate < currentDate ? endDate : currentDate;
                
                var date = startDate;
                while (date <= actualEndDate)
                {
                    allKeys.Add(GeneratePeriodKey(date, period));
                    
                    // Move to next period
                    date = period switch
                    {
                        "monthly" => date.AddMonths(1),
                        "quarterly" => date.AddMonths(3),
                        "yearly" => date.AddYears(1),
                        _ => date.AddYears(1)
                    };
                }
            }

            return allKeys.OrderBy(k => k).ToList();
        }

        /// <summary>
        /// Generate a period key for a given date and period type
        /// </summary>
        private string GeneratePeriodKey(DateTime date, string period)
        {
            return period switch
            {
                "monthly" => $"{date.Year:0000}-{date.Month:00}",
                "quarterly" => $"{date.Year:0000}-Q{((date.Month - 1) / 3) + 1}",
                "yearly" => $"{date.Year:0000}",
                _ => $"{date.Year:0000}"
            };
        }

        /// <summary>
        /// Parse a period key to get start and end dates
        /// </summary>
        private (DateTime periodStart, DateTime periodEnd) ParsePeriodKey(string periodKey, string period)
        {
            return period switch
            {
                "monthly" => ParseMonthlyKey(periodKey),
                "quarterly" => ParseQuarterlyKey(periodKey),
                "yearly" => ParseYearlyKey(periodKey),
                _ => ParseYearlyKey(periodKey)
            };
        }

        private (DateTime start, DateTime end) ParseMonthlyKey(string key)
        {
            var parts = key.Split('-');
            var year = int.Parse(parts[0]);
            var month = int.Parse(parts[1]);
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }

        private (DateTime start, DateTime end) ParseQuarterlyKey(string key)
        {
            var parts = key.Split('-');
            var year = int.Parse(parts[0]);
            var quarter = int.Parse(parts[1].Replace("Q", ""));
            var month = (quarter - 1) * 3 + 1;
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(3).AddDays(-1);
            return (start, end);
        }

        private (DateTime start, DateTime end) ParseYearlyKey(string key)
        {
            var year = int.Parse(key);
            var start = new DateTime(year, 1, 1);
            var end = new DateTime(year, 12, 31);
            return (start, end);
        }

        /// <summary>
        /// Calculate average outstanding amount for a loan during a specific period
        /// </summary>
        private decimal CalculateAverageOutstandingForPeriod(dynamic loan, DateTime periodStart, DateTime periodEnd)
        {
            // For simplicity, calculate outstanding at the midpoint of the period
            var midPoint = periodStart.AddDays((periodEnd - periodStart).TotalDays / 2);
            var monthsElapsed = Math.Max(0, ((midPoint.Year - loan.StartDate.Year) * 12) + 
                midPoint.Month - loan.StartDate.Month);
            
            var monthlyPrincipal = loan.LoanAmount / (decimal)loan.TenorMonths;
            var principalRepaid = Math.Min(monthlyPrincipal * monthsElapsed, loan.LoanAmount);
            
            return Math.Max(0, loan.LoanAmount - principalRepaid);
        }

        /// <summary>
        /// Calculate number of months in a period (fractional months allowed)
        /// </summary>
        private decimal CalculateMonthsInPeriod(DateTime start, DateTime end)
        {
            var totalDays = (end - start).TotalDays + 1; // +1 to include both start and end dates
            return (decimal)(totalDays / 30.44); // Average days per month
        }

        /// <summary>
        /// Format period label for display
        /// </summary>
        private string FormatPeriodLabel(string periodKey, string period)
        {
            return period switch
            {
                "monthly" => FormatMonthlyLabel(periodKey),
                "quarterly" => FormatQuarterlyLabel(periodKey),
                "yearly" => periodKey,
                _ => periodKey
            };
        }

        private string FormatMonthlyLabel(string key)
        {
            var parts = key.Split('-');
            var year = int.Parse(parts[0]);
            var month = int.Parse(parts[1]);
            var date = new DateTime(year, month, 1);
            return date.ToString("MMM yyyy");
        }

        private string FormatQuarterlyLabel(string key)
        {
            var parts = key.Split('-');
            var year = parts[0];
            var quarter = parts[1];
            return $"{quarter} {year}";
        }

        /// <summary>
        /// Calculate payment discipline analysis by vintage (loan origination year)
        /// </summary>
        private async Task<object> CalculatePaymentDisciplineByVintage()
        {
            var loans = await _dbContext.Loans.ToListAsync();
            var payments = await _dbContext.LoanPayments.ToListAsync();

            // Group loans by vintage (origination year)
            var vintages = loans.GroupBy(l => l.StartDate.Year).OrderBy(g => g.Key);
            
            var vintageAnalysis = new List<object>();

            foreach (var vintage in vintages)
            {
                var vintageYear = vintage.Key;
                var vintageLoans = vintage.ToList();
                
                // Get all payments for loans in this vintage
                var vintagePayments = payments.Where(p => vintageLoans.Any(l => l.LoanID == p.LoanId)).ToList();
                
                // Calculate timeline data for this vintage (monthly)
                var timelineData = new List<object>();
                
                // Get the range of months to analyze (from first loan start to current date)
                var startDate = vintageLoans.Min(l => l.StartDate);
                var endDate = DateTime.Now;
                
                var currentMonth = new DateTime(startDate.Year, startDate.Month, 1);
                
                while (currentMonth <= endDate)
                {
                    var monthPayments = vintagePayments.Where(p => 
                        p.DueDate.Year == currentMonth.Year && 
                        p.DueDate.Month == currentMonth.Month).ToList();
                    
                    if (monthPayments.Any())
                    {
                        // Calculate value-weighted average days late
                        var totalAmount = monthPayments.Sum(p => p.TotalAmount);
                        var weightedDaysLate = monthPayments.Sum(p => 
                        {
                            var daysLate = p.PaymentDate.HasValue ? 
                                Math.Max(0, (p.PaymentDate.Value - p.DueDate).Days) : 
                                Math.Max(0, (DateTime.Now - p.DueDate).Days);
                            return daysLate * p.TotalAmount;
                        }) / totalAmount;
                        
                        timelineData.Add(new
                        {
                            Month = currentMonth.ToString("yyyy-MM"),
                            Label = currentMonth.ToString("MMM yyyy"),
                            PaymentCount = monthPayments.Count,
                            TotalAmount = totalAmount,
                            WeightedAverageDaysLate = Math.Round(weightedDaysLate, 1),
                            OnTimePayments = monthPayments.Count(p => p.PaymentDate.HasValue && (p.PaymentDate.Value - p.DueDate).Days <= 0),
                            LatePayments = monthPayments.Count(p => p.PaymentDate.HasValue && (p.PaymentDate.Value - p.DueDate).Days > 0),
                            MissedPayments = monthPayments.Count(p => !p.PaymentDate.HasValue && (DateTime.Now - p.DueDate).Days > 30)
                        });
                    }
                    
                    currentMonth = currentMonth.AddMonths(1);
                }
                
                // Calculate vintage summary statistics
                var allVintagePayments = vintagePayments.Where(p => p.DueDate <= DateTime.Now).ToList();
                var totalVintageAmount = allVintagePayments.Sum(p => p.TotalAmount);
                var overallWeightedDaysLate = allVintagePayments.Any() && totalVintageAmount > 0 ? 
                    allVintagePayments.Sum(p => 
                    {
                        var daysLate = p.PaymentDate.HasValue ? 
                            Math.Max(0, (p.PaymentDate.Value - p.DueDate).Days) : 
                            Math.Max(0, (DateTime.Now - p.DueDate).Days);
                        return daysLate * p.TotalAmount;
                    }) / totalVintageAmount : 0;
                
                vintageAnalysis.Add(new
                {
                    Vintage = vintageYear,
                    LoanCount = vintageLoans.Count,
                    TotalLoanAmount = vintageLoans.Sum(l => l.LoanAmount),
                    PaymentTimeline = timelineData,
                    Summary = new
                    {
                        TotalPayments = allVintagePayments.Count,
                        TotalPaymentAmount = totalVintageAmount,
                        OverallWeightedAverageDaysLate = Math.Round(overallWeightedDaysLate, 1),
                        OnTimePaymentRate = allVintagePayments.Any() ? 
                            (double)allVintagePayments.Count(p => p.PaymentDate.HasValue && (p.PaymentDate.Value - p.DueDate).Days <= 0) / allVintagePayments.Count * 100 : 0,
                        AveragePaymentAmount = allVintagePayments.Any() ? allVintagePayments.Average(p => p.TotalAmount) : 0
                    }
                });
            }

            return new
            {
                VintageAnalysis = vintageAnalysis,
                GeneratedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Get comprehensive collateral breakdown analysis showing loan amounts by collateral type and property type
        /// </summary>
        [HttpGet("collateral-breakdown")]
        public async Task<IActionResult> GetCollateralBreakdown()
        {
            try
            {
                // Get all loans and calculate status like LoanController does
                var allLoans = await _dbContext.Loans.ToListAsync();
                
                // Apply the same status calculation logic as LoanController
                foreach (var loan in allLoans)
                {
                    if (string.IsNullOrEmpty(loan.Status))
                    {
                        var monthsDifference = (DateTime.Now.Year - loan.StartDate.Year) * 12 + DateTime.Now.Month - loan.StartDate.Month;
                        loan.Status = monthsDifference <= loan.TenorMonths ? "Aktief" : "Afgelost";
                    }
                }
                
                var loans = allLoans
                    .Where(l => l.Status == "Active" || l.Status == "Aktief")
                    .ToList();

                // Now load the related data for the filtered loans
                var loanIds = loans.Select(l => l.LoanID).ToList();
                var loansWithCollaterals = await _dbContext.Loans
                    .Where(l => loanIds.Contains(l.LoanID))
                    .Include(l => l.LoanCollaterals)
                    .ThenInclude(lc => lc.Collateral)
                    .ToListAsync();

                // Apply status calculation to the loans with collaterals too
                foreach (var loan in loansWithCollaterals)
                {
                    if (string.IsNullOrEmpty(loan.Status))
                    {
                        var monthsDifference = (DateTime.Now.Year - loan.StartDate.Year) * 12 + DateTime.Now.Month - loan.StartDate.Month;
                        loan.Status = monthsDifference <= loan.TenorMonths ? "Aktief" : "Afgelost";
                    }
                }

                // Use the loans with collaterals for the rest of the calculations
                loans = loansWithCollaterals;

                // Calculate total outstanding loans
                var totalOutstanding = loans.Sum(l => l.OutstandingAmount);

                // Separate secured and unsecured loans
                var securedLoans = loans.Where(l => l.LoanCollaterals != null && l.LoanCollaterals.Any()).ToList();
                var unsecuredLoans = loans.Where(l => l.LoanCollaterals == null || !l.LoanCollaterals.Any()).ToList();

                var totalSecured = securedLoans.Sum(l => l.OutstandingAmount);
                var totalUnsecured = unsecuredLoans.Sum(l => l.OutstandingAmount);

                // Analyze collateral types
                var collateralTypeBreakdown = securedLoans
                    .SelectMany(l => l.LoanCollaterals!.Where(lc => lc.Collateral != null).Select(lc => new { Loan = l, Collateral = lc.Collateral! }))
                    .GroupBy(x => x.Collateral.CollateralType ?? "Unknown")
                    .Select(g => new
                    {
                        CollateralType = g.Key,
                        Count = g.Count(),
                        TotalLoanAmount = g.Sum(x => x.Loan.OutstandingAmount),
                        Percentage = totalOutstanding > 0 ? (double)(g.Sum(x => x.Loan.OutstandingAmount) / totalOutstanding * 100) : 0,
                        AverageAppraisalValue = g.Average(x => x.Collateral?.AppraisalValue ?? 0)
                    })
                    .OrderByDescending(x => x.TotalLoanAmount)
                    .ToList();

                // Analyze property types for real estate collateral
                var propertyTypeBreakdown = securedLoans
                    .SelectMany(l => l.LoanCollaterals!.Where(lc => lc.Collateral != null).Select(lc => new { Loan = l, Collateral = lc.Collateral! }))
                    .Where(x => !string.IsNullOrEmpty(x.Collateral?.PropertyType))
                    .GroupBy(x => x.Collateral.PropertyType!)
                    .Select(g => new
                    {
                        PropertyType = g.Key,
                        Count = g.Count(),
                        TotalLoanAmount = g.Sum(x => x.Loan.OutstandingAmount),
                        Percentage = totalOutstanding > 0 ? (double)(g.Sum(x => x.Loan.OutstandingAmount) / totalOutstanding * 100) : 0,
                        AverageAppraisalValue = g.Average(x => x.Collateral?.AppraisalValue ?? 0)
                    })
                    .OrderByDescending(x => x.TotalLoanAmount)
                    .ToList();

                // Create a list to hold all collateral types including unsecured
                var allCollateralTypes = new List<object>(collateralTypeBreakdown);

                // Add unsecured loans to collateral type breakdown
                if (totalUnsecured > 0)
                {
                    allCollateralTypes.Add(new
                    {
                        CollateralType = "Unsecured",
                        Count = unsecuredLoans.Count,
                        TotalLoanAmount = totalUnsecured,
                        Percentage = totalOutstanding > 0 ? (double)(totalUnsecured / totalOutstanding * 100) : 0,
                        AverageAppraisalValue = 0.0
                    });
                }

                return Ok(new
                {
                    TotalPortfolioValue = totalOutstanding,
                    Summary = new
                    {
                        TotalOutstanding = totalOutstanding,
                        TotalSecured = totalSecured,
                        TotalUnsecured = totalUnsecured,
                        SecuredPercentage = totalOutstanding > 0 ? (double)(totalSecured / totalOutstanding * 100) : 0,
                        UnsecuredPercentage = totalOutstanding > 0 ? (double)(totalUnsecured / totalOutstanding * 100) : 0,
                        SecuredLoanCount = securedLoans.Count,
                        UnsecuredLoanCount = unsecuredLoans.Count
                    },
                    CollateralTypes = allCollateralTypes,
                    PropertyTypes = propertyTypeBreakdown,
                    GeneratedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while calculating collateral breakdown.", error = ex.Message });
            }
        }
    }
}