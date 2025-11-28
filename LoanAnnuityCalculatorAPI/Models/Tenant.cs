using System.ComponentModel.DataAnnotations;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Represents an asset manager organization (e.g., Asset Manager ABC)
    /// Complete data isolation between tenants
    /// </summary>
    public class Tenant
    {
        [Key]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Unique database encryption key for this tenant
        /// Used for additional data isolation
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string DatabaseKey { get; set; } = Guid.NewGuid().ToString();

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeactivatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Fund> Funds { get; set; } = new List<Fund>();
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual TenantSubscription? Subscription { get; set; }
        public virtual ICollection<UsageTracking> UsageHistory { get; set; } = new List<UsageTracking>();
    }
}
