using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LoanAnnuityCalculatorAPI.Models.Loan;

namespace LoanAnnuityCalculatorAPI.Models.Payment
{
    public class LoanPayment
    {
        [Key]
        public int PaymentId { get; set; }

        [ForeignKey("Loan")]
        public int LoanId { get; set; }

        /// <summary>
        /// The month number in the loan schedule (1 = first payment, 2 = second payment, etc.)
        /// </summary>
        public int PaymentMonth { get; set; }

        /// <summary>
        /// The interest portion of the payment
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal InterestAmount { get; set; }

        /// <summary>
        /// The capital/principal portion of the payment
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CapitalAmount { get; set; }

        /// <summary>
        /// Total payment amount (InterestAmount + CapitalAmount)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// The date when the payment was due
        /// </summary>
        public DateTime DueDate { get; set; }

        /// <summary>
        /// The date when the payment was actually received (null if not yet received)
        /// </summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// Payment status: Pending, OnTime, Late, Missed
        /// </summary>
        public string PaymentStatus { get; set; } = "Pending";

        /// <summary>
        /// Number of days late (positive) or early (negative). 0 = on time.
        /// </summary>
        public int DaysLate { get; set; } = 0;

        /// <summary>
        /// Remaining loan balance after this payment
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingBalance { get; set; }

        /// <summary>
        /// Notes about the payment (optional)
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// When this payment record was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this payment record was last updated
        /// </summary>
        public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Loan.Loan? Loan { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,    // Payment not yet due or received
        OnTime,     // Payment received on or before due date
        Late,       // Payment received after due date
        Missed      // Payment not received and past due date
    }
}