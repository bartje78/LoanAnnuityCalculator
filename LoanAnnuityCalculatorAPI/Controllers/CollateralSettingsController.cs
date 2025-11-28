using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using OfficeOpenXml;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollateralSettingsController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;
        private readonly ILogger<CollateralSettingsController> _logger;
        
        public CollateralSettingsController(LoanDbContext dbContext, ILogger<CollateralSettingsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET: api/CollateralSettings
        [HttpGet]
        public async Task<IActionResult> GetAllCollaterals()
        {
            try
            {
                var collaterals = await _dbContext.Collaterals
                    .Include(c => c.LoanCollaterals)
                        .ThenInclude(lc => lc.Loan)
                    .OrderByDescending(c => c.CreatedDate)
                    .ToListAsync();

                var result = collaterals.Select(c => new
                {
                    CollateralId = c.CollateralId,
                    CollateralType = c.CollateralType,
                    Description = c.Description,
                    AppraisalValue = c.AppraisalValue,
                    AppraisalDate = c.AppraisalDate,
                    PropertyType = c.PropertyType,
                    SecurityType = c.SecurityType,
                    FirstMortgageAmount = c.FirstMortgageAmount,
                    LiquidityHaircut = c.LiquidityHaircut,
                    PropertyAddress = c.PropertyAddress,
                    LandRegistryCode = c.LandRegistryCode,
                    PostalCode = c.PostalCode,
                    HouseNumber = c.HouseNumber,
                    AssetUniqueId = c.AssetUniqueId,
                    CreatedDate = c.CreatedDate,
                    LastUpdatedDate = c.LastUpdatedDate,
                    LastAppraisalUpdate = c.LastAppraisalUpdate,
                    AssociatedLoans = c.LoanCollaterals?.Select(lc => new
                    {
                        LoanId = lc.LoanId,
                        Priority = lc.Priority,
                        AllocationPercentage = lc.AllocationPercentage,
                        AssignedDate = lc.AssignedDate
                    }) ?? Enumerable.Empty<object>()
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching collaterals.", error = ex.Message });
            }
        }

        // GET: api/CollateralSettings/types
        [HttpGet("types")]
        public async Task<IActionResult> GetCollateralTypes()
        {
            try
            {
                // Get types from both CollateralIndexes (market data) and Collaterals PropertyType (specific real estate subsets)
                var indexTypes = await _dbContext.CollateralIndexes
                    .Select(ci => ci.CollateralType)
                    .Distinct()
                    .ToListAsync();

                var propertyTypes = await _dbContext.Collaterals
                    .Select(c => c.PropertyType)
                    .Distinct()
                    .ToListAsync();

                // Combine and deduplicate both lists, focusing on property types (subsets)
                var allTypes = indexTypes.Union(propertyTypes)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .OrderBy(t => t)
                    .ToList();

                return Ok(allTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching collateral types.", error = ex.Message });
            }
        }

        // GET: api/CollateralSettings/property-types
        [HttpGet("property-types")]
        public async Task<IActionResult> GetPropertyTypes()
        {
            try
            {
                // Get real estate property types from CollateralIndexes (uploaded market data)
                var propertyTypes = await _dbContext.CollateralIndexes
                    .Select(ci => ci.CollateralType)
                    .Distinct()
                    .Where(t => !string.IsNullOrEmpty(t))
                    .OrderBy(t => t)
                    .ToListAsync();

                return Ok(propertyTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching property types.", error = ex.Message });
            }
        }

        // GET: api/CollateralSettings/indices/{collateralType}
        [HttpGet("indices/{collateralType}")]
        public async Task<IActionResult> GetCollateralIndices(string collateralType)
        {
            try
            {
                var indices = await _dbContext.CollateralIndexes
                    .Where(ci => ci.CollateralType == collateralType)
                    .OrderBy(ci => ci.Quarter)
                    .ToListAsync();

                return Ok(indices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching collateral indices.", error = ex.Message });
            }
        }

        // POST: api/CollateralSettings/upload/{collateralType}
        [HttpPost("upload/{collateralType}")]
        public async Task<IActionResult> UploadExcelFile(string collateralType, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded." });
                }

                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    return BadRequest(new { message = "Only Excel files (.xlsx, .xls) are supported." });
                }

                List<(string Quarter, decimal PriceIndex)> indexData;
                try
                {
                    indexData = await ParseExcelFile(file);
                }
                catch (Exception parseEx)
                {
                    return BadRequest(new { message = $"Error parsing Excel file: {parseEx.Message}", error = parseEx.Message });
                }
                
                if (indexData.Count == 0)
                {
                    return BadRequest(new { message = "No valid data found in the Excel file." });
                }

                // Remove existing data for this collateral type
                var existingData = await _dbContext.CollateralIndexes
                    .Where(ci => ci.CollateralType == collateralType)
                    .ToListAsync();

                if (existingData.Any())
                {
                    _dbContext.CollateralIndexes.RemoveRange(existingData);
                }

                // Add new data
                var newIndices = indexData.Select(data => new CollateralIndex
                {
                    CollateralType = collateralType,
                    Quarter = data.Quarter,
                    PriceIndex = data.PriceIndex,
                    CreatedDate = DateTime.UtcNow
                }).ToList();

                await _dbContext.CollateralIndexes.AddRangeAsync(newIndices);
                await _dbContext.SaveChangesAsync();

                return Ok(new 
                { 
                    message = $"Successfully uploaded {newIndices.Count} index records for {collateralType}.",
                    recordsUploaded = newIndices.Count,
                    collateralType = collateralType
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error processing Excel file.", error = ex.Message });
            }
        }

        // DELETE: api/CollateralSettings/indices/{collateralType}
        [HttpDelete("indices/{collateralType}")]
        public async Task<IActionResult> DeleteCollateralTypeIndices(string collateralType)
        {
            try
            {
                var indices = await _dbContext.CollateralIndexes
                    .Where(ci => ci.CollateralType == collateralType)
                    .ToListAsync();

                if (!indices.Any())
                {
                    return NotFound(new { message = $"No indices found for collateral type: {collateralType}" });
                }

                _dbContext.CollateralIndexes.RemoveRange(indices);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = $"Successfully deleted {indices.Count} index records for {collateralType}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting collateral indices.", error = ex.Message });
            }
        }

        // GET: api/CollateralSettings/indexed-value/{collateralType}/{appraisalValue}/{appraisalDate}
        [HttpGet("indexed-value/{collateralType}/{appraisalValue:decimal}/{appraisalDate:datetime}")]
        public async Task<IActionResult> GetIndexedValue(string collateralType, decimal appraisalValue, DateTime appraisalDate)
        {
            try
            {
                var indexedValue = await CalculateIndexedValue(collateralType, appraisalValue, appraisalDate);
                
                return Ok(new 
                { 
                    originalValue = appraisalValue,
                    indexedValue = indexedValue,
                    appraisalDate = appraisalDate,
                    collateralType = collateralType,
                    indexationDate = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error calculating indexed value.", error = ex.Message });
            }
        }

        // GET: api/CollateralSettings/indexed-collateral/{propertyType}
        [HttpGet("indexed-collateral/{propertyType}")]
        public async Task<IActionResult> GetIndexedCollateralValues(string propertyType)
        {
            try
            {
                var collaterals = await _dbContext.Collaterals
                    .Include(c => c.LoanCollaterals)
                        .ThenInclude(lc => lc.Loan)
                    .Where(c => c.PropertyType == propertyType && c.AppraisalValue.HasValue && c.AppraisalDate.HasValue)
                    .ToListAsync();

                // Pre-calculate indexed values for all collaterals
                var collateralIndexedValues = new Dictionary<int, decimal>();
                foreach (var collateral in collaterals)
                {
                    try
                    {
                        var indexedValue = await CalculateIndexedValue(propertyType, collateral.AppraisalValue!.Value, collateral.AppraisalDate!.Value);
                        collateralIndexedValues[collateral.CollateralId] = indexedValue;
                    }
                    catch
                    {
                        // Fallback to appraisal value if indexing fails
                        collateralIndexedValues[collateral.CollateralId] = collateral.AppraisalValue!.Value;
                    }
                }

                var indexedCollaterals = new List<object>();

                foreach (var collateral in collaterals)
                {
                    try
                    {
                        var indexedValue = collateralIndexedValues[collateral.CollateralId];
                        
                        // Calculate haircut amount
                        var haircutAmount = collateral.LiquidityHaircut > 0 
                            ? indexedValue * (collateral.LiquidityHaircut / 100m) 
                            : 0;
                        
                        // For each loan that uses this collateral
                        foreach (var loanCollateral in collateral.LoanCollaterals)
                        {
                            // Calculate pro-rata subordination for this specific loan
                            var proRataSubordination = CalculateProRataSubordination(
                                collateral, 
                                loanCollateral.LoanId, 
                                indexedValue,
                                collaterals,
                                collateralIndexedValues);
                            
                            // Calculate effective value with pro-rata subordination
                            var effectiveCollateralValue = CalculateEffectiveCollateralValueWithProRataSubordination(
                                indexedValue, 
                                collateral, 
                                proRataSubordination);
                            
                            indexedCollaterals.Add(new
                            {
                                collateralId = collateral.CollateralId,
                                loanId = loanCollateral.LoanId,
                            description = collateral.Description,
                            securityType = collateral.SecurityType,
                            firstMortgageAmount = collateral.FirstMortgageAmount,
                            liquidityHaircut = collateral.LiquidityHaircut,
                            originalValue = collateral.AppraisalValue.Value,
                            indexedValue = indexedValue,
                            effectiveCollateralValue = effectiveCollateralValue,
                            appraisalDate = collateral.AppraisalDate.Value,
                            propertyType = collateral.PropertyType,
                            indexationDate = DateTime.Now,
                            valueChange = indexedValue - collateral.AppraisalValue.Value,
                            valueChangePercentage = collateral.AppraisalValue.Value > 0 ? 
                                Math.Round(((indexedValue - collateral.AppraisalValue.Value) / collateral.AppraisalValue.Value) * 100, 2) : 0,
                            haircutAmount = haircutAmount,
                            subordinationAmount = proRataSubordination,
                            securityAdjustment = indexedValue - effectiveCollateralValue,
                            priority = loanCollateral.Priority,
                            allocationPercentage = loanCollateral.AllocationPercentage
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error for this specific collateral but continue processing others
                        Console.WriteLine($"Error processing collateral {collateral.CollateralId}: {ex.Message}");
                        
                        // Add the collateral with original value if indexing fails for each loan
                        var effectiveCollateralValue = CalculateEffectiveCollateralValue(collateral.AppraisalValue!.Value, collateral);
                        
                        // Calculate haircut amount separately
                        var haircutAmount = collateral.LiquidityHaircut > 0 
                            ? collateral.AppraisalValue.Value * (collateral.LiquidityHaircut / 100m) 
                            : 0;
                        
                        // First mortgage amount (subordination)
                        var firstMortgageAmount = collateral.SecurityType == "2nd Mortgage" && collateral.FirstMortgageAmount.HasValue 
                            ? collateral.FirstMortgageAmount.Value 
                            : 0;
                        
                        foreach (var loanCollateral in collateral.LoanCollaterals)
                        {
                            indexedCollaterals.Add(new
                            {
                                collateralId = collateral.CollateralId,
                                loanId = loanCollateral.LoanId,
                                description = collateral.Description,
                                securityType = collateral.SecurityType,
                                firstMortgageAmount = collateral.FirstMortgageAmount,
                                liquidityHaircut = collateral.LiquidityHaircut,
                                originalValue = collateral.AppraisalValue!.Value,
                                indexedValue = collateral.AppraisalValue.Value, // Use original value as fallback
                                effectiveCollateralValue = effectiveCollateralValue,
                                appraisalDate = collateral.AppraisalDate!.Value,
                                propertyType = collateral.PropertyType,
                                indexationDate = DateTime.Now,
                                valueChange = 0,
                                valueChangePercentage = 0,
                                haircutAmount = haircutAmount,
                                subordinationAmount = firstMortgageAmount,
                                securityAdjustment = collateral.AppraisalValue.Value - effectiveCollateralValue,
                                priority = loanCollateral.Priority,
                                allocationPercentage = loanCollateral.AllocationPercentage,
                                error = "No index data available"
                            });
                        }
                    }
                }

                return Ok(indexedCollaterals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error calculating indexed collateral values.", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        private async Task<List<(string Quarter, decimal PriceIndex)>> ParseExcelFile(IFormFile file)
        {
            var result = new List<(string Quarter, decimal PriceIndex)>();

            try
            {
                // Set EPPlus license for non-commercial use
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using (var stream = file.OpenReadStream())
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        if (package.Workbook.Worksheets.Count == 0)
                        {
                            throw new InvalidOperationException("Excel file contains no worksheets.");
                        }

                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            throw new InvalidOperationException("No worksheet found in the Excel file.");
                        }

                        if (worksheet.Dimension == null)
                        {
                            throw new InvalidOperationException("Worksheet is empty or has no data.");
                        }

                        var rowCount = worksheet.Dimension.Rows;
                        var colCount = worksheet.Dimension.Columns;
                        
                        if (rowCount < 2)
                        {
                            throw new InvalidOperationException("Excel file must have at least 2 rows (header + data).");
                        }
                        
                        if (colCount < 2)
                        {
                            throw new InvalidOperationException("Excel file must have at least 2 columns (Quarter and PriceIndex).");
                        }

                        // Check headers
                        var header1 = worksheet.Cells[1, 1].Value?.ToString()?.Trim();
                        var header2 = worksheet.Cells[1, 2].Value?.ToString()?.Trim();
                        
                        // Log what headers we found for debugging
                        Console.WriteLine($"Found headers: '{header1}', '{header2}'");
                        
                        // Validate headers (case-insensitive)
                        if (!string.Equals(header1, "Quarter", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException($"First column header should be 'Quarter' but found '{header1}'");
                        }
                        
                        if (!string.Equals(header2, "PriceIndex", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException($"Second column header should be 'PriceIndex' but found '{header2}'");
                        }
                        
                        // Start from row 2 to skip header
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var quarterValue = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                                var priceIndexValue = worksheet.Cells[row, 2].Value;

                                if (string.IsNullOrEmpty(quarterValue) || priceIndexValue == null)
                                    continue;

                                // Validate quarter format (e.g., "2015Q3")
                                if (!IsValidQuarterFormat(quarterValue))
                                    continue;

                                if (decimal.TryParse(priceIndexValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal priceIndex))
                                {
                                    result.Add((quarterValue, priceIndex));
                                }
                            }
                            catch
                            {
                                // Skip invalid rows
                                continue;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse Excel file. Rows found: {result.Count}. Detailed error: {ex.Message}", ex);
            }

            return result;
        }

        private bool IsValidQuarterFormat(string quarter)
        {
            if (string.IsNullOrEmpty(quarter) || quarter.Length != 6)
                return false;

            // Format should be YYYYQN (e.g., "2015Q3")
            var yearPart = quarter.Substring(0, 4);
            var quarterPart = quarter.Substring(4, 2);

            return int.TryParse(yearPart, out int year) && 
                   year >= 2000 && year <= 2100 &&
                   (quarterPart == "Q1" || quarterPart == "Q2" || quarterPart == "Q3" || quarterPart == "Q4");
        }

        private async Task<decimal> CalculateIndexedValue(string collateralType, decimal appraisalValue, DateTime appraisalDate)
        {
            // Get the quarter for the appraisal date
            var appraisalQuarter = GetQuarterFromDate(appraisalDate);
            
            // Get the latest available quarter
            var latestIndex = await _dbContext.CollateralIndexes
                .Where(ci => ci.CollateralType == collateralType)
                .OrderByDescending(ci => ci.Quarter)
                .FirstOrDefaultAsync();

            if (latestIndex == null)
            {
                // No index data available, return original value
                return appraisalValue;
            }

            // Get the index for the appraisal quarter (or closest available)
            var appraisalIndex = await _dbContext.CollateralIndexes
                .Where(ci => ci.CollateralType == collateralType && ci.Quarter == appraisalQuarter)
                .FirstOrDefaultAsync();

            if (appraisalIndex == null)
            {
                // Find the closest available index before or on the appraisal date
                // Load all indices for this type into memory to use the QuarterDate property
                var allIndices = await _dbContext.CollateralIndexes
                    .Where(ci => ci.CollateralType == collateralType)
                    .ToListAsync();
                
                appraisalIndex = allIndices
                    .Where(ci => ci.QuarterDate <= appraisalDate)
                    .OrderByDescending(ci => ci.QuarterDate)
                    .FirstOrDefault();
            }

            if (appraisalIndex == null)
            {
                // No suitable index found, return original value
                return appraisalValue;
            }

            // Calculate indexed value: (Current Index / Appraisal Index) * Appraisal Value
            var indexationFactor = latestIndex.PriceIndex / appraisalIndex.PriceIndex;
            return appraisalValue * indexationFactor;
        }

        private string GetQuarterFromDate(DateTime date)
        {
            int quarter = (date.Month - 1) / 3 + 1;
            return $"{date.Year}Q{quarter}";
        }

        /// <summary>
        /// Calculate pro-rata subordination amount for a specific collateral and loan.
        /// When multiple collaterals secure the same loan as 2nd mortgages:
        /// 1. Take the max FirstMortgageAmount (assumes user enters same total on each property)
        /// 2. Allocate proportionally based on indexed market values
        /// This prevents double-counting when user enters the total first lien on each property.
        /// </summary>
        private decimal CalculateProRataSubordination(
            Collateral currentCollateral,
            int loanId,
            decimal currentIndexedValue,
            IEnumerable<Collateral> allCollaterals,
            Dictionary<int, decimal> collateralIndexedValues)
        {
            // Only apply pro-rata logic for 2nd mortgages with first mortgage amounts
            if (currentCollateral.SecurityType == null || 
                !currentCollateral.SecurityType.Equals("2nd Mortgage", StringComparison.OrdinalIgnoreCase) ||
                !currentCollateral.FirstMortgageAmount.HasValue ||
                currentCollateral.FirstMortgageAmount.Value <= 0)
            {
                return 0;
            }

            // Find ALL 2nd mortgage collaterals linked to the same loan
            var relatedCollaterals = allCollaterals
                .Where(c => c.LoanCollaterals.Any(lc => lc.LoanId == loanId) &&
                           c.SecurityType != null &&
                           c.SecurityType.Equals("2nd Mortgage", StringComparison.OrdinalIgnoreCase) &&
                           c.FirstMortgageAmount.HasValue &&
                           c.FirstMortgageAmount.Value > 0)
                .ToList();

            // If only one collateral, use its full FirstMortgageAmount
            if (relatedCollaterals.Count <= 1)
            {
                _logger.LogInformation("Single 2nd mortgage collateral for loan {LoanId}, using full subordination: {Amount}", 
                    loanId, currentCollateral.FirstMortgageAmount.Value);
                return currentCollateral.FirstMortgageAmount.Value;
            }

            // Multiple collaterals: use MAX FirstMortgageAmount as the total
            // (assumes user enters the same total first lien amount on each property)
            var totalFirstMortgageAmount = relatedCollaterals.Max(c => c.FirstMortgageAmount!.Value);

            // Calculate total indexed value of all related collaterals
            var totalIndexedValue = relatedCollaterals
                .Where(c => collateralIndexedValues.ContainsKey(c.CollateralId))
                .Sum(c => collateralIndexedValues[c.CollateralId]);

            if (totalIndexedValue <= 0)
            {
                _logger.LogWarning("Total indexed value is 0 for loan {LoanId}, cannot allocate subordination", loanId);
                return 0;
            }

            // Calculate pro-rata share for this collateral based on its market value
            var proRataShare = currentIndexedValue / totalIndexedValue;
            var allocatedSubordination = totalFirstMortgageAmount * proRataShare;

            _logger.LogInformation(
                "Pro-rata subordination for Collateral {CollateralId} on Loan {LoanId}: " +
                "({CurrentValue} / {TotalValue}) * {TotalSubordination} = {AllocatedSubordination} " +
                "({Share:P1} of total across {RelatedCount} collaterals)",
                currentCollateral.CollateralId, loanId, currentIndexedValue, totalIndexedValue, 
                totalFirstMortgageAmount, allocatedSubordination, proRataShare, relatedCollaterals.Count);

            return allocatedSubordination;
        }

        /// <summary>
        /// Calculate effective collateral value with pro-rata subordination already calculated
        /// </summary>
        private decimal CalculateEffectiveCollateralValueWithProRataSubordination(
            decimal indexedValue, 
            Collateral collateral, 
            decimal proRataSubordination)
        {
            var effectiveValue = indexedValue;
            
            _logger.LogInformation("CalculateEffectiveCollateralValue - CollateralId: {CollateralId}, " +
                "SecurityType: '{SecurityType}', ProRataSubordination: {ProRataSubordination}, " +
                "IndexedValue: {IndexedValue}", 
                collateral.CollateralId, collateral.SecurityType, proRataSubordination, indexedValue);
            
            // Apply liquidity haircut (percentage discount)
            if (collateral.LiquidityHaircut > 0)
            {
                var haircutAmount = effectiveValue * (collateral.LiquidityHaircut / 100m);
                effectiveValue = effectiveValue - haircutAmount;
                _logger.LogInformation("Applied haircut of {HaircutPercent}%, reduced by {HaircutAmount}", 
                    collateral.LiquidityHaircut, haircutAmount);
            }
            
            // Subtract pro-rata subordination amount
            if (proRataSubordination > 0)
            {
                effectiveValue = effectiveValue - proRataSubordination;
                _logger.LogInformation("Deducted pro-rata subordination of {Subordination}, " +
                    "effectiveValue now: {EffectiveValue}", 
                    proRataSubordination, effectiveValue);
            }

            // Ensure non-negative value
            var finalValue = Math.Max(0, effectiveValue);
            _logger.LogInformation("Final effective value: {FinalValue}", finalValue);
            
            return finalValue;
        }

        private decimal CalculateEffectiveCollateralValue(decimal indexedValue, Collateral collateral)
        {
            // Start with the indexed value
            var effectiveValue = indexedValue;
            
            _logger.LogInformation("CalculateEffectiveCollateralValue - CollateralId: {CollateralId}, " +
                "SecurityType: '{SecurityType}', FirstMortgageAmount: {FirstMortgageAmount}, " +
                "IndexedValue: {IndexedValue}", 
                collateral.CollateralId, collateral.SecurityType, collateral.FirstMortgageAmount, indexedValue);
            
            // Apply liquidity haircut (percentage discount)
            if (collateral.LiquidityHaircut > 0)
            {
                var haircutAmount = effectiveValue * (collateral.LiquidityHaircut / 100m);
                effectiveValue = effectiveValue - haircutAmount;
                _logger.LogInformation("Applied haircut of {HaircutPercent}%, reduced by {HaircutAmount}", 
                    collateral.LiquidityHaircut, haircutAmount);
            }
            
            // If it's a 2nd mortgage and has a first mortgage amount, subtract it
            // Use case-insensitive comparison to handle potential variations
            if (collateral.SecurityType != null && 
                collateral.SecurityType.Equals("2nd Mortgage", StringComparison.OrdinalIgnoreCase) && 
                collateral.FirstMortgageAmount.HasValue && 
                collateral.FirstMortgageAmount.Value > 0)
            {
                effectiveValue = effectiveValue - collateral.FirstMortgageAmount.Value;
                _logger.LogInformation("Deducted first mortgage amount of {FirstMortgageAmount}, " +
                    "effectiveValue now: {EffectiveValue}", 
                    collateral.FirstMortgageAmount.Value, effectiveValue);
            }
            else
            {
                _logger.LogInformation("First mortgage NOT deducted - SecurityType: '{SecurityType}', " +
                    "FirstMortgageAmount: {FirstMortgageAmount}", 
                    collateral.SecurityType, collateral.FirstMortgageAmount);
            }

            // Ensure non-negative value
            var finalValue = Math.Max(0, effectiveValue);
            _logger.LogInformation("Final effective value: {FinalValue}", finalValue);
            
            return finalValue;
        }

        // GET: api/CollateralSettings/statistics/{collateralType}
        [HttpGet("statistics/{collateralType}")]
        public async Task<IActionResult> GetCollateralStatistics(string collateralType)
        {
            try
            {
                var indices = await _dbContext.CollateralIndexes
                    .Where(ci => ci.CollateralType == collateralType)
                    .OrderBy(ci => ci.Quarter)
                    .ToListAsync();

                if (indices.Count < 2)
                {
                    return Ok(new
                    {
                        CollateralType = collateralType,
                        DataPoints = indices.Count,
                        Message = "Insufficient data for statistical analysis (need at least 2 quarters)"
                    });
                }

                // Calculate quarterly returns
                var returns = new List<decimal>();
                for (int i = 1; i < indices.Count; i++)
                {
                    var previousValue = indices[i - 1].PriceIndex;
                    var currentValue = indices[i].PriceIndex;
                    
                    if (previousValue > 0)
                    {
                        var quarterlyReturn = (currentValue - previousValue) / previousValue;
                        returns.Add(quarterlyReturn);
                    }
                }

                if (returns.Count == 0)
                {
                    return Ok(new
                    {
                        CollateralType = collateralType,
                        DataPoints = indices.Count,
                        Message = "Unable to calculate returns"
                    });
                }

                // Calculate average quarterly return
                var avgQuarterlyReturn = returns.Average();
                
                // Annualize return (4 quarters per year, compound growth)
                var annualizedReturn = (decimal)Math.Pow((double)(1 + avgQuarterlyReturn), 4) - 1;

                // Calculate quarterly volatility (standard deviation of returns)
                var variance = returns.Sum(r => (r - avgQuarterlyReturn) * (r - avgQuarterlyReturn)) / returns.Count;
                var quarterlyVolatility = (decimal)Math.Sqrt((double)variance);
                
                // Annualize volatility (multiply by sqrt(4) for 4 quarters)
                var annualizedVolatility = quarterlyVolatility * (decimal)Math.Sqrt(4);

                // Calculate total cumulative return
                var startValue = indices.First().PriceIndex;
                var endValue = indices.Last().PriceIndex;
                var totalReturn = (endValue - startValue) / startValue;

                // Calculate time period
                var startQuarter = indices.First().Quarter;
                var endQuarter = indices.Last().Quarter;
                var yearsDiff = CalculateYearsBetweenQuarters(startQuarter, endQuarter);

                // Calculate CAGR (Compound Annual Growth Rate)
                var cagr = yearsDiff > 0 ? (decimal)Math.Pow((double)(endValue / startValue), 1.0 / yearsDiff) - 1 : 0;

                return Ok(new
                {
                    CollateralType = collateralType,
                    DataPoints = indices.Count,
                    StartQuarter = startQuarter,
                    EndQuarter = endQuarter,
                    YearsOfData = Math.Round(yearsDiff, 2),
                    
                    // Returns
                    AverageQuarterlyReturn = Math.Round(avgQuarterlyReturn * 100, 2), // as percentage
                    AnnualizedReturn = Math.Round(annualizedReturn * 100, 2), // as percentage
                    CAGR = Math.Round(cagr * 100, 2), // as percentage
                    TotalReturn = Math.Round(totalReturn * 100, 2), // as percentage
                    
                    // Volatility
                    QuarterlyVolatility = Math.Round(quarterlyVolatility * 100, 2), // as percentage
                    AnnualizedVolatility = Math.Round(annualizedVolatility * 100, 2), // as percentage
                    
                    // Price levels
                    StartPriceIndex = Math.Round(startValue, 2),
                    EndPriceIndex = Math.Round(endValue, 2),
                    MinPriceIndex = Math.Round(indices.Min(i => i.PriceIndex), 2),
                    MaxPriceIndex = Math.Round(indices.Max(i => i.PriceIndex), 2)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error calculating statistics.", error = ex.Message });
            }
        }

        private double CalculateYearsBetweenQuarters(string startQuarter, string endQuarter)
        {
            // Parse quarters like "2020Q1"
            var startParts = startQuarter.Split('Q');
            var endParts = endQuarter.Split('Q');
            
            if (startParts.Length != 2 || endParts.Length != 2)
                return 0;

            if (!int.TryParse(startParts[0], out int startYear) || !int.TryParse(startParts[1], out int startQ))
                return 0;
            
            if (!int.TryParse(endParts[0], out int endYear) || !int.TryParse(endParts[1], out int endQ))
                return 0;

            // Convert to total quarters
            int startTotalQuarters = startYear * 4 + startQ;
            int endTotalQuarters = endYear * 4 + endQ;
            
            int quartersDiff = endTotalQuarters - startTotalQuarters;
            return quartersDiff / 4.0;
        }
    }
}
