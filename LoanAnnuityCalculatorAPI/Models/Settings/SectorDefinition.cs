using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models.Settings
{
    /// <summary>
    /// Editable sector definitions for revenue classification
    /// Allows users to customize sector names and characteristics
    /// </summary>
    public class SectorDefinition
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ModelSettingsId { get; set; }
        
        [ForeignKey("ModelSettingsId")]
        public ModelSettings ModelSettings { get; set; } = null!;
        
        /// <summary>
        /// Internal enum value (e.g., "Manufacturing", "Retail")
        /// Used for code logic and default correlations
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string SectorCode { get; set; } = string.Empty;
        
        /// <summary>
        /// Display name shown in UI (editable by user)
        /// E.g., "Productie & Fabricage" instead of "Manufacturing"
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional description of what this sector includes
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether this sector is active and available for selection
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Display order in dropdown lists
        /// </summary>
        public int DisplayOrder { get; set; } = 0;
        
        /// <summary>
        /// Default volatility for this sector (can be overridden in ModelSettings)
        /// </summary>
        [Range(0.0, 1.0)]
        public decimal DefaultVolatility { get; set; } = 0.15m;
        
        /// <summary>
        /// Expected annual growth rate for this sector
        /// </summary>
        [Range(-0.5, 0.5)]
        public decimal ExpectedGrowth { get; set; } = 0.02m;
        
        /// <summary>
        /// Color code for charts and visualizations (hex format)
        /// </summary>
        [MaxLength(7)]
        public string ColorCode { get; set; } = "#6366f1"; // Default indigo
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
