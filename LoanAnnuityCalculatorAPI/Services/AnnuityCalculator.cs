using System;
using System.Collections.Generic;
using LoanAnnuityCalculatorAPI.Models.Loan; // Updated namespace for loan/annuity-related models

namespace LoanAnnuityCalculatorAPI.Services
{
    public class AnnuityCalculator
    {
        // Existing method: Calculate annuity details for a specific month
        public (decimal interestComponent, decimal capitalComponent, decimal remainingLoan) CalculateForSpecificMonth(
            decimal loanAmount, decimal annualInterestRate, int tenorMonths, int period, int interestOnlyMonths = 0)
        {
            Console.WriteLine($"Input Parameters: LoanAmount={loanAmount}, AnnualInterestRate={annualInterestRate}, TenorMonths={tenorMonths}, Period={period}, InterestOnlyMonths={interestOnlyMonths}");

            if (period <= 0 || period > tenorMonths)
                throw new ArgumentException("The period must be within the loan's tenor.");
            if (interestOnlyMonths < 0 || interestOnlyMonths > tenorMonths)
                throw new ArgumentException("Interest-only months must be less than or equal to the loan's tenor.");

            decimal remainingLoan = loanAmount;

            // During the interest-only period
            if (interestOnlyMonths > 0 && period <= interestOnlyMonths)
            {
                decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                Console.WriteLine($"Interest-Only Period: Interest={interestComponent}, RemainingLoan={remainingLoan}");
                return (interestComponent, 0, remainingLoan); // No capital repayment during the interest-only period
            }

            // After the interest-only period
            int remainingTenor = tenorMonths - interestOnlyMonths;
            decimal monthlyAnnuity = CalculateMonthlyAnnuity(loanAmount, annualInterestRate, remainingTenor);
            Console.WriteLine($"Monthly Annuity: {monthlyAnnuity}");

            for (int i = 1; i <= (period - interestOnlyMonths); i++)
            {
                decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                decimal capitalComponent = CalculateCapitalComponent(monthlyAnnuity, interestComponent);

                Console.WriteLine($"Month {i}: Interest={interestComponent}, Capital={capitalComponent}, RemainingLoan={remainingLoan}");

                if (i == (period - interestOnlyMonths))
                {
                    return (interestComponent, capitalComponent, remainingLoan - capitalComponent);
                }

                remainingLoan -= capitalComponent;
            }

            throw new InvalidOperationException("Unexpected error during calculation.");
        }

        // New method: Calculate annuity details for the entire loan tenor
        public (List<(int month, decimal interestComponent, decimal capitalComponent, decimal remainingLoan)> results, bool isInterestOnly) CalculateAnnuityDetailsForEntireTenor(
            decimal loanAmount, decimal annualInterestRate, int tenorMonths, int interestOnlyMonths = 0)
        {
            Console.WriteLine($"Input Parameters: LoanAmount={loanAmount}, AnnualInterestRate={annualInterestRate}, TenorMonths={tenorMonths}, InterestOnlyMonths={interestOnlyMonths}");

            if (tenorMonths <= 0)
            {
                throw new ArgumentException("Tenor months must be greater than zero.");
            }

            if (interestOnlyMonths < 0 || interestOnlyMonths > tenorMonths)
            {
                throw new ArgumentException("Interest-only months must be less than or equal to the loan's tenor.");
            }

            decimal remainingLoan = loanAmount;
            var results = new List<(int month, decimal interestComponent, decimal capitalComponent, decimal remainingLoan)>();

            // During the interest-only period
            for (int i = 1; i <= interestOnlyMonths; i++)
            {
                decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                results.Add((i, interestComponent, 0, remainingLoan));
                Console.WriteLine($"[Interest-Only] Month {i}: Interest={interestComponent}, Capital=0, RemainingLoan={remainingLoan}");
            }

            // If the entire loan period is interest-only, return the results with a flag
            if (interestOnlyMonths == tenorMonths)
            {
                Console.WriteLine("[Interest-Only] The entire loan period is interest-only. No capital repayment.");
                Console.WriteLine($"[Interest-Only] Final Results: {System.Text.Json.JsonSerializer.Serialize(results)}");
                return (results, true);
            }

            // After the interest-only period
            int remainingTenor = tenorMonths - interestOnlyMonths;
            decimal monthlyAnnuity = CalculateMonthlyAnnuity(loanAmount, annualInterestRate, remainingTenor);
            Console.WriteLine($"[Repayment] Monthly Annuity: {monthlyAnnuity}");

            for (int i = interestOnlyMonths + 1; i <= tenorMonths; i++)
            {
                decimal interestComponent = CalculateInterestComponent(remainingLoan, annualInterestRate);
                decimal capitalComponent = CalculateCapitalComponent(monthlyAnnuity, interestComponent);

                results.Add((i, interestComponent, capitalComponent, remainingLoan));
                Console.WriteLine($"[Repayment] Month {i}: Interest={interestComponent}, Capital={capitalComponent}, RemainingLoan={remainingLoan}");

                remainingLoan -= capitalComponent;

                // Ensure remainingLoan does not go negative due to rounding issues
                if (remainingLoan < 0) remainingLoan = 0;
            }

            // Log the final results before returning
            Console.WriteLine($"[Repayment] Final Results: {System.Text.Json.JsonSerializer.Serialize(results)}");
            return (results, false);
        }

        // Helper method: Calculate the monthly annuity
        public decimal CalculateMonthlyAnnuity(decimal loanAmount, decimal annualInterestRate, int tenorMonths)
        {
            if (tenorMonths <= 0)
            {
                throw new ArgumentException("Tenor months must be greater than zero.");
            }

            decimal monthlyInterestRate = annualInterestRate / 12 / 100;

            // Formula for annuity
            return loanAmount * (monthlyInterestRate * (decimal)Math.Pow(1 + (double)monthlyInterestRate, tenorMonths)) /
                   ((decimal)Math.Pow(1 + (double)monthlyInterestRate, tenorMonths) - 1);
        }

        // Helper method: Calculate the interest component
        public decimal CalculateInterestComponent(decimal remainingLoan, decimal annualInterestRate)
        {
            decimal monthlyInterestRate = annualInterestRate / 12 / 100;
            return remainingLoan * monthlyInterestRate;
        }

        // Helper method: Calculate the capital component
        public decimal CalculateCapitalComponent(decimal monthlyAnnuity, decimal interestComponent)
        {
            return monthlyAnnuity - interestComponent;
        }
    }
}