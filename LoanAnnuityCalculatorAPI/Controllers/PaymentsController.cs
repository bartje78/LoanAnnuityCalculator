using Microsoft.AspNetCore.Mvc;
using LoanAnnuityCalculatorAPI.Services;
using LoanAnnuityCalculatorAPI.Models.Payment;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentScheduleService _paymentService;

        public PaymentsController(PaymentScheduleService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Generate payment schedule for a specific loan
        /// </summary>
        [HttpPost("generate-schedule/{loanId}")]
        public async Task<ActionResult<List<LoanPayment>>> GeneratePaymentSchedule(int loanId)
        {
            try
            {
                var payments = await _paymentService.GeneratePaymentScheduleAsync(loanId);
                return Ok(payments);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating payment schedule: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate payment schedules for all loans that don't have them
        /// </summary>
        [HttpPost("generate-all-schedules")]
        public async Task<ActionResult<object>> GenerateAllPaymentSchedules()
        {
            try
            {
                var schedulesCreated = await _paymentService.GenerateAllMissingPaymentSchedulesAsync();
                return Ok(new { SchedulesCreated = schedulesCreated, Message = $"Generated payment schedules for {schedulesCreated} loans" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating payment schedules: {ex.Message}");
            }
        }

        /// <summary>
        /// Get payment history for a specific loan
        /// </summary>
        [HttpGet("loan/{loanId}")]
        public async Task<ActionResult<List<LoanPayment>>> GetPaymentHistory(int loanId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentHistoryAsync(loanId);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving payment history: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all payments for fund analysis
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<List<LoanPayment>>> GetAllPayments()
        {
            try
            {
                var payments = await _paymentService.GetAllPaymentsAsync();
                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving all payments: {ex.Message}");
            }
        }

        /// <summary>
        /// Record a payment received for a specific loan and payment month
        /// </summary>
        [HttpPost("record-payment")]
        public async Task<ActionResult<LoanPayment>> RecordPayment([FromBody] RecordPaymentRequest request)
        {
            try
            {
                var payment = await _paymentService.RecordPaymentAsync(
                    request.LoanId, 
                    request.PaymentMonth, 
                    request.ActualPaymentDate, 
                    request.Notes);

                if (payment == null)
                    return NotFound($"Payment not found for loan {request.LoanId}, month {request.PaymentMonth}");

                return Ok(payment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error recording payment: {ex.Message}");
            }
        }

        /// <summary>
        /// Get payment discipline summary for a debtor
        /// </summary>
        [HttpGet("discipline/{debtorId}")]
        public async Task<ActionResult<object>> GetPaymentDiscipline(int debtorId)
        {
            try
            {
                var summary = await _paymentService.GetPaymentDisciplineSummaryAsync(debtorId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving payment discipline: {ex.Message}");
            }
        }

        /// <summary>
        /// Update payment statuses for testing purposes
        /// </summary>
        [HttpPost("update-test-statuses")]
        public async Task<ActionResult<object>> UpdateTestPaymentStatuses()
        {
            try
            {
                var updatedCount = await _paymentService.UpdatePaymentStatusesForTestingAsync();
                return Ok(new { UpdatedPayments = updatedCount, Message = $"Updated {updatedCount} payment records with test statuses" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating payment statuses: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Request model for recording a payment
    /// </summary>
    public class RecordPaymentRequest
    {
        public int LoanId { get; set; }
        public int PaymentMonth { get; set; }
        public DateTime ActualPaymentDate { get; set; }
        public string? Notes { get; set; }
    }
}