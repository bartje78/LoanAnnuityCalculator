using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Represents a withdrawal from a building depot loan
    /// </summary>
    public class BuildingDepotWithdrawal
    {
        [Key]
        public int WithdrawalId { get; set; }

        [Required]
        public int LoanId { get; set; }

        [ForeignKey("LoanId")]
        [JsonIgnore]
        public virtual Loan.Loan? Loan { get; set; }

        [Required]
        [StringLength(50)]
        public string WithdrawalType { get; set; } = string.Empty; // "Supplier" or "LumpSum"

        [Required]
        public DateTime WithdrawalDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string? DeclarationFileName { get; set; }

        [StringLength(1000)]
        public string? DeclarationFilePath { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // "Draft", "Submitted", "Approved", "Paid"

        public int? TenantId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? SubmittedDate { get; set; }

        public virtual ICollection<BuildingDepotWithdrawalLineItem> LineItems { get; set; } = new List<BuildingDepotWithdrawalLineItem>();
    }

    /// <summary>
    /// Represents a line item within a building depot withdrawal
    /// </summary>
    public class BuildingDepotWithdrawalLineItem
    {
        [Key]
        public int LineItemId { get; set; }

        [Required]
        public int WithdrawalId { get; set; }

        [ForeignKey("WithdrawalId")]
        [JsonIgnore]
        public virtual BuildingDepotWithdrawal? Withdrawal { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(200)]
        public string? SupplierName { get; set; }

        [StringLength(34)]
        public string? SupplierIBAN { get; set; }

        [StringLength(500)]
        public string? ReceiptFileName { get; set; }

        [StringLength(1000)]
        public string? ReceiptFilePath { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
