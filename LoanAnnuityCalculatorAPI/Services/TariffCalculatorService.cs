using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class TariffCalculatorService
    {
        private readonly LoanDbContext _context;
        private readonly AnnuityCalculator _annuityCalculator;
        private readonly PaymentCalculatorService _paymentCalculator;
        private readonly EcbApiService _ecbService;

        public TariffCalculatorService(
            LoanDbContext context, 
            AnnuityCalculator annuityCalculator, 
            PaymentCalculatorService paymentCalculator,
            EcbApiService ecbService)
        {
            _context = context;
            _annuityCalculator = annuityCalculator;
            _paymentCalculator = paymentCalculator;
            _ecbService = ecbService;
        }

        public async Task<TariffCalculationResponse> CalculateTariffAsync(TariffCalculationRequest request)
        {
            var response = new TariffCalculationResponse();

            // 1. Calculate LTV
            decimal effectiveCollateral = request.CollateralValue - request.SubordinationAmount;
            effectiveCollateral = effectiveCollateral * (1 - request.LiquidityHaircut / 100);
            response.LTV = request.LoanAmount / effectiveCollateral * 100;

            // 2. Get spreads from database or use provided values
            response.BaseRate = request.BaseRate;
            response.LtvSpread = request.LtvSpread ?? await GetLtvSpreadFromDatabase(response.LTV);
            response.RatingSpread = request.RatingSpread ?? await GetRatingSpreadFromDatabase(request.CreditRating);
            response.ExtraSpread = request.ExtraSpread ?? 0;

            // 3. Calculate interest rate
            response.InterestRate = response.BaseRate + response.LtvSpread + response.RatingSpread + response.ExtraSpread;

            // 4. Generate payment schedule using the appropriate redemption scheme (moved before payment details calculation)
            var schedule = _paymentCalculator.CalculateForEntireTenor(
                request.LoanAmount,
                response.InterestRate,
                request.LoanTerm,
                request.InterestOnlyPeriod,
                request.RedemptionScheme
            );

            response.PaymentSchedule = schedule.Select(s => new PaymentScheduleItem
            {
                Month = s.month,
                InterestComponent = Math.Round(s.interestComponent, 2),
                CapitalComponent = Math.Round(s.capitalComponent, 2),
                RemainingLoan = Math.Round(s.remainingLoan, 2)
            }).ToList();

            // 5. Calculate payment details from the schedule
            var (monthlyPayment, totalInterest, totalAmount) = CalculatePaymentDetailsFromSchedule(
                request.LoanAmount,
                response.PaymentSchedule,
                request.RedemptionScheme
            );

            response.MonthlyPayment = monthlyPayment;
            response.TotalInterest = totalInterest;
            response.TotalAmount = totalAmount;

            // 6. Generate chart data
            response.ChartData = GenerateChartData(response.PaymentSchedule);

            // 7. Calculate BSE (Bruto Steun Equivalent) with detailed breakdown
            var (bse, breakdown) = await CalculateBSEWithBreakdownAsync(request, response);
            response.BSE = bse;
            response.BseBreakdown = breakdown;

            return response;
        }

        private (decimal monthlyPayment, decimal totalInterest, decimal totalAmount) CalculatePaymentDetails(
            decimal loanAmount,
            decimal interestRate,
            int loanTerm,
            int interestOnlyPeriod
        )
        {
            decimal monthlyRate = interestRate / 100 / 12;
            decimal monthlyPayment = 0;
            decimal totalInterest = 0;

            if (interestOnlyPeriod >= loanTerm)
            {
                // Entire loan is interest-only
                monthlyPayment = loanAmount * monthlyRate;
                totalInterest = monthlyPayment * loanTerm;
            }
            else
            {
                // Calculate annuity payment for the repayment period
                int repaymentMonths = loanTerm - interestOnlyPeriod;
                monthlyPayment = _annuityCalculator.CalculateMonthlyAnnuity(loanAmount, interestRate, repaymentMonths);

                // Calculate total interest
                decimal interestOnlyInterest = 0;
                if (interestOnlyPeriod > 0)
                {
                    interestOnlyInterest = loanAmount * monthlyRate * interestOnlyPeriod;
                }

                decimal repaymentInterest = (monthlyPayment * repaymentMonths) - loanAmount;
                totalInterest = interestOnlyInterest + repaymentInterest;
            }

            decimal totalAmount = loanAmount + totalInterest;

            return (
                Math.Round(monthlyPayment, 2),
                Math.Round(totalInterest, 2),
                Math.Round(totalAmount, 2)
            );
        }

        private (decimal monthlyPayment, decimal totalInterest, decimal totalAmount) CalculatePaymentDetailsFromSchedule(
            decimal loanAmount,
            List<PaymentScheduleItem> schedule,
            string redemptionScheme
        )
        {
            // Calculate total interest from the schedule
            decimal totalInterest = schedule.Sum(s => s.InterestComponent);
            decimal totalAmount = loanAmount + totalInterest;

            // Calculate "monthly payment" based on redemption scheme
            decimal monthlyPayment;
            
            switch (redemptionScheme)
            {
                case "Linear":
                    // For linear, monthly payment is the FIRST month's total payment (highest)
                    monthlyPayment = schedule.First().InterestComponent + schedule.First().CapitalComponent;
                    break;
                    
                case "Bullet":
                    // For bullet, monthly payment is the interest-only payment (until final month)
                    // Use the most common payment (excluding the last month which has the balloon)
                    if (schedule.Count > 1)
                    {
                        monthlyPayment = schedule[0].InterestComponent + schedule[0].CapitalComponent;
                    }
                    else
                    {
                        monthlyPayment = schedule.First().InterestComponent + schedule.First().CapitalComponent;
                    }
                    break;
                    
                case "Annuity":
                default:
                    // For annuity, all regular payments are the same
                    // Find the first non-interest-only payment
                    var regularPayment = schedule.FirstOrDefault(s => s.CapitalComponent > 0);
                    if (regularPayment != null)
                    {
                        monthlyPayment = regularPayment.InterestComponent + regularPayment.CapitalComponent;
                    }
                    else
                    {
                        // If all are interest-only, use the first month
                        monthlyPayment = schedule.First().InterestComponent + schedule.First().CapitalComponent;
                    }
                    break;
            }

            return (
                Math.Round(monthlyPayment, 2),
                Math.Round(totalInterest, 2),
                Math.Round(totalAmount, 2)
            );
        }

        private ChartData GenerateChartData(List<PaymentScheduleItem> schedule)
        {
            var chartData = new ChartData();

            foreach (var item in schedule)
            {
                chartData.InterestComponent.Add(item.InterestComponent);
                chartData.CapitalComponent.Add(item.CapitalComponent);
                chartData.Labels.Add($"Maand {item.Month}");
            }

            return chartData;
        }

        private async Task<decimal> GetLtvSpreadFromDatabase(decimal ltv)
        {
            // Get active tariff settings
            var settings = await _context.TariffSettings
                .Include(t => t.LtvTiers)
                .FirstOrDefaultAsync(t => t.IsActive);

            if (settings == null || !settings.LtvTiers.Any())
                return 0;

            // Find the appropriate LTV tier
            // Tiers are sorted by MaxLtv, find the first tier where LTV is less than or equal to MaxLtv
            var orderedTiers = settings.LtvTiers.OrderBy(t => t.MaxLtv).ToList();
            var tier = orderedTiers.FirstOrDefault(t => ltv <= t.MaxLtv);

            // If no tier matches (LTV > highest MaxLtv), use the highest tier (maximum spread)
            if (tier == null)
                tier = orderedTiers.LastOrDefault();

            // Convert from basis points to percentage
            return tier != null ? tier.Spread / 100m : 0;
        }

        private async Task<decimal> GetRatingSpreadFromDatabase(string creditRating)
        {
            // Get active tariff settings
            var settings = await _context.TariffSettings
                .Include(t => t.CreditRatings)
                .FirstOrDefaultAsync(t => t.IsActive);

            if (settings == null || !settings.CreditRatings.Any())
                return 0;

            // Find the appropriate rating spread
            var ratingSpread = settings.CreditRatings
                .FirstOrDefault(r => r.Rating.Equals(creditRating, StringComparison.OrdinalIgnoreCase));

            // Convert from basis points to percentage
            return ratingSpread != null ? ratingSpread.Spread / 100m : 0;
        }

        /// <summary>
        /// Calculate Bruto Steun Equivalent (BSE) with detailed yearly breakdown
        /// BSE = PV(Market Rate Payments) - PV(Subsidized Payments)
        /// Market Rate = Base Rate + Risk Premium (based on rating and collateral)
        /// Discount Rate = EU Reference Rate (1-year EURIBOR average from Sept-Nov previous year)
        /// </summary>
        private async Task<(decimal bse, BseBreakdown breakdown)> CalculateBSEWithBreakdownAsync(
            TariffCalculationRequest request, 
            TariffCalculationResponse response)
        {
            // Get the market risk premium from EU BSE matrix (rating × security level)
            var (riskPremium, _) = await GetMarketRiskPremiumBreakdown(request.CreditRating, response.LTV);

            Console.WriteLine($"BSE Calculation - IsNewCompany: {request.IsNewCompany}, Initial Risk Premium: {riskPremium}%");

            // Apply minimum 400bps spread for new companies (no financial history)
            bool minimumApplied = false;
            if (request.IsNewCompany && riskPremium < 4.00m)
            {
                Console.WriteLine($"Applying new company minimum - changing risk premium from {riskPremium}% to 4.00%");
                riskPremium = 4.00m; // Minimum 400 basis points for new companies
                minimumApplied = true;
            }

            // Get BSE reference rate from ECB (1-year EURIBOR average)
            decimal ecbBaseRate;
            decimal bseDiscountRate;
            try
            {
                var bseRateResponse = await _ecbService.GetBseReferenceRateAsync();
                ecbBaseRate = bseRateResponse.ReferenceRate; // EU reference rate (e.g., 2.21%)
                bseDiscountRate = ecbBaseRate + 1.00m; // Add 100bps spread for discounting
            }
            catch
            {
                // Fallback to base rate if ECB service fails
                ecbBaseRate = response.BaseRate;
                bseDiscountRate = response.BaseRate + 1.00m;
            }

            // Market rate = ECB reference rate + Risk premium from matrix
            decimal marketRate = ecbBaseRate + riskPremium;

            // Subsidized rate is the actual rate we're charging
            decimal subsidizedRate = response.InterestRate;

            // Determine security level for display (in Dutch)
            string securityLevel = response.LTV <= 130 ? "Hoog" : response.LTV <= 159 ? "Normaal" : "Laag";

            var breakdown = new BseBreakdown
            {
                MarketRate = Math.Round(marketRate, 4),
                LoanRate = Math.Round(subsidizedRate, 4),
                MarketRateBreakdown = new MarketRateBreakdown
                {
                    BaseRate = Math.Round(ecbBaseRate, 4), // Use ECB rate
                    RiskPremium = Math.Round(riskPremium, 4),
                    CreditRating = request.CreditRating,
                    LTV = Math.Round(response.LTV, 2),
                    SecurityLevel = securityLevel,
                    IsNewCompany = request.IsNewCompany,
                    NewCompanyMinimumApplied = minimumApplied,
                    // Keep obsolete fields for backward compatibility
                    RatingPremium = Math.Round(riskPremium, 4),
                    LtvAdjustment = 0m
                },
                YearlyBreakdown = new List<BseYearlyDetail>()
            };

            // If subsidized rate is equal or higher than market rate, there's no subsidy
            if (subsidizedRate >= marketRate)
            {
                return (0, breakdown);
            }

            // Calculate yearly breakdown using the actual payment schedule
            int totalYears = (int)Math.Ceiling(request.LoanTerm / 12.0);
            decimal monthlyMarketRate = marketRate / 100 / 12;
            decimal monthlyDiscountRate = bseDiscountRate / 100 / 12; // Use EU reference rate + 100bps for discounting

            for (int year = 1; year <= totalYears; year++)
            {
                decimal yearlyMarketInterest = 0;
                decimal yearlyLoanInterest = 0;
                decimal yearlyDiscountedDifference = 0;

                int startMonth = (year - 1) * 12 + 1;
                int endMonth = Math.Min(year * 12, request.LoanTerm);

                for (int month = startMonth; month <= endMonth; month++)
                {
                    // Get the actual payment schedule item for this month
                    var scheduleItem = response.PaymentSchedule.FirstOrDefault(s => s.Month == month);
                    if (scheduleItem == null) continue;

                    // Calculate what the market interest would be on the remaining balance
                    decimal marketInterest = scheduleItem.RemainingLoan * monthlyMarketRate;
                    
                    // The actual loan interest is from the schedule
                    decimal loanInterest = scheduleItem.InterestComponent;
                    
                    decimal monthlyDifference = marketInterest - loanInterest;

                    yearlyMarketInterest += marketInterest;
                    yearlyLoanInterest += loanInterest;

                    // Discount this month's difference to present value
                    decimal discountFactor = (decimal)Math.Pow((double)(1 + monthlyDiscountRate), month);
                    yearlyDiscountedDifference += monthlyDifference / discountFactor;
                }

                breakdown.YearlyBreakdown.Add(new BseYearlyDetail
                {
                    Year = year,
                    MarketInterest = Math.Round(yearlyMarketInterest, 2),
                    LoanInterest = Math.Round(yearlyLoanInterest, 2),
                    Difference = Math.Round(yearlyMarketInterest - yearlyLoanInterest, 2),
                    DiscountedValue = Math.Round(yearlyDiscountedDifference, 2)
                });
            }

            // Total BSE is the sum of all discounted yearly differences
            decimal totalBse = breakdown.YearlyBreakdown.Sum(y => y.DiscountedValue);

            return (Math.Round(totalBse, 2), breakdown);
        }

        /// <summary>
        /// Get market risk premium based on EU BSE methodology
        /// Uses official EU matrix table with Credit Rating (rows) and Security Level based on LTV (columns)
        /// Security levels based on LGD (Loss Given Default):
        /// - High: LGD < 30% (LTV ≤ 130%)
        /// - Normal/Medium: LGD 31-59% (LTV 131-159%)
        /// - Low: LGD ≥ 60% (LTV > 159%)
        /// Returns: (total risk premium from matrix in %, 0 for backward compatibility with breakdown display)
        /// </summary>
        private async Task<(decimal ratingPremium, decimal ltvAdjustment)> GetMarketRiskPremiumBreakdown(string creditRating, decimal ltv)
        {
            // Determine security level based on LTV
            // LGD (Loss Given Default) relationship: Higher LTV = Higher LGD = Lower security
            string securityLevel;
            if (ltv <= 130)
                securityLevel = "HIGH";      // Hoog - LGD < 30%
            else if (ltv <= 159)
                securityLevel = "NORMAL";    // Normaal - LGD 31-59%
            else
                securityLevel = "LOW";       // Laag - LGD ≥ 60%

            // Official EU BSE Risk Premium Matrix (Credit Rating × Security Level)
            // Values in basis points from EU regulation, converted to percentage
            decimal riskPremiumBps = (creditRating.ToUpper(), securityLevel) switch
            {
                // Zeer goed (AAA-A)
                ("AAA", "HIGH") => 60m,
                ("AAA", "NORMAL") => 75m,
                ("AAA", "LOW") => 100m,
                ("AA", "HIGH") => 60m,
                ("AA", "NORMAL") => 75m,
                ("AA", "LOW") => 100m,
                ("A", "HIGH") => 60m,
                ("A", "NORMAL") => 75m,
                ("A", "LOW") => 100m,
                
                // Goed (BBB)
                ("BBB", "HIGH") => 75m,
                ("BBB", "NORMAL") => 100m,
                ("BBB", "LOW") => 220m,
                
                // Bevredigend (BB)
                ("BB", "HIGH") => 100m,
                ("BB", "NORMAL") => 220m,
                ("BB", "LOW") => 400m,
                
                // Zwak (B)
                ("B", "HIGH") => 220m,
                ("B", "NORMAL") => 400m,
                ("B", "LOW") => 650m,
                
                // Slecht/Financiële moeilijkheden (CCC en lager)
                ("CCC", "HIGH") or ("CC", "HIGH") or ("C", "HIGH") => 400m,
                ("CCC", "NORMAL") or ("CC", "NORMAL") or ("C", "NORMAL") => 650m,
                ("CCC", "LOW") or ("CC", "LOW") or ("C", "LOW") => 1000m,
                
                // Legacy compatibility for old rating system (map to approximate equivalents)
                ("HIGH", "HIGH") => 60m,      // Map to AAA-A, High
                ("HIGH", "NORMAL") => 75m,
                ("HIGH", "LOW") => 100m,
                ("MEDIUM", "HIGH") => 75m,    // Map to BBB, High
                ("MEDIUM", "NORMAL") => 100m,
                ("MEDIUM", "LOW") => 220m,
                ("LOW", "HIGH") => 220m,      // Map to B, High
                ("LOW", "NORMAL") => 400m,
                ("LOW", "LOW") => 650m,
                
                // Default for unknown ratings - use BBB, Normal
                _ => 100m
            };

            // Convert basis points to percentage (100 bps = 1.00%)
            decimal riskPremium = riskPremiumBps / 100m;

            // Return the single risk premium from the matrix
            // For display purposes in the breakdown, we return it as ratingPremium with 0 ltvAdjustment
            // This maintains backward compatibility with the existing UI
            return (riskPremium, 0m);
        }

        /// <summary>
        /// Get market risk premium as a single value (for backward compatibility)
        /// </summary>
        private async Task<decimal> GetMarketRiskPremium(string creditRating, decimal ltv)
        {
            var (ratingPremium, ltvAdjustment) = await GetMarketRiskPremiumBreakdown(creditRating, ltv);
            return ratingPremium + ltvAdjustment;
        }

        /// <summary>
        /// Calculate present value of loan payments
        /// </summary>
        private decimal CalculatePresentValue(
            decimal loanAmount,
            decimal annualInterestRate,
            int loanTerm,
            int interestOnlyPeriod,
            decimal discountRate
        )
        {
            decimal monthlyRate = annualInterestRate / 100 / 12;
            decimal discountMonthlyRate = discountRate / 100 / 12;
            decimal presentValue = 0;

            // Interest-only period
            if (interestOnlyPeriod > 0)
            {
                decimal interestPayment = loanAmount * monthlyRate;
                for (int month = 1; month <= interestOnlyPeriod; month++)
                {
                    decimal pv = interestPayment / (decimal)Math.Pow((double)(1 + discountMonthlyRate), month);
                    presentValue += pv;
                }
            }

            // Repayment period
            if (loanTerm > interestOnlyPeriod)
            {
                int repaymentMonths = loanTerm - interestOnlyPeriod;
                decimal monthlyPayment = _annuityCalculator.CalculateMonthlyAnnuity(loanAmount, annualInterestRate, repaymentMonths);

                for (int month = interestOnlyPeriod + 1; month <= loanTerm; month++)
                {
                    decimal pv = monthlyPayment / (decimal)Math.Pow((double)(1 + discountMonthlyRate), month);
                    presentValue += pv;
                }
            }

            return presentValue;
        }
    }
}
