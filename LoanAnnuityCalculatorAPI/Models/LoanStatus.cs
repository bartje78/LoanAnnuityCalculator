using System.ComponentModel.DataAnnotations;

namespace LoanAnnuityCalculatorAPI.Models
{
    public class LoanStatus
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string StatusName { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public bool IsActive { get; set; } = true;
        
        [Required]
        public bool IsDefault { get; set; } = false;
        
        [Required]
        public int SortOrder { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Optional properties for status calculation
        public bool IsCalculated { get; set; } = false;
        
        [MaxLength(50)]
        public string? CalculationType { get; set; } // "ActiveTenor", "Completed", "Manual"
    }
}