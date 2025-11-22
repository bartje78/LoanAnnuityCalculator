using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Junction table to support many-to-many relationship between Loans and Collaterals
    /// This allows the same collateral asset to secure multiple loans
    /// </summary>
    public class LoanCollateral
    {
        [Key]
        public int LoanCollateralId { get; set; }

        [ForeignKey("Loan")]
        public int LoanId { get; set; }

        [ForeignKey("Collateral")]
        public int CollateralId { get; set; }

        /// <summary>
        /// Date when this collateral was assigned to this loan
        /// </summary>
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Percentage of the collateral value allocated to this specific loan
        /// Useful when one collateral secures multiple loans with different priorities
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal AllocationPercentage { get; set; } = 100.00m;

        /// <summary>
        /// Priority/ranking of this loan's claim on the collateral
        /// Lower numbers = higher priority (1 = first priority, 2 = second priority, etc.)
        /// </summary>
        public int Priority { get; set; } = 1;

        /// <summary>
        /// Optional notes specific to this loan-collateral relationship
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual Loan.Loan? Loan { get; set; }
        public virtual Collateral? Collateral { get; set; }
    }
}