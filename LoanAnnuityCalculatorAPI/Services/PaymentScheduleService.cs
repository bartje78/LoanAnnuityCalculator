using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models.Loan;
using LoanAnnuityCalculatorAPI.Models.Payment;
using LoanAnnuityCalculatorAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class PaymentScheduleService
    {
        private readonly LoanDbContext _context;
        private readonly AnnuityCalculator _calculatorService;

        public PaymentScheduleService(LoanDbContext context, AnnuityCalculator calculatorService)
        {
            _context = context;
            _calculatorService = calculatorService;
        }

        /// <summary>
        /// Generates payment schedule for a loan and saves it to the database
        /// </summary>
        public async Task<List<LoanPayment>> GeneratePaymentScheduleAsync(int loanId)
        {
            var loan = await _context.Loans.FindAsync(loanId);
            if (loan == null)
                throw new ArgumentException($"Loan with ID {loanId} not found");

            // Check if payment schedule already exists
            var existingPayments = await _context.LoanPayments
                .Where(lp => lp.LoanId == loanId)
                .ToListAsync();

            if (existingPayments.Any())
            {
                return existingPayments.OrderBy(p => p.PaymentMonth).ToList();
            }

            var payments = new List<LoanPayment>();
            decimal remainingLoan = loan.LoanAmount;
            decimal monthlyInterestRate = loan.AnnualInterestRate / 100 / 12;

            // Calculate annuity payment for capital+interest phase
            decimal annuityPayment = 0;
            if (loan.TenorMonths > loan.InterestOnlyMonths)
            {
                int capitalRepaymentMonths = loan.TenorMonths - loan.InterestOnlyMonths;
                if (monthlyInterestRate > 0)
                {
                    // Use higher precision by calculating compound factor with decimal only
                    decimal compoundFactor = DecimalPower(1 + monthlyInterestRate, capitalRepaymentMonths);
                    annuityPayment = (loan.LoanAmount * monthlyInterestRate * compoundFactor) / (compoundFactor - 1);
                }
                else
                {
                    annuityPayment = loan.LoanAmount / capitalRepaymentMonths;
                }
            }

            // Generate payment schedule for the entire loan term
            for (int month = 1; month <= loan.TenorMonths; month++)
            {
                decimal interestComponent = remainingLoan * monthlyInterestRate;
                decimal capitalComponent = 0;

                if (month <= loan.InterestOnlyMonths)
                {
                    // Interest-only phase
                    capitalComponent = 0;
                }
                else
                {
                    // Capital+interest phase
                    capitalComponent = annuityPayment - interestComponent;
                    
                    // On the last payment, adjust to pay off remaining loan exactly
                    if (month == loan.TenorMonths && remainingLoan > 0)
                    {
                        capitalComponent = remainingLoan;
                    }
                }

                // Round the components for storage/display
                decimal roundedInterest = Math.Round(interestComponent, 2);
                decimal roundedCapital = Math.Round(capitalComponent, 2);
                decimal roundedTotal = Math.Round(interestComponent + capitalComponent, 2);

                // Calculate due date (1st of each month following the start date)
                var dueDate = loan.StartDate.AddMonths(month);
                dueDate = new DateTime(dueDate.Year, dueDate.Month, 1);

                // For historical payments (past due dates), set payment date as if paid on time
                DateTime? paymentDate = null;
                string paymentStatus = "Pending";
                int daysLate = 0;

                if (dueDate <= DateTime.Now)
                {
                    // This payment was due in the past - mark as paid on time for now
                    paymentDate = dueDate;
                    paymentStatus = "OnTime";
                    daysLate = 0;
                }

                // Update remaining loan using rounded capital
                remainingLoan -= roundedCapital;
                
                // Ensure last payment shows exactly zero remaining balance
                decimal remainingBalance = month == loan.TenorMonths ? 0 : Math.Round(remainingLoan, 2);

                var payment = new LoanPayment
                {
                    LoanId = loanId,
                    PaymentMonth = month,
                    InterestAmount = roundedInterest,
                    CapitalAmount = roundedCapital,
                    TotalAmount = roundedTotal,
                    DueDate = dueDate,
                    PaymentDate = paymentDate,
                    PaymentStatus = paymentStatus,
                    DaysLate = daysLate,
                    RemainingBalance = remainingBalance,
                    Notes = paymentDate.HasValue ? "Generated payment record" : null,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow
                };

                payments.Add(payment);

                // Ensure remaining loan doesn't go negative
                if (remainingLoan < 0) remainingLoan = 0;
                
                if (remainingLoan == 0) break;
            }

            // Save all payments to database
            await _context.LoanPayments.AddRangeAsync(payments);
            await _context.SaveChangesAsync();

            return payments;
        }

        /// <summary>
        /// Generates payment schedules for all loans that don't have them yet
        /// </summary>
        public async Task<int> GenerateAllMissingPaymentSchedulesAsync()
        {
            var loansWithoutPayments = await _context.Loans
                .Where(l => !_context.LoanPayments.Any(lp => lp.LoanId == l.LoanID))
                .ToListAsync();

            int schedulesCreated = 0;

            foreach (var loan in loansWithoutPayments)
            {
                try
                {
                    await GeneratePaymentScheduleAsync(loan.LoanID);
                    schedulesCreated++;
                }
                catch (Exception ex)
                {
                    // Log error but continue with other loans
                    Console.WriteLine($"Error generating payment schedule for loan {loan.LoanID}: {ex.Message}");
                }
            }

            return schedulesCreated;
        }

        /// <summary>
        /// Gets payment history for a specific loan
        /// </summary>
        public async Task<List<LoanPayment>> GetPaymentHistoryAsync(int loanId)
        {
            return await _context.LoanPayments
                .Where(lp => lp.LoanId == loanId)
                .OrderBy(lp => lp.PaymentMonth)
                .ToListAsync();
        }

        /// <summary>
        /// Gets all payments for fund analysis
        /// </summary>
        public async Task<List<LoanPayment>> GetAllPaymentsAsync()
        {
            return await _context.LoanPayments
                .OrderBy(lp => lp.DueDate)
                .ToListAsync();
        }

        /// <summary>
        /// Updates payment status when a payment is received
        /// </summary>
        public async Task<LoanPayment?> RecordPaymentAsync(int loanId, int paymentMonth, DateTime actualPaymentDate, string? notes = null)
        {
            var payment = await _context.LoanPayments
                .FirstOrDefaultAsync(lp => lp.LoanId == loanId && lp.PaymentMonth == paymentMonth);

            if (payment == null)
                return null;

            payment.PaymentDate = actualPaymentDate;
            payment.DaysLate = (actualPaymentDate - payment.DueDate).Days;
            
            // Determine payment status
            if (payment.DaysLate <= 0)
                payment.PaymentStatus = "OnTime";
            else if (payment.DaysLate <= 30) // Allow some grace period
                payment.PaymentStatus = "Late";
            else
                payment.PaymentStatus = "Late"; // Could add "VeryLate" status if needed

            if (!string.IsNullOrEmpty(notes))
                payment.Notes = notes;

            payment.LastUpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return payment;
        }

        /// <summary>
        /// Gets payment discipline summary for a debtor
        /// </summary>
        public async Task<object> GetPaymentDisciplineSummaryAsync(int debtorId)
        {
            var paymentData = await _context.LoanPayments
                .Include(lp => lp.Loan)
                .Where(lp => lp.Loan!.DebtorID == debtorId && lp.PaymentDate.HasValue)
                .GroupBy(lp => lp.PaymentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalPayments = paymentData.Sum(p => p.Count);
            var onTimePayments = paymentData.Where(p => p.Status == "OnTime").Sum(p => p.Count);
            var latePayments = paymentData.Where(p => p.Status == "Late").Sum(p => p.Count);

            return new
            {
                TotalPayments = totalPayments,
                OnTimePayments = onTimePayments,
                LatePayments = latePayments,
                OnTimePercentage = totalPayments > 0 ? Math.Round((double)onTimePayments / totalPayments * 100, 1) : 0,
                PaymentsByStatus = paymentData
            };
        }

        /// <summary>
        /// Updates payment statuses with realistic test data for visualization
        /// </summary>
        public async Task<int> UpdatePaymentStatusesForTestingAsync()
        {
            var currentDate = DateTime.Now;
            var payments = await _context.LoanPayments
                .Include(lp => lp.Loan)
                .Where(lp => lp.DueDate <= currentDate)
                .OrderBy(lp => lp.LoanId)
                .ThenBy(lp => lp.PaymentMonth)
                .ToListAsync();

            var random = new Random(42); // Fixed seed for consistent results
            var updatedCount = 0;

            // Group payments by loan to create consistent payment patterns per debtor
            var paymentsByLoan = payments.GroupBy(p => p.LoanId).ToList();

            foreach (var loanPayments in paymentsByLoan)
            {
                // Determine overall payment behavior for this loan (70% good, 20% fair, 10% poor)
                var behaviorScore = random.NextDouble();
                double onTimeProbability;
                double minorLateProbability;
                double moderateLateProbability;
                
                if (behaviorScore < 0.70)
                {
                    // Good payer: 85% on-time, 12% minor late, 3% moderate late
                    onTimeProbability = 0.85;
                    minorLateProbability = 0.97;
                    moderateLateProbability = 1.0;
                }
                else if (behaviorScore < 0.90)
                {
                    // Fair payer: 60% on-time, 25% minor late, 12% moderate late, 3% severe
                    onTimeProbability = 0.60;
                    minorLateProbability = 0.85;
                    moderateLateProbability = 0.97;
                }
                else
                {
                    // Poor payer: 30% on-time, 30% minor late, 25% moderate late, 15% severe/missed
                    onTimeProbability = 0.30;
                    minorLateProbability = 0.60;
                    moderateLateProbability = 0.85;
                }

                var consecutiveLateCount = 0;

                foreach (var payment in loanPayments.OrderBy(p => p.PaymentMonth))
                {
                    var probability = random.NextDouble();
                    
                    // Add early-term honeymoon effect (better payment in first 6 months)
                    var honeymoonBoost = payment.PaymentMonth <= 6 ? 0.15 : 0;
                    
                    // Add deterioration effect if multiple consecutive late payments
                    var deteriorationPenalty = consecutiveLateCount > 2 ? 0.1 : 0;

                    var adjustedOnTimeProbability = Math.Min(0.95, onTimeProbability + honeymoonBoost - deteriorationPenalty);

                    if (probability < adjustedOnTimeProbability)
                    {
                        // On-time payment (within 3 days grace period)
                        var earlyDays = random.Next(-2, 4); // Can be 2 days early to 3 days late
                        payment.PaymentStatus = earlyDays <= 0 ? "OnTime" : "OnTime";
                        payment.PaymentDate = payment.DueDate.AddDays(earlyDays);
                        payment.DaysLate = Math.Max(0, earlyDays);
                        consecutiveLateCount = 0;
                    }
                    else if (probability < minorLateProbability)
                    {
                        // Minor late (4-14 days)
                        var daysLate = random.Next(4, 15);
                        payment.PaymentStatus = "LateMinor";
                        payment.PaymentDate = payment.DueDate.AddDays(daysLate);
                        payment.DaysLate = daysLate;
                        consecutiveLateCount++;
                    }
                    else if (probability < moderateLateProbability)
                    {
                        // Moderate late (15-45 days)
                        var daysLate = random.Next(15, 46);
                        payment.PaymentStatus = "LateModerate";
                        payment.PaymentDate = payment.DueDate.AddDays(daysLate);
                        payment.DaysLate = daysLate;
                        consecutiveLateCount++;
                    }
                    else
                    {
                        // Severe late or missed
                        var missedProbability = random.NextDouble();
                        if (missedProbability < 0.3)
                        {
                            // Missed payment (15% of problematic payments)
                            payment.PaymentStatus = "Missed";
                            payment.PaymentDate = null;
                            payment.DaysLate = (int)(currentDate - payment.DueDate).TotalDays;
                        }
                        else
                        {
                            // Severe late (46-120 days)
                            var daysLate = random.Next(46, 121);
                            payment.PaymentStatus = "LateSevere";
                            payment.PaymentDate = payment.DueDate.AddDays(daysLate);
                            payment.DaysLate = daysLate;
                        }
                        consecutiveLateCount++;
                    }

                    payment.LastUpdatedDate = DateTime.Now;
                    updatedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return updatedCount;
        }

        /// <summary>
        /// Calculate decimal power without converting to double, maintaining precision
        /// </summary>
        private decimal DecimalPower(decimal baseValue, int exponent)
        {
            if (exponent == 0) return 1m;
            if (exponent == 1) return baseValue;
            
            decimal result = 1m;
            decimal multiplier = baseValue;
            int exp = Math.Abs(exponent);
            
            // Use binary exponentiation for efficiency
            while (exp > 0)
            {
                if (exp % 2 == 1)
                    result *= multiplier;
                multiplier *= multiplier;
                exp /= 2;
            }
            
            return exponent < 0 ? 1m / result : result;
        }
    }
}
