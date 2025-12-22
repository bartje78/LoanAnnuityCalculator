using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class LtvCalculationService
    {
        private readonly LoanDbContext _dbContext;

        public LtvCalculationService(LoanDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Calculates the Loan-to-Value (LTV) ratio for a specific loan, considering:
        /// - Indexed collateral values
        /// - Liquidity haircuts
        /// - Loan-level first mortgage amounts (subordination)
        /// - Shared collateral across multiple loans
        /// </summary>
        public async Task<decimal> CalculateLtvAsync(int loanId)
        {
            // Load the loan with all its collaterals
            var loan = await _dbContext.Loans
                .Include(l => l.LoanCollaterals)
                    .ThenInclude(lc => lc.Collateral)
                .FirstOrDefaultAsync(l => l.LoanID == loanId);

            if (loan == null || loan.LoanCollaterals == null || !loan.LoanCollaterals.Any())
            {
                return 0;
            }

            // Use outstanding amount if available, otherwise fall back to loan amount
            decimal currentExposure = loan.OutstandingAmount > 0 ? loan.OutstandingAmount : loan.LoanAmount;

            // Step 1: Calculate total collateral value (indexed or appraisal)
            decimal totalCollateralValue = 0;
            foreach (var loanCollateral in loan.LoanCollaterals)
            {
                var collateral = loanCollateral.Collateral;
                if (collateral == null) continue;

                // Try to get indexed value
                var indexedValue = await GetIndexedCollateralValueAsync(collateral);
                if (indexedValue.HasValue)
                {
                    totalCollateralValue += indexedValue.Value;
                }
                else
                {
                    totalCollateralValue += collateral.AppraisalValue ?? 0;
                }
            }

            // Step 2: Apply liquidity haircuts
            decimal totalHaircut = 0;
            foreach (var loanCollateral in loan.LoanCollaterals)
            {
                var collateral = loanCollateral.Collateral;
                if (collateral == null) continue;

                if (collateral.LiquidityHaircut > 0)
                {
                    var collateralValue = await GetIndexedCollateralValueAsync(collateral);
                    if (!collateralValue.HasValue)
                    {
                        collateralValue = collateral.AppraisalValue.HasValue && collateral.AppraisalValue.Value > 0 
                            ? collateral.AppraisalValue.Value 
                            : 0;
                    }
                    var haircutAmount = collateralValue.Value * (collateral.LiquidityHaircut / 100);
                    totalHaircut += haircutAmount;
                }
            }

            decimal effectiveValue = totalCollateralValue - totalHaircut;

            // Step 3: Subtract loan-level first mortgage (subordination)
            if (loan.FirstMortgageAmount.HasValue && loan.FirstMortgageAmount.Value > 0)
            {
                effectiveValue = Math.Max(0, effectiveValue - loan.FirstMortgageAmount.Value);
            }

            // Step 4: Calculate total exposure including other loans sharing the same collateral
            decimal totalExposure = currentExposure;

            // Get all loans for the same debtor to check for shared collateral
            var allLoans = await _dbContext.Loans
                .Where(l => l.DebtorID == loan.DebtorID && l.LoanID != loanId)
                .Include(l => l.LoanCollaterals)
                    .ThenInclude(lc => lc.Collateral)
                .ToListAsync();

            foreach (var otherLoan in allLoans)
            {
                if (otherLoan.LoanCollaterals == null || !otherLoan.LoanCollaterals.Any())
                    continue;

                // Check if this other loan shares any collateral with the current loan
                bool sharesCollateral = false;
                foreach (var otherLoanCollateral in otherLoan.LoanCollaterals)
                {
                    foreach (var currentLoanCollateral in loan.LoanCollaterals)
                    {
                        if (IsSameCollateral(currentLoanCollateral.Collateral, otherLoanCollateral.Collateral))
                        {
                            sharesCollateral = true;
                            break;
                        }
                    }
                    if (sharesCollateral) break;
                }

                // Only add the loan amount once, even if it shares multiple collaterals
                if (sharesCollateral)
                {
                    decimal otherExposure = otherLoan.OutstandingAmount > 0 ? otherLoan.OutstandingAmount : otherLoan.LoanAmount;
                    totalExposure += otherExposure;
                }
            }

            // Step 5: Calculate final LTV
            if (effectiveValue == 0)
            {
                return 999; // Return high number if no effective collateral value
            }

            decimal ltv = (totalExposure / effectiveValue) * 100;
            return ltv;
        }

        /// <summary>
        /// Calculates LTV for all loans of a debtor
        /// </summary>
        public async Task<Dictionary<int, decimal>> CalculateLtvForDebtorLoansAsync(int debtorId)
        {
            var loanIds = await _dbContext.Loans
                .Where(l => l.DebtorID == debtorId)
                .Select(l => l.LoanID)
                .ToListAsync();

            var result = new Dictionary<int, decimal>();
            foreach (var loanId in loanIds)
            {
                result[loanId] = await CalculateLtvAsync(loanId);
            }

            return result;
        }

        private async Task<decimal?> GetIndexedCollateralValueAsync(Collateral collateral)
        {
            if (collateral == null || string.IsNullOrEmpty(collateral.PropertyType) || 
                collateral.PropertyType == "Not Applicable" || 
                !collateral.AppraisalValue.HasValue || !collateral.AppraisalDate.HasValue)
            {
                return collateral.AppraisalValue;
            }

            // Calculate indexed value using the same logic as CollateralSettingsController
            var appraisalQuarter = GetQuarterFromDate(collateral.AppraisalDate.Value);
            
            // Get the latest available index
            var latestIndex = await _dbContext.CollateralIndexes
                .Where(ci => ci.CollateralType == collateral.PropertyType)
                .OrderByDescending(ci => ci.Quarter)
                .FirstOrDefaultAsync();

            if (latestIndex == null)
            {
                return collateral.AppraisalValue.Value;
            }

            // Get the index for the appraisal quarter
            var appraisalIndex = await _dbContext.CollateralIndexes
                .Where(ci => ci.CollateralType == collateral.PropertyType && ci.Quarter == appraisalQuarter)
                .FirstOrDefaultAsync();

            if (appraisalIndex == null)
            {
                // Find the closest available index before or on the appraisal date
                var allIndices = await _dbContext.CollateralIndexes
                    .Where(ci => ci.CollateralType == collateral.PropertyType)
                    .ToListAsync();
                
                appraisalIndex = allIndices
                    .Where(ci => ci.QuarterDate <= collateral.AppraisalDate.Value)
                    .OrderByDescending(ci => ci.QuarterDate)
                    .FirstOrDefault();
            }

            if (appraisalIndex == null)
            {
                return collateral.AppraisalValue.Value;
            }

            // Calculate indexed value
            var indexationFactor = latestIndex.PriceIndex / appraisalIndex.PriceIndex;
            return collateral.AppraisalValue.Value * indexationFactor;
        }

        private string GetQuarterFromDate(DateTime date)
        {
            int quarter = (date.Month - 1) / 3 + 1;
            return $"{date.Year}Q{quarter}";
        }

        private bool IsSameCollateral(Collateral? collateral1, Collateral? collateral2)
        {
            if (collateral1 == null || collateral2 == null)
                return false;

            // Same CollateralId is a definite match
            if (collateral1.CollateralId == collateral2.CollateralId)
                return true;

            // Check land registry code (most reliable for real estate)
            if (!string.IsNullOrWhiteSpace(collateral1.LandRegistryCode) &&
                !string.IsNullOrWhiteSpace(collateral2.LandRegistryCode))
            {
                return collateral1.LandRegistryCode.Trim().Equals(collateral2.LandRegistryCode.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            // Check unique asset ID
            if (!string.IsNullOrWhiteSpace(collateral1.AssetUniqueId) &&
                !string.IsNullOrWhiteSpace(collateral2.AssetUniqueId))
            {
                return collateral1.AssetUniqueId.Trim().Equals(collateral2.AssetUniqueId.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            // Check property address components
            if (!string.IsNullOrWhiteSpace(collateral1.PropertyAddress) &&
                !string.IsNullOrWhiteSpace(collateral2.PropertyAddress) &&
                !string.IsNullOrWhiteSpace(collateral1.PostalCode) &&
                !string.IsNullOrWhiteSpace(collateral2.PostalCode))
            {
                return collateral1.PropertyAddress.Trim().Equals(collateral2.PropertyAddress.Trim(), StringComparison.OrdinalIgnoreCase) &&
                       collateral1.PostalCode.Trim().Equals(collateral2.PostalCode.Trim(), StringComparison.OrdinalIgnoreCase) &&
                       collateral1.HouseNumber?.Trim().Equals(collateral2.HouseNumber?.Trim(), StringComparison.OrdinalIgnoreCase) == true;
            }

            return false;
        }
    }
}
