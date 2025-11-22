using LoanAnnuityCalculatorAPI.Models.Loan;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class LoanFinancialCalculatorService
    {
        public LoanFinancialCalculatorService()
        {
        }

        /// <summary>
        /// Calculates the outstanding loan balance at the end of a specific year
        /// </summary>
        public decimal CalculateOutstandingBalanceAtYear(Loan loan, int year)
        {
            // Calculate months elapsed from start to end of specified year
            var endOfYear = new DateTime(year, 12, 31);
            
            if (endOfYear < loan.StartDate)
                return loan.LoanAmount; // Loan hasn't started yet
            
            int monthsElapsed = ((endOfYear.Year - loan.StartDate.Year) * 12) + 
                                endOfYear.Month - loan.StartDate.Month;
            
            if (endOfYear.Day < loan.StartDate.Day)
                monthsElapsed--;
            
            if (monthsElapsed <= 0)
                return loan.LoanAmount;
            
            if (monthsElapsed >= loan.TenorMonths)
                return 0; // Loan fully repaid
            
            // Calculate based on redemption schedule
            if (loan.RedemptionSchedule == "Linear")
            {
                return CalculateLinearOutstanding(loan, monthsElapsed);
            }
            else // Annuity
            {
                return CalculateAnnuityOutstanding(loan, monthsElapsed);
            }
        }

        /// <summary>
        /// Calculates total interest paid during a specific year
        /// </summary>
        public decimal CalculateInterestForYear(Loan loan, int year)
        {
            var yearStart = new DateTime(year, 1, 1);
            var yearEnd = new DateTime(year, 12, 31);
            
            if (yearEnd < loan.StartDate)
                return 0; // Loan hasn't started
            
            // Determine the actual period within the year
            var periodStart = loan.StartDate > yearStart ? loan.StartDate : yearStart;
            var periodEnd = yearEnd;
            
            // Calculate which payment months fall in this year
            int startMonth = ((periodStart.Year - loan.StartDate.Year) * 12) + 
                            periodStart.Month - loan.StartDate.Month;
            int endMonth = ((periodEnd.Year - loan.StartDate.Year) * 12) + 
                          periodEnd.Month - loan.StartDate.Month;
            
            if (startMonth < 0) startMonth = 0;
            if (endMonth >= loan.TenorMonths) endMonth = loan.TenorMonths - 1;
            
            decimal totalInterest = 0;
            
            for (int month = startMonth; month <= endMonth; month++)
            {
                decimal outstandingAtStartOfMonth = CalculateOutstandingBalanceAtMonth(loan, month);
                decimal monthlyInterest = outstandingAtStartOfMonth * (loan.AnnualInterestRate / 100m / 12m);
                totalInterest += monthlyInterest;
            }
            
            return totalInterest;
        }

        /// <summary>
        /// Calculates total principal (redemption) paid during a specific year
        /// </summary>
        public decimal CalculateRedemptionForYear(Loan loan, int year)
        {
            var outstandingStartOfYear = CalculateOutstandingBalanceAtYear(loan, year - 1);
            var outstandingEndOfYear = CalculateOutstandingBalanceAtYear(loan, year);
            
            return outstandingStartOfYear - outstandingEndOfYear;
        }

        /// <summary>
        /// Gets total collateral value for a loan
        /// </summary>
        public decimal GetTotalCollateralValue(Loan loan)
        {
            if (loan.LoanCollaterals == null || !loan.LoanCollaterals.Any())
                return 0;
            
            return loan.LoanCollaterals
                .Where(lc => lc.Collateral != null && lc.Collateral.AppraisalValue.HasValue)
                .Sum(lc => lc.Collateral!.AppraisalValue!.Value * (lc.AllocationPercentage / 100m));
        }

        private decimal CalculateOutstandingBalanceAtMonth(Loan loan, int monthNumber)
        {
            if (monthNumber <= 0)
                return loan.LoanAmount;
            
            if (monthNumber >= loan.TenorMonths)
                return 0;
            
            if (loan.RedemptionSchedule == "Linear")
            {
                return CalculateLinearOutstanding(loan, monthNumber);
            }
            else
            {
                return CalculateAnnuityOutstanding(loan, monthNumber);
            }
        }

        private decimal CalculateLinearOutstanding(Loan loan, int monthsElapsed)
        {
            // During interest-only period, no principal is repaid
            if (monthsElapsed <= loan.InterestOnlyMonths)
                return loan.LoanAmount;
            
            // After interest-only period, calculate linear redemption
            int redemptionMonths = monthsElapsed - loan.InterestOnlyMonths;
            int totalRedemptionMonths = loan.TenorMonths - loan.InterestOnlyMonths;
            
            if (totalRedemptionMonths <= 0)
                return loan.LoanAmount;
            
            decimal monthlyRedemption = loan.LoanAmount / totalRedemptionMonths;
            decimal totalRedeemed = monthlyRedemption * redemptionMonths;
            
            return Math.Max(0, loan.LoanAmount - totalRedeemed);
        }

        private decimal CalculateAnnuityOutstanding(Loan loan, int monthsElapsed)
        {
            decimal monthlyRate = loan.AnnualInterestRate / 100m / 12m;
            
            // During interest-only period
            if (monthsElapsed <= loan.InterestOnlyMonths)
                return loan.LoanAmount;
            
            // After interest-only period, use annuity formula
            int paymentsAfterInterestOnly = monthsElapsed - loan.InterestOnlyMonths;
            int totalRedemptionPayments = loan.TenorMonths - loan.InterestOnlyMonths;
            
            if (totalRedemptionPayments <= 0 || monthlyRate == 0)
                return loan.LoanAmount;
            
            // Calculate remaining balance using annuity formula
            int remainingPayments = totalRedemptionPayments - paymentsAfterInterestOnly;
            
            if (remainingPayments <= 0)
                return 0;
            
            // Monthly payment during redemption period
            decimal monthlyPayment = loan.LoanAmount * 
                (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), totalRedemptionPayments)) /
                ((decimal)Math.Pow((double)(1 + monthlyRate), totalRedemptionPayments) - 1);
            
            // Calculate remaining balance
            decimal remainingBalance = monthlyPayment * 
                ((decimal)Math.Pow((double)(1 + monthlyRate), remainingPayments) - 1) /
                (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), remainingPayments));
            
            return Math.Max(0, remainingBalance);
        }
    }

    public class LoanFinancialSummary
    {
        public int Year { get; set; }
        public int LoanId { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal InterestExpense { get; set; }
        public decimal RedemptionAmount { get; set; }
        public decimal CollateralValue { get; set; }
    }
}
