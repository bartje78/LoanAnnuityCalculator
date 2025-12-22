using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LoanAnnuityCalculatorAPI.Models.Debtor;

namespace LoanAnnuityCalculatorAPI.Models.Loan

{
    public class Loan
    {
        [Key]
        [Column("Id")] // map model property to DB column "Id"
        public int LoanID { get; set; }

        [ForeignKey("DebtorDetails")]
        public int DebtorID { get; set; }

        /// <summary>
        /// Tenant this loan belongs to - CRITICAL for data isolation
        /// </summary>
        [Required]
        public int TenantId { get; set; }

        /// <summary>
        /// Fund this loan is associated with
        /// </summary>
        [Required]
        public int FundId { get; set; }

        public decimal LoanAmount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TenorMonths { get; set; }
        public int InterestOnlyMonths { get; set; }
        public DateTime StartDate { get; set; }
        public string? Status { get; set; }
        
        public string RedemptionSchedule { get; set; } = "Annuity";
        
        // Security type for the loan
        public string? SecurityType { get; set; }
        
        // First mortgage amount (subordination) - external first lien on the collateral package
        [Column(TypeName = "decimal(18,2)")]
        public decimal? FirstMortgageAmount { get; set; }
        
        // Building Depot specific fields
        public decimal? CreditLimit { get; set; }  // Maximum amount that can be drawn
        public decimal? AmountDrawn { get; set; }  // Actual amount drawn from the credit facility
        
        // Manual override for outstanding amount (for import corrections)
        public decimal? ManualOutstandingAmount { get; set; }
        
        // Calculated property for current outstanding amount
        [NotMapped]
        public decimal OutstandingAmount 
        { 
            get => ManualOutstandingAmount ?? CalculateOutstandingAmount();
            set => ManualOutstandingAmount = value;
        }

        // Navigation properties
        public DebtorDetails? DebtorDetails { get; set; }
        public virtual ICollection<LoanCollateral> LoanCollaterals { get; set; } = new List<LoanCollateral>();

        /// <summary>
        /// Calculates the current outstanding amount based on the loan schedule and elapsed time since start date
        /// </summary>
        private decimal CalculateOutstandingAmount()
        {
            try
            {
                // If loan hasn't started yet, outstanding amount equals original amount
                if (DateTime.Now < StartDate)
                    return LoanAmount;
                
                // Calculate how many months have elapsed since the start date
                int monthsElapsed = ((DateTime.Now.Year - StartDate.Year) * 12) + DateTime.Now.Month - StartDate.Month;
                
                // Adjust for day of month - if we haven't reached the payment day of the current month, subtract one month
                if (DateTime.Now.Day < StartDate.Day)
                    monthsElapsed--;
                
                // Ensure monthsElapsed is not negative
                if (monthsElapsed < 0)
                    return LoanAmount;
                
                // If we're past the loan term, outstanding amount is 0
                if (monthsElapsed >= TenorMonths)
                    return 0;

                decimal remainingLoan = LoanAmount;
                decimal monthlyInterestRate = AnnualInterestRate / 100 / 12;

                switch (RedemptionSchedule)
                {
                    case "BuildingDepot":
                        // For building depot: use AmountDrawn as the base, not LoanAmount
                        // During interest-only period: outstanding = amount drawn
                        // After interest-only: repay the drawn amount over remaining tenor
                        decimal drawnAmount = AmountDrawn ?? 0;
                        
                        if (monthsElapsed < InterestOnlyMonths)
                        {
                            // Still in interest-only phase - outstanding equals amount drawn
                            return drawnAmount;
                        }
                        else
                        {
                            // In repayment phase - calculate like annuity/linear on drawn amount
                            remainingLoan = drawnAmount;
                            int capitalRepaymentMonths = TenorMonths - InterestOnlyMonths;
                            int monthsIntoRepayment = monthsElapsed - InterestOnlyMonths;
                            
                            if (monthsIntoRepayment >= capitalRepaymentMonths)
                            {
                                return 0; // Fully repaid
                            }
                            
                            // Use annuity method for building depot repayment
                            decimal depotAnnuityPayment = 0;
                            if (monthlyInterestRate > 0)
                            {
                                depotAnnuityPayment = (drawnAmount * monthlyInterestRate * (decimal)Math.Pow((double)(1 + monthlyInterestRate), capitalRepaymentMonths)) /
                                               ((decimal)Math.Pow((double)(1 + monthlyInterestRate), capitalRepaymentMonths) - 1);
                            }
                            else
                            {
                                depotAnnuityPayment = drawnAmount / capitalRepaymentMonths;
                            }
                            
                            // Calculate remaining after months of repayment
                            for (int month = 1; month <= monthsIntoRepayment; month++)
                            {
                                decimal interestComponent = remainingLoan * monthlyInterestRate;
                                decimal capitalComponent = depotAnnuityPayment - interestComponent;
                                remainingLoan -= capitalComponent;
                                if (remainingLoan < 0) remainingLoan = 0;
                                if (remainingLoan == 0) break;
                            }
                        }
                        break;
                    case "Annuity":
                        // Calculate annuity payment for capital+interest phase
                        decimal annuityPayment = 0;
                        if (TenorMonths > InterestOnlyMonths)
                        {
                            int capitalRepaymentMonths = TenorMonths - InterestOnlyMonths;
                            if (monthlyInterestRate > 0)
                            {
                                annuityPayment = (LoanAmount * monthlyInterestRate * (decimal)Math.Pow((double)(1 + monthlyInterestRate), capitalRepaymentMonths)) /
                                               ((decimal)Math.Pow((double)(1 + monthlyInterestRate), capitalRepaymentMonths) - 1);
                            }
                            else
                            {
                                annuityPayment = LoanAmount / capitalRepaymentMonths;
                            }
                        }
                        for (int month = 1; month <= monthsElapsed; month++)
                        {
                            decimal interestComponent = remainingLoan * monthlyInterestRate;
                            decimal capitalComponent = 0;
                            if (month <= InterestOnlyMonths)
                            {
                                capitalComponent = 0;
                            }
                            else
                            {
                                capitalComponent = annuityPayment - interestComponent;
                            }
                            remainingLoan -= capitalComponent;
                            if (remainingLoan < 0) remainingLoan = 0;
                            if (remainingLoan == 0) break;
                        }
                        break;
                    case "Linear":
                        decimal linearCapital = LoanAmount / (TenorMonths - InterestOnlyMonths);
                        for (int month = 1; month <= monthsElapsed; month++)
                        {
                            decimal interestComponent = remainingLoan * monthlyInterestRate;
                            decimal capitalComponent = 0;
                            if (month <= InterestOnlyMonths)
                            {
                                capitalComponent = 0;
                            }
                            else
                            {
                                capitalComponent = linearCapital;
                            }
                            remainingLoan -= capitalComponent;
                            if (remainingLoan < 0) remainingLoan = 0;
                            if (remainingLoan == 0) break;
                        }
                        break;
                    case "Bullet":
                        // Only interest is paid until the last month, then full principal is repaid
                        if (monthsElapsed < TenorMonths)
                        {
                            // Still in interest-only phase
                            return LoanAmount;
                        }
                        else
                        {
                            // Loan is repaid at end
                            return 0;
                        }
                }
                return Math.Max(0, remainingLoan);
            }
            catch (Exception)
            {
                // If calculation fails for any reason, return original loan amount as fallback
                return LoanAmount;
            }
        }
    }
}

    public class AnnuityDetail
    {
        public int Month { get; set; } // The month number in the loan schedule
        public decimal InterestComponent { get; set; } // The interest portion of the payment
        public decimal CapitalComponent { get; set; } // The principal portion of the payment
        public decimal RemainingLoan { get; set; } // The remaining loan balance after the payment
    }