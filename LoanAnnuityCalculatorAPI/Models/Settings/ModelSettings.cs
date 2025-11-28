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
        
        // Sector-specific revenue volatility parameters for Monte Carlo
        public decimal SectorVolatilityManufacturing { get; set; } = 0.15m; // 15%
        public decimal SectorVolatilityRetail { get; set; } = 0.20m; // 20%
        public decimal SectorVolatilityRealEstate { get; set; } = 0.12m; // 12%
        public decimal SectorVolatilityHealthcare { get; set; } = 0.10m; // 10%
        public decimal SectorVolatilityTechnology { get; set; } = 0.25m; // 25% - high volatility
        public decimal SectorVolatilityProfessionalServices { get; set; } = 0.18m; // 18%
        public decimal SectorVolatilityHospitality { get; set; } = 0.30m; // 30% - very volatile
        public decimal SectorVolatilityAgriculture { get; set; } = 0.22m; // 22%
        public decimal SectorVolatilityConstruction { get; set; } = 0.20m; // 20%
        public decimal SectorVolatilityFinancialServices { get; set; } = 0.16m; // 16%
        public decimal SectorVolatilityTransportation { get; set; } = 0.18m; // 18%
        public decimal SectorVolatilityOther { get; set; } = 0.15m; // 15% - default
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property for property type-specific parameters
        public List<PropertyTypeParameters> PropertyTypeParameters { get; set; } = new List<PropertyTypeParameters>();
        
        // Navigation property for sector correlations
        public List<SectorCorrelation> SectorCorrelations { get; set; } = new List<SectorCorrelation>();
        
        // Navigation property for sector-collateral correlations
        public List<SectorCollateralCorrelation> SectorCollateralCorrelations { get; set; } = new List<SectorCollateralCorrelation>();
        
        // Navigation property for editable sector definitions
        public List<SectorDefinition> SectorDefinitions { get; set; } = new List<SectorDefinition>();
        
        /// <summary>
        /// Get volatility for a specific sector
        /// </summary>
        public decimal GetSectorVolatility(Sector sector)
        {
            return sector switch
            {
                Sector.Manufacturing => SectorVolatilityManufacturing,
                Sector.Retail => SectorVolatilityRetail,
                Sector.RealEstate => SectorVolatilityRealEstate,
                Sector.Healthcare => SectorVolatilityHealthcare,
                Sector.Technology => SectorVolatilityTechnology,
                Sector.ProfessionalServices => SectorVolatilityProfessionalServices,
                Sector.Hospitality => SectorVolatilityHospitality,
                Sector.Agriculture => SectorVolatilityAgriculture,
                Sector.Construction => SectorVolatilityConstruction,
                Sector.FinancialServices => SectorVolatilityFinancialServices,
                Sector.Transportation => SectorVolatilityTransportation,
                Sector.Other => SectorVolatilityOther,
                _ => SectorVolatilityOther
            };
        }
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
    
    public class SectorCorrelation
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ModelSettingsId { get; set; }
        
        [ForeignKey("ModelSettingsId")]
        public ModelSettings ModelSettings { get; set; } = null!;
        
        [Required]
        [MaxLength(50)]
        public string Sector1 { get; set; } = string.Empty; // Using string to store Sector enum name
        
        [Required]
        [MaxLength(50)]
        public string Sector2 { get; set; } = string.Empty; // Using string to store Sector enum name
        
        [Range(-1.0, 1.0)]
        public decimal CorrelationCoefficient { get; set; } = 0.35m; // Default cross-sector correlation
    }
    
    /// <summary>
    /// Correlation between revenue sectors and collateral property types
    /// Used for portfolio Monte Carlo to model realistic joint behavior
    /// </summary>
    public class SectorCollateralCorrelation
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ModelSettingsId { get; set; }
        
        [ForeignKey("ModelSettingsId")]
        public ModelSettings ModelSettings { get; set; } = null!;
        
        [Required]
        [MaxLength(50)]
        public string Sector { get; set; } = string.Empty; // Revenue sector
        
        [Required]
        [MaxLength(100)]
        public string PropertyType { get; set; } = string.Empty; // Collateral type
        
        [Range(-1.0, 1.0)]
        public decimal CorrelationCoefficient { get; set; } = 0.30m; // Default sector-collateral correlation
    }
}
