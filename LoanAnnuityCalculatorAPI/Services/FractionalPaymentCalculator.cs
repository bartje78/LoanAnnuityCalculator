using System;
using LoanAnnuityCalculatorAPI.Models;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class FractionalPaymentCalculator
    {
        private readonly AnnuityCalculator _annuityCalculator;

        public FractionalPaymentCalculator(AnnuityCalculator annuityCalculator)
        {
            _annuityCalculator = annuityCalculator;
        }

        /// <summary>
        /// Calculate the period difference including fractional months based on invoice day
        /// </summary>
        public decimal CalculatePeriodDifference(DateTime loanStartDate, int invoiceDay, DateTime? referenceDate = null)
        {
            DateTime currentDate = referenceDate ?? DateTime.Now;
            
            // Create the invoice date for the current month
            DateTime currentInvoiceDate = new DateTime(currentDate.Year, currentDate.Month, Math.Min(invoiceDay, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)));
            
            // If the current invoice date hasn't passed yet, use the previous month's invoice
            if (currentInvoiceDate > currentDate)
            {
                currentInvoiceDate = currentInvoiceDate.AddMonths(-1);
                currentInvoiceDate = new DateTime(currentInvoiceDate.Year, currentInvoiceDate.Month, Math.Min(invoiceDay, DateTime.DaysInMonth(currentInvoiceDate.Year, currentInvoiceDate.Month)));
            }

            // If loan start day matches invoice day, it's simple - just count the months
            if (loanStartDate.Day == invoiceDay)
            {
                decimal monthsDifference = (currentInvoiceDate.Year - loanStartDate.Year) * 12 + (currentInvoiceDate.Month - loanStartDate.Month);
                return Math.Max(0, monthsDifference);
            }

            // For loans that don't start on the invoice day, we need to handle the first partial period
            // Find the first invoice date after loan start
            DateTime firstInvoiceDate = new DateTime(loanStartDate.Year, loanStartDate.Month, Math.Min(invoiceDay, DateTime.DaysInMonth(loanStartDate.Year, loanStartDate.Month)));
            
            if (firstInvoiceDate <= loanStartDate)
            {
                firstInvoiceDate = firstInvoiceDate.AddMonths(1);
                firstInvoiceDate = new DateTime(firstInvoiceDate.Year, firstInvoiceDate.Month, Math.Min(invoiceDay, DateTime.DaysInMonth(firstInvoiceDate.Year, firstInvoiceDate.Month)));
            }

            // If we haven't reached the first invoice date yet, return fractional period for the first partial month
            if (currentInvoiceDate <= firstInvoiceDate)
            {
                int daysInFirstMonth = DateTime.DaysInMonth(loanStartDate.Year, loanStartDate.Month);
                int daysFromStartToCurrentInvoice = (currentInvoiceDate - loanStartDate).Days;
                decimal firstMonthFraction = (decimal)daysFromStartToCurrentInvoice / daysInFirstMonth;
                return Math.Max(0, firstMonthFraction);
            }

            // After the first invoice, count full months from the first invoice date
            decimal fullMonthsAfterFirst = (currentInvoiceDate.Year - firstInvoiceDate.Year) * 12 + (currentInvoiceDate.Month - firstInvoiceDate.Month);
            
            // Add the initial fractional period (which is always less than 1)
            int daysInStartMonth = DateTime.DaysInMonth(loanStartDate.Year, loanStartDate.Month);
            int daysInFirstPeriod = (firstInvoiceDate - loanStartDate).Days;
            decimal firstPeriodFraction = (decimal)daysInFirstPeriod / daysInStartMonth;
            
            return Math.Max(0, firstPeriodFraction + fullMonthsAfterFirst);
        }

        /// <summary>
        /// Calculate the period difference for a specific next payment, considering loan end date
        /// This version is used when we need to know the exact period for the NEXT invoice
        /// </summary>
        public decimal CalculatePeriodDifferenceForNextPayment(DateTime loanStartDate, int tenorMonths, int invoiceDay, DateTime? referenceDate = null)
        {
            DateTime currentDate = referenceDate ?? DateTime.Now;
            DateTime loanEndDate = loanStartDate.AddMonths(tenorMonths);
            DateTime nextInvoiceDate = CalculateNextInvoiceDate(loanStartDate, invoiceDay, currentDate);
            
            // If next invoice is after loan end, we need to calculate a final fractional payment
            if (nextInvoiceDate >= loanEndDate)
            {
                // Calculate period difference up to loan end date instead of next invoice
                return CalculatePeriodDifference(loanStartDate, invoiceDay, loanEndDate);
            }
            
            // Otherwise, use the regular calculation based on next invoice date
            return CalculatePeriodDifference(loanStartDate, invoiceDay, nextInvoiceDate);
        }

        /// <summary>
        /// Calculate the next invoice date based on loan start date and invoice day setting
        /// </summary>
        public DateTime CalculateNextInvoiceDate(DateTime loanStartDate, int invoiceDay, DateTime? referenceDate = null)
        {
            DateTime currentDate = referenceDate ?? DateTime.Now;
            
            // Calculate current invoice date
            DateTime currentInvoiceDate = new DateTime(currentDate.Year, currentDate.Month, Math.Min(invoiceDay, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)));
            
            // If the current invoice date hasn't passed yet, that's our next invoice date
            if (currentInvoiceDate >= currentDate)
            {
                return currentInvoiceDate;
            }
            
            // Otherwise, next invoice is next month
            DateTime nextInvoiceDate = currentInvoiceDate.AddMonths(1);
            return new DateTime(nextInvoiceDate.Year, nextInvoiceDate.Month, Math.Min(invoiceDay, DateTime.DaysInMonth(nextInvoiceDate.Year, nextInvoiceDate.Month)));
        }

        /// <summary>
        /// Calculate fractional interest and capital for a specific period
        /// </summary>
        public FractionalPaymentResult CalculateFractionalPayment(
            decimal loanAmount,
            decimal annualInterestRate,
            int tenorMonths,
            int interestOnlyMonths,
            decimal periodDifference,
            int invoiceDay,
            DateTime loanStartDate)
        {
            // periodDifference represents elapsed periods, so the next payment is for period (periodDifference + 1)
            decimal nextPaymentPeriod = periodDifference + 1;
            
            // If it's a whole number of months, use standard calculation
            if (periodDifference == Math.Floor(periodDifference))
            {
                int wholePeriod = (int)nextPaymentPeriod;
                if (wholePeriod <= interestOnlyMonths)
                {
                    return new FractionalPaymentResult
                    {
                        InterestComponent = _annuityCalculator.CalculateInterestComponent(loanAmount, annualInterestRate),
                        CapitalComponent = 0,
                        IsInterestOnlyPeriod = true,
                        FractionalDays = 0,
                        TotalDaysInPeriod = 30 // Standard month
                    };
                }
                else
                {
                    // Calculate annuity payment for standard periods (next payment)
                    return CalculateStandardAnnuityPayment(loanAmount, annualInterestRate, tenorMonths, interestOnlyMonths, wholePeriod);
                }
            }

            // Handle fractional periods - next payment will be for a partial period
            return CalculateFractionalPeriodPayment(loanAmount, annualInterestRate, tenorMonths, interestOnlyMonths, periodDifference, invoiceDay, loanStartDate);
        }

        private FractionalPaymentResult CalculateStandardAnnuityPayment(
            decimal loanAmount,
            decimal annualInterestRate,
            int tenorMonths,
            int interestOnlyMonths,
            int period)
        {
            // Calculate the remaining loan at the start of this period
            decimal remainingLoan = loanAmount;
            
            // Skip through interest-only periods (loan amount stays the same)
            if (period <= interestOnlyMonths)
            {
                decimal interestComponent = _annuityCalculator.CalculateInterestComponent(remainingLoan, annualInterestRate);
                return new FractionalPaymentResult
                {
                    InterestComponent = interestComponent,
                    CapitalComponent = 0,
                    IsInterestOnlyPeriod = true,
                    FractionalDays = 0,
                    TotalDaysInPeriod = 30
                };
            }

            // Calculate remaining loan by iterating through repayment periods
            int annuityMonths = tenorMonths - interestOnlyMonths;
            decimal monthlyPayment = _annuityCalculator.CalculateMonthlyAnnuity(remainingLoan, annualInterestRate, annuityMonths);

            // Iterate through each month up to (but not including) the current period
            // to calculate the remaining balance
            for (int i = interestOnlyMonths + 1; i < period; i++)
            {
                decimal interestComponent = _annuityCalculator.CalculateInterestComponent(remainingLoan, annualInterestRate);
                decimal capitalComponent = monthlyPayment - interestComponent;
                remainingLoan -= capitalComponent;
                
                if (remainingLoan < 0) remainingLoan = 0;
            }

            // Now calculate for the current period with the correct remaining balance
            decimal currentInterest = _annuityCalculator.CalculateInterestComponent(remainingLoan, annualInterestRate);
            decimal currentCapital = monthlyPayment - currentInterest;

            return new FractionalPaymentResult
            {
                InterestComponent = currentInterest,
                CapitalComponent = currentCapital,
                IsInterestOnlyPeriod = false,
                FractionalDays = 0,
                TotalDaysInPeriod = 30
            };
        }

        private FractionalPaymentResult CalculateFractionalPeriodPayment(
            decimal loanAmount,
            decimal annualInterestRate,
            int tenorMonths,
            int interestOnlyMonths,
            decimal periodDifference,
            int invoiceDay,
            DateTime loanStartDate)
        {
            // For fractional periods, we need to calculate pro-rata amounts
            decimal wholePart = Math.Floor(periodDifference);
            decimal fractionalPart = periodDifference - wholePart;

            // First, calculate the remaining loan balance at the start of this fractional period
            decimal remainingLoan = loanAmount;
            bool isInterestOnly = wholePart < interestOnlyMonths;

            // If we're past the interest-only period, calculate the remaining balance
            if (wholePart >= interestOnlyMonths)
            {
                int annuityMonths = tenorMonths - interestOnlyMonths;
                decimal monthlyPayment = _annuityCalculator.CalculateMonthlyAnnuity(loanAmount, annualInterestRate, annuityMonths);

                // Iterate through repayment periods to get current remaining balance
                for (int i = interestOnlyMonths + 1; i <= wholePart; i++)
                {
                    decimal interestComponent = _annuityCalculator.CalculateInterestComponent(remainingLoan, annualInterestRate);
                    decimal capitalComponent = monthlyPayment - interestComponent;
                    remainingLoan -= capitalComponent;
                    
                    if (remainingLoan < 0) remainingLoan = 0;
                }
            }

            // Calculate the base monthly amounts using the correct remaining balance
            decimal monthlyInterest = _annuityCalculator.CalculateInterestComponent(remainingLoan, annualInterestRate);
            decimal monthlyCapital = 0;

            if (!isInterestOnly)
            {
                int annuityMonths = tenorMonths - interestOnlyMonths;
                decimal monthlyPayment = _annuityCalculator.CalculateMonthlyAnnuity(loanAmount, annualInterestRate, annuityMonths);
                monthlyCapital = monthlyPayment - monthlyInterest;
            }

            // Calculate the fractional amounts based on days
            DateTime periodStartDate = loanStartDate.AddMonths((int)wholePart);
            DateTime nextInvoiceDate = CalculateNextInvoiceDate(loanStartDate, invoiceDay, periodStartDate);
            
            int daysInPeriod = (nextInvoiceDate - periodStartDate).Days;
            int totalDaysInMonth = DateTime.DaysInMonth(periodStartDate.Year, periodStartDate.Month);
            
            decimal dailyInterest = monthlyInterest / totalDaysInMonth;
            decimal dailyCapital = monthlyCapital / totalDaysInMonth;

            return new FractionalPaymentResult
            {
                InterestComponent = dailyInterest * daysInPeriod,
                CapitalComponent = dailyCapital * daysInPeriod,
                IsInterestOnlyPeriod = isInterestOnly,
                FractionalDays = daysInPeriod,
                TotalDaysInPeriod = totalDaysInMonth
            };
        }
    }

    public class FractionalPaymentResult
    {
        public decimal InterestComponent { get; set; }
        public decimal CapitalComponent { get; set; }
        public bool IsInterestOnlyPeriod { get; set; }
        public int FractionalDays { get; set; }
        public int TotalDaysInPeriod { get; set; }
    }
}