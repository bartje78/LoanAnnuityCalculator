using System;
using System.Collections.Generic;
using LoanAnnuityCalculatorAPI.Models.Loan;

namespace LoanAnnuityCalculatorAPI.Services
{
    /// <summary>
    /// Service for calculating payment schedules for different redemption types (Annuity, Linear, Bullet)
    /// </summary>
    public class PaymentCalculatorService
    {
        /// <summary>
        /// Calculate payment details for a specific month based on the loan's redemption schedule
        /// </summary>
        public (decimal interestComponent, decimal capitalComponent, decimal remainingLoan) CalculateForSpecificMonth(
            decimal loanAmount, 
            decimal annualInterestRate, 
            int tenorMonths, 
            int period, 
            int interestOnlyMonths,
            string redemptionSchedule)
        {
            if (period <= 0 || period > tenorMonths)
                throw new ArgumentException("The period must be within the loan's tenor.");
            if (interestOnlyMonths < 0 || interestOnlyMonths > tenorMonths)
                throw new ArgumentException("Interest-only months must be less than or equal to the loan's tenor.");

            return redemptionSchedule switch
            {
                "Annuity" => CalculateAnnuityForMonth(loanAmount, annualInterestRate, tenorMonths, period, interestOnlyMonths),
                "Linear" => CalculateLinearForMonth(loanAmount, annualInterestRate, tenorMonths, period, interestOnlyMonths),
                "Bullet" => CalculateBulletForMonth(loanAmount, annualInterestRate, tenorMonths, period, interestOnlyMonths),
                "BuildingDepot" => CalculateAnnuityForMonth(loanAmount, annualInterestRate, tenorMonths, period, interestOnlyMonths),
                _ => throw new ArgumentException($"Unsupported redemption schedule: {redemptionSchedule}")
            };
        }

        /// <summary>
        /// Calculate payment details for the entire tenor based on the loan's redemption schedule
        /// </summary>
        public List<(int month, decimal interestComponent, decimal capitalComponent, decimal remainingLoan)> CalculateForEntireTenor(
            decimal loanAmount,
            decimal annualInterestRate,
            int tenorMonths,
            int interestOnlyMonths,
            string redemptionSchedule)
        {
            if (tenorMonths <= 0)
                throw new ArgumentException("Tenor months must be greater than zero.");
            if (interestOnlyMonths < 0 || interestOnlyMonths > tenorMonths)
                throw new ArgumentException("Interest-only months must be less than or equal to the loan's tenor.");

            return redemptionSchedule switch
            {
                "Annuity" => CalculateAnnuityForEntireTenor(loanAmount, annualInterestRate, tenorMonths, interestOnlyMonths),
                "Linear" => CalculateLinearForEntireTenor(loanAmount, annualInterestRate, tenorMonths, interestOnlyMonths),
                "Bullet" => CalculateBulletForEntireTenor(loanAmount, annualInterestRate, tenorMonths, interestOnlyMonths),
                "BuildingDepot" => CalculateAnnuityForEntireTenor(loanAmount, annualInterestRate, tenorMonths, interestOnlyMonths),
                _ => throw new ArgumentException($"Unsupported redemption schedule: {redemptionSchedule}")
            };
        }

        #region Annuity Calculations

        private (decimal interestComponent, decimal capitalComponent, decimal remainingLoan) CalculateAnnuityForMonth(
            decimal loanAmount, decimal annualInterestRate, int tenorMonths, int period, int interestOnlyMonths)
        {
            decimal remainingLoan = loanAmount;

            // During the interest-only period
            if (period <= interestOnlyMonths)
            {
                decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                return (interestComponent, 0, remainingLoan);
            }

            // After the interest-only period - calculate remaining loan at the target period
            int remainingTenor = tenorMonths - interestOnlyMonths;
            decimal monthlyAnnuity = CalculateMonthlyAnnuity(loanAmount, annualInterestRate, remainingTenor);

            for (int i = interestOnlyMonths + 1; i <= period; i++)
            {
                decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                decimal capitalComponent = monthlyAnnuity - interestComponent;

                if (i == period)
                {
                    return (interestComponent, capitalComponent, remainingLoan);
                }

                remainingLoan -= capitalComponent;
                if (remainingLoan < 0) remainingLoan = 0;
            }

            throw new InvalidOperationException("Unexpected error during annuity calculation.");
        }

        private List<(int month, decimal interestComponent, decimal capitalComponent, decimal remainingLoan)> CalculateAnnuityForEntireTenor(
            decimal loanAmount, decimal annualInterestRate, int tenorMonths, int interestOnlyMonths)
        {
            decimal remainingLoan = loanAmount;
            var results = new List<(int month, decimal interestComponent, decimal capitalComponent, decimal remainingLoan)>();

            // Interest-only period
            for (int i = 1; i <= interestOnlyMonths; i++)
            {
                decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                results.Add((i, interestComponent, 0, remainingLoan));
            }

            // Repayment period
            int remainingTenor = tenorMonths - interestOnlyMonths;
            if (remainingTenor > 0)
            {
                decimal monthlyAnnuity = CalculateMonthlyAnnuity(loanAmount, annualInterestRate, remainingTenor);

                for (int i = interestOnlyMonths + 1; i <= tenorMonths; i++)
                {
                    decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                    decimal capitalComponent = monthlyAnnuity - interestComponent;

                    remainingLoan -= capitalComponent;
                    if (remainingLoan < 0) remainingLoan = 0;

                    results.Add((i, interestComponent, capitalComponent, remainingLoan));
                }
            }

            return results;
        }

        #endregion

        #region Linear Calculations

        private (decimal interestComponent, decimal capitalComponent, decimal remainingLoan) CalculateLinearForMonth(
            decimal loanAmount, decimal annualInterestRate, int tenorMonths, int period, int interestOnlyMonths)
        {
            decimal remainingLoan = loanAmount;

            // During the interest-only period
            if (period <= interestOnlyMonths)
            {
                decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                return (interestComponent, 0, remainingLoan);
            }

            // After the interest-only period - calculate remaining loan at the target period
            int remainingTenor = tenorMonths - interestOnlyMonths;
            decimal linearCapitalPayment = loanAmount / remainingTenor;

            for (int i = interestOnlyMonths + 1; i <= period; i++)
            {
                decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);

                if (i == period)
                {
                    return (interestComponent, linearCapitalPayment, remainingLoan);
                }

                remainingLoan -= linearCapitalPayment;
                if (remainingLoan < 0) remainingLoan = 0;
            }

            throw new InvalidOperationException("Unexpected error during linear calculation.");
        }

        private List<(int month, decimal interestComponent, decimal capitalComponent, decimal remainingLoan)> CalculateLinearForEntireTenor(
            decimal loanAmount, decimal annualInterestRate, int tenorMonths, int interestOnlyMonths)
        {
            decimal remainingLoan = loanAmount;
            var results = new List<(int month, decimal interestComponent, decimal capitalComponent, decimal remainingLoan)>();

            // Interest-only period
            for (int i = 1; i <= interestOnlyMonths; i++)
            {
                decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                results.Add((i, interestComponent, 0, remainingLoan));
            }

            // Repayment period with linear amortization
            int remainingTenor = tenorMonths - interestOnlyMonths;
            if (remainingTenor > 0)
            {
                decimal linearCapitalPayment = loanAmount / remainingTenor;

                for (int i = interestOnlyMonths + 1; i <= tenorMonths; i++)
                {
                    decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                    
                    remainingLoan -= linearCapitalPayment;
                    if (remainingLoan < 0) remainingLoan = 0;
                    
                    results.Add((i, interestComponent, linearCapitalPayment, remainingLoan));
                }
            }

            return results;
        }

        #endregion

        #region Bullet Calculations

        private (decimal interestComponent, decimal capitalComponent, decimal remainingLoan) CalculateBulletForMonth(
            decimal loanAmount, decimal annualInterestRate, int tenorMonths, int period, int interestOnlyMonths)
        {
            decimal interestComponent = CalculateInterestComponent(loanAmount, annualInterestRate);

            // For bullet loans, capital is only repaid in the final month
            if (period < tenorMonths)
            {
                return (interestComponent, 0, loanAmount);
            }
            else // period == tenorMonths
            {
                return (interestComponent, loanAmount, loanAmount);
            }
        }

        private List<(int month, decimal interestComponent, decimal capitalComponent, decimal remainingLoan)> CalculateBulletForEntireTenor(
            decimal loanAmount, decimal annualInterestRate, int tenorMonths, int interestOnlyMonths)
        {
            var results = new List<(int month, decimal interestComponent, decimal capitalComponent, decimal remainingLoan)>();
            decimal interestComponent = CalculateInterestComponent(loanAmount, annualInterestRate);

            // All months except the last: interest only
            for (int i = 1; i < tenorMonths; i++)
            {
                results.Add((i, interestComponent, 0, loanAmount));
            }

            // Last month: interest + full principal repayment (remaining balance = 0)
            results.Add((tenorMonths, interestComponent, loanAmount, 0));

            return results;
        }

        #endregion

        #region Helper Methods

        private decimal CalculateMonthlyAnnuity(decimal loanAmount, decimal annualInterestRate, int tenorMonths)
        {
            if (tenorMonths <= 0)
                throw new ArgumentException("Tenor months must be greater than zero.");

            decimal monthlyInterestRate = annualInterestRate / 12 / 100;

            if (monthlyInterestRate == 0)
                return loanAmount / tenorMonths;

            return loanAmount * (monthlyInterestRate * (decimal)Math.Pow(1 + (double)monthlyInterestRate, tenorMonths)) /
                   ((decimal)Math.Pow(1 + (double)monthlyInterestRate, tenorMonths) - 1);
        }

        private decimal CalculateInterestComponent(decimal remainingLoan, decimal annualInterestRate)
        {
            decimal monthlyInterestRate = annualInterestRate / 12 / 100;
            return remainingLoan * monthlyInterestRate;
        }

        #endregion
    }
}
