using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Models.Settings;
using Microsoft.EntityFrameworkCore;

namespace LoanAnnuityCalculatorAPI.Services
{
    /// <summary>
    /// Service for seeding default sector correlation values
    /// Based on empirical economic research and sector analysis
    /// </summary>
    public class SectorCorrelationSeedService
    {
        private readonly LoanDbContext _context;
        
        public SectorCorrelationSeedService(LoanDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Seeds default correlation matrix for all sector pairs AND sector-collateral correlations
        /// Correlations are symmetric, so we only store one direction (Sector1 < Sector2 alphabetically)
        /// </summary>
        public async Task SeedDefaultCorrelationsAsync(int modelSettingsId)
        {
            var modelSettings = await _context.ModelSettings
                .Include(m => m.SectorCorrelations)
                .Include(m => m.SectorCollateralCorrelations)
                .Include(m => m.SectorDefinitions)
                .FirstOrDefaultAsync(m => m.Id == modelSettingsId);
                
            if (modelSettings == null)
            {
                throw new InvalidOperationException($"ModelSettings with ID {modelSettingsId} not found");
            }
            
            // Seed sector definitions first (required for validation)
            await SeedSectorDefinitionsAsync(modelSettings);
            
            // Seed sector-to-sector correlations
            await SeedSectorSectorCorrelationsAsync(modelSettings);
            
            // Seed sector-to-collateral correlations
            await SeedSectorCollateralCorrelationsAsync(modelSettings);
            
            await _context.SaveChangesAsync();
        }
        
        /// <summary>
        /// Seeds editable sector definitions with Dutch display names
        /// </summary>
        private async Task SeedSectorDefinitionsAsync(ModelSettings modelSettings)
        {
            // If already seeded, skip
            if (modelSettings.SectorDefinitions.Any())
            {
                return;
            }
            
            var sectorDefinitions = new List<(Sector Code, string DisplayName, string Description, decimal Volatility, decimal ExpectedGrowth, string Color)>
            {
                (Sector.Manufacturing, "Productie & Fabricage", "Industriële productie, fabricage van goederen", 0.15m, 0.025m, "#6366f1"),
                (Sector.Retail, "Detailhandel", "Retail, winkels, e-commerce", 0.20m, 0.015m, "#ec4899"),
                (Sector.RealEstate, "Vastgoed", "Vastgoedontwikkeling, beheer en verhuur", 0.12m, 0.030m, "#8b5cf6"),
                (Sector.Healthcare, "Zorg & Welzijn", "Gezondheidszorg, medische diensten", 0.10m, 0.035m, "#10b981"),
                (Sector.Technology, "Technologie & IT", "Software, hardware, IT-diensten", 0.25m, 0.060m, "#3b82f6"),
                (Sector.ProfessionalServices, "Zakelijke Dienstverlening", "Advies, consultancy, zakelijke diensten", 0.18m, 0.025m, "#f59e0b"),
                (Sector.Hospitality, "Horeca & Toerisme", "Hotels, restaurants, recreatie", 0.30m, 0.020m, "#ef4444"),
                (Sector.Agriculture, "Landbouw & Visserij", "Agrarische sector, voedselproductie", 0.22m, 0.010m, "#84cc16"),
                (Sector.Construction, "Bouw & Infrastructuur", "Bouwsector, civiele techniek", 0.20m, 0.020m, "#f97316"),
                (Sector.FinancialServices, "Financiële Dienstverlening", "Banken, verzekeringen, financiële diensten", 0.16m, 0.025m, "#06b6d4"),
                (Sector.Transportation, "Transport & Logistiek", "Vervoer, logistiek, distributie", 0.18m, 0.020m, "#a855f7"),
                (Sector.Other, "Overig", "Overige sectoren", 0.15m, 0.020m, "#6b7280")
            };
            
            int order = 1;
            foreach (var (code, name, desc, volatility, growth, color) in sectorDefinitions)
            {
                modelSettings.SectorDefinitions.Add(new SectorDefinition
                {
                    ModelSettingsId = modelSettings.Id,
                    SectorCode = code.ToString(),
                    DisplayName = name,
                    Description = desc,
                    DefaultVolatility = volatility,
                    ExpectedGrowth = growth,
                    ColorCode = color,
                    IsActive = true,
                    DisplayOrder = order++
                });
            }
        }
        
        private async Task SeedSectorSectorCorrelationsAsync(ModelSettings modelSettings)
        {
            // If already seeded, skip
            if (modelSettings.SectorCorrelations.Any())
            {
                return;
            }
            
            // Define default correlations based on economic research
            // Higher correlations for related sectors, lower for unrelated
            var defaultCorrelations = new Dictionary<(Sector, Sector), decimal>
            {
                // Manufacturing correlations
                [(Sector.Manufacturing, Sector.Retail)] = 0.65m,
                [(Sector.Manufacturing, Sector.Construction)] = 0.70m,
                [(Sector.Manufacturing, Sector.Transportation)] = 0.60m,
                [(Sector.Manufacturing, Sector.Technology)] = 0.55m,
                [(Sector.Manufacturing, Sector.RealEstate)] = 0.45m,
                [(Sector.Manufacturing, Sector.FinancialServices)] = 0.50m,
                [(Sector.Manufacturing, Sector.ProfessionalServices)] = 0.40m,
                [(Sector.Manufacturing, Sector.Healthcare)] = 0.30m,
                [(Sector.Manufacturing, Sector.Hospitality)] = 0.35m,
                [(Sector.Manufacturing, Sector.Agriculture)] = 0.40m,
                [(Sector.Manufacturing, Sector.Other)] = 0.35m,
                
                // Retail correlations
                [(Sector.Retail, Sector.Hospitality)] = 0.75m,
                [(Sector.Retail, Sector.RealEstate)] = 0.60m,
                [(Sector.Retail, Sector.Transportation)] = 0.55m,
                [(Sector.Retail, Sector.Technology)] = 0.50m,
                [(Sector.Retail, Sector.FinancialServices)] = 0.55m,
                [(Sector.Retail, Sector.Construction)] = 0.50m,
                [(Sector.Retail, Sector.ProfessionalServices)] = 0.45m,
                [(Sector.Retail, Sector.Healthcare)] = 0.35m,
                [(Sector.Retail, Sector.Agriculture)] = 0.40m,
                [(Sector.Retail, Sector.Other)] = 0.40m,
                
                // Real Estate correlations
                [(Sector.RealEstate, Sector.Construction)] = 0.80m,
                [(Sector.RealEstate, Sector.FinancialServices)] = 0.70m,
                [(Sector.RealEstate, Sector.Hospitality)] = 0.65m,
                [(Sector.RealEstate, Sector.ProfessionalServices)] = 0.50m,
                [(Sector.RealEstate, Sector.Technology)] = 0.45m,
                [(Sector.RealEstate, Sector.Transportation)] = 0.40m,
                [(Sector.RealEstate, Sector.Healthcare)] = 0.35m,
                [(Sector.RealEstate, Sector.Agriculture)] = 0.30m,
                [(Sector.RealEstate, Sector.Other)] = 0.40m,
                
                // Healthcare correlations (more defensive)
                [(Sector.Healthcare, Sector.ProfessionalServices)] = 0.50m,
                [(Sector.Healthcare, Sector.Technology)] = 0.45m,
                [(Sector.Healthcare, Sector.FinancialServices)] = 0.40m,
                [(Sector.Healthcare, Sector.Hospitality)] = 0.25m,
                [(Sector.Healthcare, Sector.Construction)] = 0.30m,
                [(Sector.Healthcare, Sector.Transportation)] = 0.35m,
                [(Sector.Healthcare, Sector.Agriculture)] = 0.25m,
                [(Sector.Healthcare, Sector.Other)] = 0.30m,
                
                // Technology correlations
                [(Sector.Technology, Sector.ProfessionalServices)] = 0.70m,
                [(Sector.Technology, Sector.FinancialServices)] = 0.65m,
                [(Sector.Technology, Sector.Transportation)] = 0.55m,
                [(Sector.Technology, Sector.Construction)] = 0.45m,
                [(Sector.Technology, Sector.Hospitality)] = 0.50m,
                [(Sector.Technology, Sector.Agriculture)] = 0.35m,
                [(Sector.Technology, Sector.Other)] = 0.45m,
                
                // Professional Services correlations
                [(Sector.ProfessionalServices, Sector.FinancialServices)] = 0.75m,
                [(Sector.ProfessionalServices, Sector.Construction)] = 0.50m,
                [(Sector.ProfessionalServices, Sector.Hospitality)] = 0.45m,
                [(Sector.ProfessionalServices, Sector.Transportation)] = 0.45m,
                [(Sector.ProfessionalServices, Sector.Agriculture)] = 0.35m,
                [(Sector.ProfessionalServices, Sector.Other)] = 0.40m,
                
                // Hospitality correlations (cyclical)
                [(Sector.Hospitality, Sector.Transportation)] = 0.70m,
                [(Sector.Hospitality, Sector.Construction)] = 0.55m,
                [(Sector.Hospitality, Sector.FinancialServices)] = 0.60m,
                [(Sector.Hospitality, Sector.Agriculture)] = 0.45m,
                [(Sector.Hospitality, Sector.Other)] = 0.45m,
                
                // Agriculture correlations
                [(Sector.Agriculture, Sector.Transportation)] = 0.55m,
                [(Sector.Agriculture, Sector.Construction)] = 0.40m,
                [(Sector.Agriculture, Sector.FinancialServices)] = 0.45m,
                [(Sector.Agriculture, Sector.Other)] = 0.35m,
                
                // Construction correlations
                [(Sector.Construction, Sector.Transportation)] = 0.65m,
                [(Sector.Construction, Sector.FinancialServices)] = 0.60m,
                [(Sector.Construction, Sector.Other)] = 0.45m,
                
                // Financial Services correlations
                [(Sector.FinancialServices, Sector.Transportation)] = 0.55m,
                [(Sector.FinancialServices, Sector.Other)] = 0.45m,
                
                // Transportation correlations
                [(Sector.Transportation, Sector.Other)] = 0.40m
            };
            
            // Create SectorCorrelation entries
            foreach (var ((sector1, sector2), correlation) in defaultCorrelations)
            {
                // Ensure alphabetical order for consistency
                var (s1, s2) = sector1.ToString().CompareTo(sector2.ToString()) < 0 
                    ? (sector1, sector2) 
                    : (sector2, sector1);
                
                modelSettings.SectorCorrelations.Add(new SectorCorrelation
                {
                    ModelSettingsId = modelSettings.Id,
                    Sector1 = s1.ToString(),
                    Sector2 = s2.ToString(),
                    CorrelationCoefficient = correlation
                });
            }
        }
        
        /// <summary>
        /// Seeds sector-collateral correlations based on economic relationships
        /// Higher correlations when sector revenue directly depends on property type
        /// </summary>
        private async Task SeedSectorCollateralCorrelationsAsync(ModelSettings modelSettings)
        {
            // If already seeded, skip
            if (modelSettings.SectorCollateralCorrelations.Any())
            {
                return;
            }
            
            // Common property types (aligned with your existing PropertyTypeParameters)
            var propertyTypes = new[] { "Residential", "Commercial", "Industrial", "Land", "Mixed-Use", "Agricultural", "Office", "Retail Space", "Warehouse" };
            
            // Define sector-collateral correlations
            var sectorCollateralCorrelations = new Dictionary<(Sector, string), decimal>
            {
                // Manufacturing - strong correlation with industrial/warehouse, moderate with commercial
                [(Sector.Manufacturing, "Industrial")] = 0.70m,
                [(Sector.Manufacturing, "Warehouse")] = 0.65m,
                [(Sector.Manufacturing, "Commercial")] = 0.50m,
                [(Sector.Manufacturing, "Office")] = 0.40m,
                [(Sector.Manufacturing, "Land")] = 0.45m,
                [(Sector.Manufacturing, "Residential")] = 0.30m,
                [(Sector.Manufacturing, "Retail Space")] = 0.35m,
                [(Sector.Manufacturing, "Mixed-Use")] = 0.45m,
                [(Sector.Manufacturing, "Agricultural")] = 0.25m,
                
                // Retail - very strong with retail space, strong with commercial
                [(Sector.Retail, "Retail Space")] = 0.85m,
                [(Sector.Retail, "Commercial")] = 0.75m,
                [(Sector.Retail, "Mixed-Use")] = 0.70m,
                [(Sector.Retail, "Office")] = 0.45m,
                [(Sector.Retail, "Residential")] = 0.50m,
                [(Sector.Retail, "Warehouse")] = 0.40m,
                [(Sector.Retail, "Industrial")] = 0.35m,
                [(Sector.Retail, "Land")] = 0.40m,
                [(Sector.Retail, "Agricultural")] = 0.20m,
                
                // Real Estate - strong with all property types (cyclical)
                [(Sector.RealEstate, "Residential")] = 0.80m,
                [(Sector.RealEstate, "Commercial")] = 0.80m,
                [(Sector.RealEstate, "Office")] = 0.75m,
                [(Sector.RealEstate, "Mixed-Use")] = 0.85m,
                [(Sector.RealEstate, "Retail Space")] = 0.70m,
                [(Sector.RealEstate, "Industrial")] = 0.65m,
                [(Sector.RealEstate, "Warehouse")] = 0.60m,
                [(Sector.RealEstate, "Land")] = 0.75m,
                [(Sector.RealEstate, "Agricultural")] = 0.55m,
                
                // Healthcare - moderate with office/mixed-use, lower with others
                [(Sector.Healthcare, "Office")] = 0.55m,
                [(Sector.Healthcare, "Commercial")] = 0.50m,
                [(Sector.Healthcare, "Mixed-Use")] = 0.50m,
                [(Sector.Healthcare, "Residential")] = 0.30m,
                [(Sector.Healthcare, "Retail Space")] = 0.25m,
                [(Sector.Healthcare, "Industrial")] = 0.20m,
                [(Sector.Healthcare, "Warehouse")] = 0.20m,
                [(Sector.Healthcare, "Land")] = 0.25m,
                [(Sector.Healthcare, "Agricultural")] = 0.15m,
                
                // Technology - strong with office, moderate with commercial
                [(Sector.Technology, "Office")] = 0.75m,
                [(Sector.Technology, "Commercial")] = 0.60m,
                [(Sector.Technology, "Mixed-Use")] = 0.65m,
                [(Sector.Technology, "Industrial")] = 0.45m,
                [(Sector.Technology, "Warehouse")] = 0.40m,
                [(Sector.Technology, "Residential")] = 0.35m,
                [(Sector.Technology, "Retail Space")] = 0.30m,
                [(Sector.Technology, "Land")] = 0.30m,
                [(Sector.Technology, "Agricultural")] = 0.15m,
                
                // Professional Services - very strong with office
                [(Sector.ProfessionalServices, "Office")] = 0.80m,
                [(Sector.ProfessionalServices, "Commercial")] = 0.65m,
                [(Sector.ProfessionalServices, "Mixed-Use")] = 0.60m,
                [(Sector.ProfessionalServices, "Residential")] = 0.35m,
                [(Sector.ProfessionalServices, "Retail Space")] = 0.30m,
                [(Sector.ProfessionalServices, "Industrial")] = 0.30m,
                [(Sector.ProfessionalServices, "Warehouse")] = 0.25m,
                [(Sector.ProfessionalServices, "Land")] = 0.30m,
                [(Sector.ProfessionalServices, "Agricultural")] = 0.20m,
                
                // Hospitality - very strong with commercial, strong with mixed-use/residential
                [(Sector.Hospitality, "Commercial")] = 0.85m,
                [(Sector.Hospitality, "Mixed-Use")] = 0.75m,
                [(Sector.Hospitality, "Residential")] = 0.60m,
                [(Sector.Hospitality, "Retail Space")] = 0.65m,
                [(Sector.Hospitality, "Office")] = 0.45m,
                [(Sector.Hospitality, "Land")] = 0.50m,
                [(Sector.Hospitality, "Warehouse")] = 0.25m,
                [(Sector.Hospitality, "Industrial")] = 0.25m,
                [(Sector.Hospitality, "Agricultural")] = 0.30m,
                
                // Agriculture - very strong with agricultural land, moderate with land
                [(Sector.Agriculture, "Agricultural")] = 0.90m,
                [(Sector.Agriculture, "Land")] = 0.70m,
                [(Sector.Agriculture, "Warehouse")] = 0.40m,
                [(Sector.Agriculture, "Industrial")] = 0.35m,
                [(Sector.Agriculture, "Residential")] = 0.25m,
                [(Sector.Agriculture, "Commercial")] = 0.25m,
                [(Sector.Agriculture, "Office")] = 0.20m,
                [(Sector.Agriculture, "Retail Space")] = 0.20m,
                [(Sector.Agriculture, "Mixed-Use")] = 0.25m,
                
                // Construction - strong with all property types (directly builds them)
                [(Sector.Construction, "Residential")] = 0.75m,
                [(Sector.Construction, "Commercial")] = 0.75m,
                [(Sector.Construction, "Industrial")] = 0.70m,
                [(Sector.Construction, "Mixed-Use")] = 0.80m,
                [(Sector.Construction, "Office")] = 0.70m,
                [(Sector.Construction, "Retail Space")] = 0.65m,
                [(Sector.Construction, "Warehouse")] = 0.65m,
                [(Sector.Construction, "Land")] = 0.70m,
                [(Sector.Construction, "Agricultural")] = 0.50m,
                
                // Financial Services - strong with commercial/office (banks, etc.)
                [(Sector.FinancialServices, "Office")] = 0.70m,
                [(Sector.FinancialServices, "Commercial")] = 0.75m,
                [(Sector.FinancialServices, "Mixed-Use")] = 0.65m,
                [(Sector.FinancialServices, "Residential")] = 0.50m,
                [(Sector.FinancialServices, "Retail Space")] = 0.45m,
                [(Sector.FinancialServices, "Industrial")] = 0.40m,
                [(Sector.FinancialServices, "Warehouse")] = 0.35m,
                [(Sector.FinancialServices, "Land")] = 0.45m,
                [(Sector.FinancialServices, "Agricultural")] = 0.35m,
                
                // Transportation - strong with warehouse/industrial, moderate with commercial
                [(Sector.Transportation, "Warehouse")] = 0.75m,
                [(Sector.Transportation, "Industrial")] = 0.70m,
                [(Sector.Transportation, "Commercial")] = 0.55m,
                [(Sector.Transportation, "Land")] = 0.50m,
                [(Sector.Transportation, "Mixed-Use")] = 0.45m,
                [(Sector.Transportation, "Office")] = 0.40m,
                [(Sector.Transportation, "Retail Space")] = 0.45m,
                [(Sector.Transportation, "Residential")] = 0.35m,
                [(Sector.Transportation, "Agricultural")] = 0.40m,
                
                // Other - default moderate correlations
                [(Sector.Other, "Commercial")] = 0.40m,
                [(Sector.Other, "Office")] = 0.35m,
                [(Sector.Other, "Mixed-Use")] = 0.40m,
                [(Sector.Other, "Residential")] = 0.30m,
                [(Sector.Other, "Retail Space")] = 0.30m,
                [(Sector.Other, "Industrial")] = 0.35m,
                [(Sector.Other, "Warehouse")] = 0.30m,
                [(Sector.Other, "Land")] = 0.35m,
                [(Sector.Other, "Agricultural")] = 0.25m
            };
            
            // Create SectorCollateralCorrelation entries
            foreach (var ((sector, propertyType), correlation) in sectorCollateralCorrelations)
            {
                modelSettings.SectorCollateralCorrelations.Add(new SectorCollateralCorrelation
                {
                    ModelSettingsId = modelSettings.Id,
                    Sector = sector.ToString(),
                    PropertyType = propertyType,
                    CorrelationCoefficient = correlation
                });
            }
        }
        
        /// <summary>
        /// Builds a full correlation matrix (n x n) from the stored sector pairs
        /// Diagonal elements are 1.0 (perfect self-correlation)
        /// Off-diagonal elements are retrieved from database or default to 0.35
        /// </summary>
        public async Task<double[,]> BuildCorrelationMatrixAsync(int modelSettingsId)
        {
            var sectors = Enum.GetValues<Sector>();
            var n = sectors.Length;
            var matrix = new double[n, n];
            
            // Load all correlations for this model settings
            var correlations = await _context.SectorCorrelations
                .Where(sc => sc.ModelSettingsId == modelSettingsId)
                .ToListAsync();
            
            // Build dictionary for fast lookup
            var correlationDict = correlations.ToDictionary(
                sc => (sc.Sector1, sc.Sector2),
                sc => (double)sc.CorrelationCoefficient
            );
            
            // Fill matrix
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        matrix[i, j] = 1.0; // Perfect self-correlation
                    }
                    else
                    {
                        var sector1 = sectors[i];
                        var sector2 = sectors[j];
                        
                        // Ensure alphabetical order for lookup
                        var (s1, s2) = sector1.ToString().CompareTo(sector2.ToString()) < 0
                            ? (sector1.ToString(), sector2.ToString())
                            : (sector2.ToString(), sector1.ToString());
                        
                        // Lookup or use default
                        matrix[i, j] = correlationDict.TryGetValue((s1, s2), out var corr)
                            ? corr
                            : 0.35; // Default cross-sector correlation
                    }
                }
            }
            
            return matrix;
        }
        
        /// <summary>
        /// Gets a dictionary of sector volatilities from ModelSettings
        /// </summary>
        public async Task<Dictionary<Sector, decimal>> GetSectorVolatilitiesAsync(int modelSettingsId)
        {
            var modelSettings = await _context.ModelSettings
                .FirstOrDefaultAsync(m => m.Id == modelSettingsId);
                
            if (modelSettings == null)
            {
                throw new InvalidOperationException($"ModelSettings with ID {modelSettingsId} not found");
            }
            
            var volatilities = new Dictionary<Sector, decimal>();
            foreach (var sector in Enum.GetValues<Sector>())
            {
                volatilities[sector] = modelSettings.GetSectorVolatility(sector);
            }
            
            return volatilities;
        }
    }
}
