using System;
using System.ComponentModel.DataAnnotations;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Represents a reusable text block for contract/offer generation
    /// </summary>
    public class ContractTextBlock
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Section { get; set; } = string.Empty; // e.g., "Introduction", "Terms", "Conditions", "Closing"

        [Required]
        public string Content { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Optional description of what this text block is for
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Insert payment breakdown chart after this text block
        /// </summary>
        public bool InsertPaymentChart { get; set; } = false;

        /// <summary>
        /// Insert BSE calculations table after this text block
        /// </summary>
        public bool InsertBseTable { get; set; } = false;

        /// <summary>
        /// Show the section name as a heading in the document (only for first block in section)
        /// </summary>
        public bool ShowSectionHeader { get; set; } = false;

        /// <summary>
        /// Conditional display: only show for specific redemption schedule (null = always show)
        /// Values: "Annuity", "Linear", "Bullet"
        /// </summary>
        [MaxLength(20)]
        public string? RedemptionScheduleType { get; set; }

        /// <summary>
        /// Conditional display: only show for specific security type (null = always show)
        /// Values: "FirstLien", "SecondLien", "NoSecurity"
        /// </summary>
        [MaxLength(20)]
        public string? SecurityType { get; set; }
    }
}
