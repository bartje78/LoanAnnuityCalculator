using System.ComponentModel.DataAnnotations;

namespace LoanAnnuityCalculatorAPI.Models
{
    /// <summary>
    /// Standardized revenue categories for consistent sector classification
    /// Users select from this predefined list instead of free-text entry
    /// </summary>
    public class StandardRevenueCategory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Sector { get; set; } = nameof(Models.Sector.Other); // Maps to Sector enum
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public int DisplayOrder { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
