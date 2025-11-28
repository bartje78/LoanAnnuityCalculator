using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Application user with custom properties
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Tenant this user belongs to - CRITICAL for data isolation
        /// </summary>
        public int? TenantId { get; set; }

        /// <summary>
        /// System admin flag - can access all tenants (use sparingly!)
        /// </summary>
        public bool IsSystemAdmin { get; set; } = false;

        // Navigation properties
        [ForeignKey("TenantId")]
        public virtual Tenant? Tenant { get; set; }

        public virtual ICollection<UserFundAccess> FundAccesses { get; set; } = new List<UserFundAccess>();
        public virtual ICollection<UserAddOn> AddOns { get; set; } = new List<UserAddOn>();
    }
}
