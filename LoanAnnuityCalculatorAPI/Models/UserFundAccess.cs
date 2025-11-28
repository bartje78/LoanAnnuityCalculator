using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Defines which funds a user has access to within their tenant
    /// </summary>
    public class UserFundAccess
    {
        [Key]
        public int UserFundAccessId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int FundId { get; set; }

        /// <summary>
        /// Role within this fund: Viewer, Editor, Manager
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Viewer";

        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey("FundId")]
        public virtual Fund Fund { get; set; } = null!;
    }

    /// <summary>
    /// Fund-level roles
    /// </summary>
    public static class FundRoles
    {
        public const string Viewer = "Viewer";      // Read-only access
        public const string Editor = "Editor";      // Can create/edit data
        public const string Manager = "Manager";    // Full access including settings
    }
}
