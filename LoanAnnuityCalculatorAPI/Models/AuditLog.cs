using System.ComponentModel.DataAnnotations;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Audit log entry for tracking sensitive operations
    /// Provides complete audit trail for compliance and security
    /// </summary>
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// User who performed the action
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public string UserName { get; set; } = string.Empty;
        
        /// <summary>
        /// Tenant context
        /// </summary>
        public int? TenantId { get; set; }
        
        /// <summary>
        /// Type of action (Create, Read, Update, Delete, Login, etc.)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;
        
        /// <summary>
        /// Entity type being accessed (Loan, Debtor, etc.)
        /// </summary>
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;
        
        /// <summary>
        /// ID of the entity being accessed
        /// </summary>
        public int? EntityId { get; set; }
        
        /// <summary>
        /// JSON representation of changes made
        /// </summary>
        public string? Changes { get; set; }
        
        /// <summary>
        /// Additional details or error messages
        /// </summary>
        public string? Details { get; set; }
        
        /// <summary>
        /// IP address of the request
        /// </summary>
        [MaxLength(45)] // IPv6 length
        public string IpAddress { get; set; } = string.Empty;
        
        /// <summary>
        /// User agent string
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        /// <summary>
        /// Request path
        /// </summary>
        [MaxLength(500)]
        public string? RequestPath { get; set; }
        
        /// <summary>
        /// HTTP method
        /// </summary>
        [MaxLength(10)]
        public string? HttpMethod { get; set; }
        
        /// <summary>
        /// Was the action successful
        /// </summary>
        public bool Success { get; set; } = true;
        
        /// <summary>
        /// Timestamp of the action
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
