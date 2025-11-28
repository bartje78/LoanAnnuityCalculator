using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Represents a loan fund within a tenant (e.g., "ABC Real Estate Fund I")
    /// </summary>
    public class Fund
    {
        [Key]
        public int FundId { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Fund reference code (e.g., "ABC-RE-I")
        /// </summary>
        [MaxLength(50)]
        public string? FundCode { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedAt { get; set; }

        // Navigation properties
        [ForeignKey("TenantId")]
        public virtual Tenant Tenant { get; set; } = null!;

        public virtual ICollection<UserFundAccess> UserAccesses { get; set; } = new List<UserFundAccess>();
    }
}
