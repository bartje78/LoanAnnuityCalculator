using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class CollateralValidationService
    {
        private readonly LoanDbContext _dbContext;
        private const decimal APPRAISAL_VALUE_TOLERANCE = 0.01m; // 1% tolerance for appraisal value differences

        public CollateralValidationService(LoanDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Creates or links collateral to a loan, ensuring data integrity and preventing duplicates
        /// </summary>
        public async Task<(bool Success, string Message, int? CollateralId)> CreateOrLinkCollateralAsync(
            int loanId, 
            Collateral newCollateral)
        {
            try
            {
                // Clean up empty strings to null values to prevent unique constraint violations
                CleanCollateralStrings(newCollateral);

                // Verify loan exists
                var loan = await _dbContext.Loans.FindAsync(loanId);
                if (loan == null)
                {
                    return (false, "Loan not found.", null);
                }

                // Find existing collateral with same unique identifier
                var existingCollateral = await FindExistingCollateralAsync(newCollateral);

                if (existingCollateral != null)
                {
                    // Validate appraisal value consistency
                    var validationResult = ValidateAppraisalValueConsistency(existingCollateral, newCollateral);
                    if (!validationResult.IsValid)
                    {
                        return (false, validationResult.ErrorMessage, null);
                    }

                    // Check if this loan is already linked to this collateral
                    var existingLink = await _dbContext.LoanCollaterals
                        .FirstOrDefaultAsync(lc => lc.LoanId == loanId && lc.CollateralId == existingCollateral.CollateralId);

                    if (existingLink != null)
                    {
                        return (false, "This loan is already secured by this collateral.", existingCollateral.CollateralId);
                    }

                    // Update existing collateral with latest information (if needed)
                    await UpdateCollateralInfoAsync(existingCollateral, newCollateral);

                    // Link existing collateral to new loan
                    await LinkCollateralToLoanAsync(loanId, existingCollateral.CollateralId, newCollateral);

                    return (true, "Loan successfully linked to existing collateral.", existingCollateral.CollateralId);
                }
                else
                {
                    // Create new collateral and link to loan
                    var createdCollateral = await CreateNewCollateralAsync(newCollateral);
                    await LinkCollateralToLoanAsync(loanId, createdCollateral.CollateralId, newCollateral);

                    return (true, "New collateral created and linked to loan.", createdCollateral.CollateralId);
                }
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Finds existing collateral with matching unique identifiers
        /// Priority: LandRegistryCode > PostalCode+HouseNumber > AssetUniqueId
        /// </summary>
        private async Task<Collateral?> FindExistingCollateralAsync(Collateral collateral)
        {
            // Priority 1: Land Registry Code (for plots of land)
            if (!string.IsNullOrWhiteSpace(collateral.LandRegistryCode))
            {
                return await _dbContext.Collaterals
                    .FirstOrDefaultAsync(c => c.LandRegistryCode == collateral.LandRegistryCode);
            }

            // Priority 2: Postal Code + House Number combination (for properties)
            if (!string.IsNullOrWhiteSpace(collateral.PostalCode) && 
                !string.IsNullOrWhiteSpace(collateral.HouseNumber))
            {
                return await _dbContext.Collaterals
                    .FirstOrDefaultAsync(c => c.PostalCode == collateral.PostalCode && 
                                            c.HouseNumber == collateral.HouseNumber);
            }

            // Priority 3: Generic Asset Unique ID (for other assets like vehicles, equipment)
            if (!string.IsNullOrWhiteSpace(collateral.AssetUniqueId))
            {
                return await _dbContext.Collaterals
                    .FirstOrDefaultAsync(c => c.AssetUniqueId == collateral.AssetUniqueId);
            }

            return null;
        }

        /// <summary>
        /// Validates that appraisal values are consistent for the same asset
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateAppraisalValueConsistency(
            Collateral existingCollateral, 
            Collateral newCollateral)
        {
            // If either doesn't have an appraisal value, no conflict
            if (!existingCollateral.AppraisalValue.HasValue || !newCollateral.AppraisalValue.HasValue)
            {
                return (true, string.Empty);
            }

            var existingValue = existingCollateral.AppraisalValue.Value;
            var newValue = newCollateral.AppraisalValue.Value;

            // Check if values are significantly different
            var percentageDifference = Math.Abs(existingValue - newValue) / existingValue;

            if (percentageDifference > APPRAISAL_VALUE_TOLERANCE)
            {
                var identifier = GetCollateralIdentifier(existingCollateral);
                return (false, 
                    $"Appraisal value mismatch for collateral '{identifier}'. " +
                    $"Existing value: {existingValue:C}, New value: {newValue:C}. " +
                    $"Difference of {percentageDifference:P2} exceeds tolerance of {APPRAISAL_VALUE_TOLERANCE:P2}. " +
                    "Please use the existing collateral record or update the appraisal value if this is a new valuation.");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Updates existing collateral with newer information if applicable
        /// </summary>
        private async Task UpdateCollateralInfoAsync(Collateral existingCollateral, Collateral newCollateral)
        {
            bool hasUpdates = false;

            // Update appraisal if new one is more recent
            if (newCollateral.AppraisalDate.HasValue && 
                (!existingCollateral.AppraisalDate.HasValue || 
                 newCollateral.AppraisalDate.Value > existingCollateral.AppraisalDate.Value))
            {
                existingCollateral.AppraisalValue = newCollateral.AppraisalValue;
                existingCollateral.AppraisalDate = newCollateral.AppraisalDate;
                existingCollateral.LastAppraisalUpdate = DateTime.UtcNow;
                hasUpdates = true;
            }

            // Update description if existing is empty or new one is more detailed
            if (string.IsNullOrWhiteSpace(existingCollateral.Description) && 
                !string.IsNullOrWhiteSpace(newCollateral.Description))
            {
                existingCollateral.Description = newCollateral.Description;
                hasUpdates = true;
            }

            // Update property type if existing is empty
            if (string.IsNullOrWhiteSpace(existingCollateral.PropertyType) && 
                !string.IsNullOrWhiteSpace(newCollateral.PropertyType))
            {
                existingCollateral.PropertyType = newCollateral.PropertyType;
                hasUpdates = true;
            }

            if (hasUpdates)
            {
                existingCollateral.LastUpdatedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Creates a new collateral record
        /// </summary>
        private async Task<Collateral> CreateNewCollateralAsync(Collateral collateral)
        {
            collateral.CreatedDate = DateTime.UtcNow;
            collateral.LastUpdatedDate = DateTime.UtcNow;
            
            if (collateral.AppraisalDate.HasValue)
            {
                collateral.LastAppraisalUpdate = DateTime.UtcNow;
            }

            _dbContext.Collaterals.Add(collateral);
            await _dbContext.SaveChangesAsync();
            
            return collateral;
        }

        /// <summary>
        /// Links a collateral to a loan through the junction table
        /// </summary>
        private async Task LinkCollateralToLoanAsync(int loanId, int collateralId, Collateral collateralData)
        {
            // Determine priority based on security type
            int priority = DeterminePriority(collateralData.SecurityType);

            var loanCollateral = new LoanCollateral
            {
                LoanId = loanId,
                CollateralId = collateralId,
                AssignedDate = DateTime.UtcNow,
                Priority = priority,
                AllocationPercentage = 100.00m, // Default to 100%, can be adjusted later
                Notes = $"Auto-linked via security type: {collateralData.SecurityType}"
            };

            _dbContext.LoanCollaterals.Add(loanCollateral);
            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Determines priority based on security type
        /// </summary>
        private int DeterminePriority(string? securityType)
        {
            return securityType?.ToLower() switch
            {
                "1st mortgage" => 1,
                "2nd mortgage" => 2,
                "3rd mortgage" => 3,
                "general security" => 4,
                "guarantee" => 5,
                "unsecured" => 6,
                _ => 4 // Default to general security priority
            };
        }

        /// <summary>
        /// Gets a human-readable identifier for the collateral
        /// </summary>
        private string GetCollateralIdentifier(Collateral collateral)
        {
            if (!string.IsNullOrWhiteSpace(collateral.LandRegistryCode))
                return $"Land Registry: {collateral.LandRegistryCode}";
            
            if (!string.IsNullOrWhiteSpace(collateral.PostalCode) && 
                !string.IsNullOrWhiteSpace(collateral.HouseNumber))
                return $"Property: {collateral.PostalCode} {collateral.HouseNumber}";
            
            if (!string.IsNullOrWhiteSpace(collateral.AssetUniqueId))
                return $"Asset ID: {collateral.AssetUniqueId}";
            
            return $"Collateral ID: {collateral.CollateralId}";
        }

        /// <summary>
        /// Gets all loans secured by the same collateral
        /// </summary>
        public async Task<List<Models.Loan.Loan>> GetLoansSecuredByCollateralAsync(int collateralId)
        {
            var loans = await _dbContext.LoanCollaterals
                .Where(lc => lc.CollateralId == collateralId)
                .Include(lc => lc.Loan)
                .ThenInclude(l => l!.DebtorDetails)
                .Select(lc => lc.Loan)
                .Where(l => l != null)
                .ToListAsync();

            return loans.Cast<Models.Loan.Loan>().ToList();
        }

        /// <summary>
        /// Cleans up collateral strings by converting empty strings to null
        /// This prevents unique constraint violations in the database
        /// </summary>
        private void CleanCollateralStrings(Collateral collateral)
        {
            // Convert empty strings to null for unique identifier fields
            if (string.IsNullOrWhiteSpace(collateral.LandRegistryCode))
                collateral.LandRegistryCode = null;
            
            if (string.IsNullOrWhiteSpace(collateral.PostalCode))
                collateral.PostalCode = null;
            
            if (string.IsNullOrWhiteSpace(collateral.HouseNumber))
                collateral.HouseNumber = null;
            
            if (string.IsNullOrWhiteSpace(collateral.AssetUniqueId))
                collateral.AssetUniqueId = null;
            
            if (string.IsNullOrWhiteSpace(collateral.PropertyAddress))
                collateral.PropertyAddress = null;
        }
    }
}