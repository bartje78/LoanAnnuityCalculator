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
                    annuityPayment = (loan.LoanAmount * monthlyInterestRate * (decimal)Math.Pow((double)(1 + monthlyInterestRate), capitalRepaymentMonths)) /
                                   ((decimal)Math.Pow((double)(1 + monthlyInterestRate), capitalRepaymentMonths) - 1);
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
                }

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

                var payment = new LoanPayment
                {
                    LoanId = loanId,
                    PaymentMonth = month,
                    InterestAmount = Math.Round(interestComponent, 2),
                    CapitalAmount = Math.Round(capitalComponent, 2),
                    TotalAmount = Math.Round(interestComponent + capitalComponent, 2),
                    DueDate = dueDate,
                    PaymentDate = paymentDate,
                    PaymentStatus = paymentStatus,
                    DaysLate = daysLate,
                    RemainingBalance = Math.Round(remainingLoan - capitalComponent, 2),
                    Notes = paymentDate.HasValue ? "Generated payment record" : null,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow
                };

                payments.Add(payment);
                remainingLoan -= capitalComponent;

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
                .Where(lp => lp.DueDate <= currentDate)
                .OrderBy(lp => lp.LoanId)
                .ThenBy(lp => lp.PaymentMonth)
                .ToListAsync();

            var random = new Random(42); // Fixed seed for consistent results
            var updatedCount = 0;

            foreach (var payment in payments)
            {
                // Create different payment scenarios based on position
                var monthsFromStart = payment.PaymentMonth;
                var probability = random.NextDouble();

                // 70% on-time, 15% minor late, 10% moderate late, 4% severe late, 1% missed
                if (probability < 0.70)
                {
                    // On-time payment
                    payment.PaymentStatus = "OnTime";
                    payment.PaymentDate = payment.DueDate;
                    payment.DaysLate = 0;
                }
                else if (probability < 0.85)
                {
                    // Minor late (1-7 days)
                    var daysLate = random.Next(1, 8);
                    payment.PaymentStatus = "LateMinor";
                    payment.PaymentDate = payment.DueDate.AddDays(daysLate);
                    payment.DaysLate = daysLate;
                }
                else if (probability < 0.95)
                {
                    // Moderate late (8-30 days)
                    var daysLate = random.Next(8, 31);
                    payment.PaymentStatus = "LateModerate";
                    payment.PaymentDate = payment.DueDate.AddDays(daysLate);
                    payment.DaysLate = daysLate;
                }
                else if (probability < 0.99)
                {
                    // Severe late (31+ days)
                    var daysLate = random.Next(31, 91);
                    payment.PaymentStatus = "LateSevere";
                    payment.PaymentDate = payment.DueDate.AddDays(daysLate);
                    payment.DaysLate = daysLate;
                }
                else
                {
                    // Missed payment
                    payment.PaymentStatus = "Missed";
                    payment.PaymentDate = null;
                    payment.DaysLate = (int)(currentDate - payment.DueDate).TotalDays;
                }

                payment.LastUpdatedDate = DateTime.Now;
                updatedCount++;
            }

            await _context.SaveChangesAsync();
            return updatedCount;
        }
    }
}