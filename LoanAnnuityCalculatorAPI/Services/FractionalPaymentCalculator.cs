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
            // If it's a whole number of months, use standard calculation
            if (periodDifference == Math.Floor(periodDifference))
            {
                int wholePeriod = (int)periodDifference;
                if (wholePeriod < interestOnlyMonths)
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
                    // Calculate annuity payment for standard periods
                    return CalculateStandardAnnuityPayment(loanAmount, annualInterestRate, tenorMonths, interestOnlyMonths, wholePeriod);
                }
            }

            // Handle fractional periods
            return CalculateFractionalPeriodPayment(loanAmount, annualInterestRate, tenorMonths, interestOnlyMonths, periodDifference, invoiceDay, loanStartDate);
        }

        private FractionalPaymentResult CalculateStandardAnnuityPayment(
            decimal loanAmount,
            decimal annualInterestRate,
            int tenorMonths,
            int interestOnlyMonths,
            int period)
        {
            // Standard monthly calculation
            decimal remainingLoan = loanAmount;
            
            // Calculate through interest-only periods
            for (int i = 1; i <= Math.Min(period, interestOnlyMonths); i++)
            {
                if (i == period + 1)
                {
                    return new FractionalPaymentResult
                    {
                        InterestComponent = _annuityCalculator.CalculateInterestComponent(remainingLoan, annualInterestRate),
                        CapitalComponent = 0,
                        IsInterestOnlyPeriod = true,
                        FractionalDays = 0,
                        TotalDaysInPeriod = 30
                    };
                }
            }

            // If we're in the annuity period
            if (period >= interestOnlyMonths)
            {
                int annuityMonths = tenorMonths - interestOnlyMonths;
                decimal monthlyPayment = _annuityCalculator.CalculateMonthlyAnnuity(remainingLoan, annualInterestRate, annuityMonths);
                decimal interestComponent = _annuityCalculator.CalculateInterestComponent(remainingLoan, annualInterestRate);
                decimal capitalComponent = monthlyPayment - interestComponent;

                return new FractionalPaymentResult
                {
                    InterestComponent = interestComponent,
                    CapitalComponent = capitalComponent,
                    IsInterestOnlyPeriod = false,
                    FractionalDays = 0,
                    TotalDaysInPeriod = 30
                };
            }

            return new FractionalPaymentResult();
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

            // Calculate the base monthly amounts
            bool isInterestOnly = wholePart < interestOnlyMonths;
            decimal monthlyInterest = _annuityCalculator.CalculateInterestComponent(loanAmount, annualInterestRate);
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