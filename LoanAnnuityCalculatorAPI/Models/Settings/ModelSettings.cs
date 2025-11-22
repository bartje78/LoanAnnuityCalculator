using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models.Settings
{
    public class ModelSettings
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string SettingName { get; set; } = "Default";
        
        // Growth parameters
        public decimal DefaultRevenueGrowthRate { get; set; } = 0.00m; // 0% default
        public decimal DefaultOperatingCostGrowthRate { get; set; } = 0.02m; // 2% default (costs grow faster)
        
        // Default volatility parameters
        public decimal DefaultRevenueVolatility { get; set; } = 0.15m; // 15% default
        public decimal DefaultEbitdaMarginVolatility { get; set; } = 0.05m; // 5% default - DEPRECATED
        public decimal DefaultOperatingCostVolatility { get; set; } = 0.10m; // 10% default
        
        // Tax parameters
        public decimal DefaultCorporateTaxRate { get; set; } = 0.21m; // 21% Netherlands standard rate
        
        // Default collateral modeling parameters (fallback for non-real estate or unspecified types)
        public decimal DefaultCollateralExpectedReturn { get; set; } = 0.02m; // 2% annual appreciation
        public decimal DefaultCollateralVolatility { get; set; } = 0.10m; // 10% volatility
        public decimal DefaultCollateralCorrelation { get; set; } = 0.30m; // Correlation with revenue
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property for property type-specific parameters
        public List<PropertyTypeParameters> PropertyTypeParameters { get; set; } = new List<PropertyTypeParameters>();
    }
    
    public class PropertyTypeParameters
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ModelSettingsId { get; set; }
        
        [ForeignKey("ModelSettingsId")]
        public ModelSettings ModelSettings { get; set; } = null!;
        
        [Required]
        [MaxLength(100)]
        public string PropertyType { get; set; } = string.Empty;
        
        public decimal ExpectedReturn { get; set; } = 0.02m; // 2% default
        public decimal Volatility { get; set; } = 0.10m; // 10% default
        public decimal CorrelationWithRevenue { get; set; } = 0.30m; // 30% default
    }
}
