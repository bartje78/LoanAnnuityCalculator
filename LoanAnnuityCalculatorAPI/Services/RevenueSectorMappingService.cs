using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Models.Debtor;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class RevenueSectorMappingService
    {
        // Maps RevenueCategory strings to standardized Sector enum
        private static readonly Dictionary<string, Sector> CategoryToSectorMap = new()
        {
            // Manufacturing
            { "Product Sales", Sector.Manufacturing },
            { "Manufacturing Revenue", Sector.Manufacturing },
            { "Production Income", Sector.Manufacturing },
            { "Goods Sold", Sector.Manufacturing },
            
            // Retail
            { "Retail Sales", Sector.Retail },
            { "Wholesale Revenue", Sector.Retail },
            { "Store Sales", Sector.Retail },
            { "E-commerce Revenue", Sector.Retail },
            
            // Real Estate
            { "Rental Income", Sector.RealEstate },
            { "Property Revenue", Sector.RealEstate },
            { "Real Estate Income", Sector.RealEstate },
            { "Lease Income", Sector.RealEstate },
            
            // Healthcare
            { "Healthcare Services", Sector.Healthcare },
            { "Medical Revenue", Sector.Healthcare },
            { "Patient Services", Sector.Healthcare },
            { "Clinical Income", Sector.Healthcare },
            
            // Technology
            { "Software Revenue", Sector.Technology },
            { "Technology Services", Sector.Technology },
            { "IT Services", Sector.Technology },
            { "SaaS Revenue", Sector.Technology },
            { "Licensing Revenue", Sector.Technology },
            
            // Professional Services
            { "Consulting Revenue", Sector.ProfessionalServices },
            { "Advisory Services", Sector.ProfessionalServices },
            { "Professional Fees", Sector.ProfessionalServices },
            { "Service Revenue", Sector.ProfessionalServices },
            
            // Hospitality
            { "Hotel Revenue", Sector.Hospitality },
            { "Restaurant Sales", Sector.Hospitality },
            { "Hospitality Income", Sector.Hospitality },
            { "Tourism Revenue", Sector.Hospitality },
            { "Event Revenue", Sector.Hospitality },
            
            // Agriculture
            { "Agricultural Sales", Sector.Agriculture },
            { "Farm Revenue", Sector.Agriculture },
            { "Crop Sales", Sector.Agriculture },
            { "Livestock Revenue", Sector.Agriculture },
            
            // Construction
            { "Construction Revenue", Sector.Construction },
            { "Project Revenue", Sector.Construction },
            { "Building Services", Sector.Construction },
            { "Contracting Revenue", Sector.Construction },
            
            // Financial Services
            { "Financial Services", Sector.FinancialServices },
            { "Investment Income", Sector.FinancialServices },
            { "Banking Revenue", Sector.FinancialServices },
            { "Interest Income", Sector.FinancialServices },
            
            // Transportation
            { "Transportation Revenue", Sector.Transportation },
            { "Logistics Services", Sector.Transportation },
            { "Freight Revenue", Sector.Transportation },
            { "Delivery Services", Sector.Transportation },
            
            // Other/Default
            { "Other Revenue", Sector.Other },
            { "Miscellaneous", Sector.Other },
            { "Other Income", Sector.Other }
        };

        /// <summary>
        /// Maps a revenue category string to a standardized sector
        /// Uses fuzzy matching if exact match not found
        /// </summary>
        public Sector MapCategoryToSector(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return Sector.Other;

            var normalized = category.Trim();
            
            // Try exact match first
            if (CategoryToSectorMap.TryGetValue(normalized, out var sector))
                return sector;

            // Try case-insensitive match
            var match = CategoryToSectorMap.FirstOrDefault(kvp => 
                kvp.Key.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            
            if (!match.Equals(default(KeyValuePair<string, Sector>)))
                return match.Value;

            // Try partial match (contains)
            match = CategoryToSectorMap.FirstOrDefault(kvp => 
                kvp.Key.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase));
            
            if (!match.Equals(default(KeyValuePair<string, Sector>)))
                return match.Value;

            // Default to Other if no match found
            return Sector.Other;
        }

        /// <summary>
        /// Calculates sector exposure weights for a debtor based on their latest P&L
        /// Returns dictionary of Sector -> weight (0-1, sum to 1.0)
        /// </summary>
        public Dictionary<Sector, decimal> CalculateSectorExposure(DebtorPL latestPL)
        {
            if (latestPL?.RevenueDetails == null || !latestPL.RevenueDetails.Any())
            {
                // No revenue breakdown - default to 100% Other
                return new Dictionary<Sector, decimal> { { Sector.Other, 1.0m } };
            }

            var totalRevenue = latestPL.RevenueDetails.Sum(r => r.Amount);
            
            if (totalRevenue <= 0)
            {
                return new Dictionary<Sector, decimal> { { Sector.Other, 1.0m } };
            }

            // Group by sector and calculate weights
            var sectorWeights = latestPL.RevenueDetails
                .GroupBy(r => MapCategoryToSector(r.RevenueCategory))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r => r.Amount) / totalRevenue
                );

            return sectorWeights;
        }

        /// <summary>
        /// Get all available revenue categories mapped to their sectors
        /// Useful for UI dropdowns
        /// </summary>
        public Dictionary<string, Sector> GetAllMappings()
        {
            return new Dictionary<string, Sector>(CategoryToSectorMap);
        }

        /// <summary>
        /// Get suggested revenue category names for a sector
        /// </summary>
        public List<string> GetCategoriesForSector(Sector sector)
        {
            return CategoryToSectorMap
                .Where(kvp => kvp.Value == sector)
                .Select(kvp => kvp.Key)
                .OrderBy(s => s)
                .ToList();
        }
    }
}
