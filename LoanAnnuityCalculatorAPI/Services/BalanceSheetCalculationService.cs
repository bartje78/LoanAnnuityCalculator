using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Models.Loan;
using Microsoft.EntityFrameworkCore;

namespace LoanAnnuityCalculatorAPI.Services
{
    /// <summary>
    /// Service for calculating balance sheet totals and extracting financial data from BalanceSheetLineItems
    /// </summary>
    public class BalanceSheetCalculationService
    {
        private readonly LoanDbContext _dbContext;
        private readonly ILogger<BalanceSheetCalculationService> _logger;

        public BalanceSheetCalculationService(LoanDbContext dbContext, ILogger<BalanceSheetCalculationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Calculate all financial values from balance sheet line items
        /// </summary>
        public async Task<BalanceSheetCalculation?> CalculateFromLineItems(int debtorId, int? bookYear = null)
        {
            // Get the most recent balance sheet for this debtor
            var balanceSheetQuery = _dbContext.DebtorBalanceSheets
                .Where(bs => bs.DebtorID == debtorId);

            if (bookYear.HasValue)
            {
                balanceSheetQuery = balanceSheetQuery.Where(bs => bs.BookYear == bookYear.Value);
            }

            var balanceSheet = await balanceSheetQuery
                .OrderByDescending(bs => bs.BookYear)
                .FirstOrDefaultAsync();

            if (balanceSheet == null)
            {
                _logger.LogWarning("No balance sheet found for debtor {DebtorId}", debtorId);
                return null;
            }

            // Load all line items for this balance sheet
            var lineItems = await _dbContext.BalanceSheetLineItems
                .Where(li => li.BalanceSheetId == balanceSheet.Id)
                .Include(li => li.Loan)
                .Include(li => li.Collateral)
                .ToListAsync();

            if (!lineItems.Any())
            {
                _logger.LogWarning("No line items found for balance sheet {BalanceSheetId}", balanceSheet.Id);
                return null;
            }

            var calculation = new BalanceSheetCalculation
            {
                DebtorId = debtorId,
                BalanceSheetId = balanceSheet.Id,
                BookYear = balanceSheet.BookYear
            };

            // Calculate assets by category
            calculation.CurrentAssets = lineItems
                .Where(li => li.Category == "CurrentAssets")
                .Sum(li => li.Amount);

            calculation.FixedAssets = lineItems
                .Where(li => li.Category == "FixedAssets")
                .Sum(li => li.Amount);

            calculation.TotalAssets = calculation.CurrentAssets + calculation.FixedAssets;

            // Calculate liabilities by category
            calculation.CurrentLiabilities = lineItems
                .Where(li => li.Category == "CurrentLiabilities")
                .Sum(li => li.Amount);

            calculation.LongTermLiabilities = lineItems
                .Where(li => li.Category == "LongTermLiabilities")
                .Sum(li => li.Amount);

            calculation.TotalLiabilities = calculation.CurrentLiabilities + calculation.LongTermLiabilities;

            // Calculate equity from line items, or derive from accounting equation if not present
            var equityFromLineItems = lineItems
                .Where(li => li.Category == "Equity")
                .Sum(li => li.Amount);
            
            // If no equity line items exist, calculate using accounting equation: Assets = Liabilities + Equity
            calculation.Equity = equityFromLineItems != 0 
                ? equityFromLineItems 
                : calculation.TotalAssets - calculation.TotalLiabilities;

            // Verify accounting equation (allow small rounding errors)
            var imbalance = Math.Abs(calculation.TotalAssets - (calculation.TotalLiabilities + calculation.Equity));
            if (imbalance > 1) // Allow 1 EUR difference for rounding
            {
                _logger.LogWarning("Balance sheet imbalance detected: Assets={Assets}, Liabilities={Liabilities}, Equity={Equity}, Difference={Difference}",
                    calculation.TotalAssets, calculation.TotalLiabilities, calculation.Equity, imbalance);
            }

            // Extract loan-related information
            var loanLineItems = lineItems.Where(li => li.LoanId.HasValue).ToList();
            calculation.TotalLoanDebt = loanLineItems.Sum(li => li.Amount);
            calculation.LoanIds = loanLineItems.Select(li => li.LoanId!.Value).Distinct().ToList();

            // Extract collateral information
            var collateralLineItems = lineItems.Where(li => li.CollateralId.HasValue).ToList();
            calculation.TotalCollateralValue = collateralLineItems.Sum(li => li.Amount);
            calculation.CollateralIds = collateralLineItems.Select(li => li.CollateralId!.Value).Distinct().ToList();

            // Extract external loans (first lien mortgages)
            var externalLoans = lineItems
                .Where(li => IsExternalLoan(li))
                .ToList();

            if (externalLoans.Any())
            {
                calculation.ExternalLoans = externalLoans.Select(li => ParseExternalLoanDetails(li)).ToList();
                _logger.LogInformation("Found {Count} external loan(s) for debtor {DebtorId}", externalLoans.Count, debtorId);
            }

            // Calculate non-loan debt (other liabilities excluding our loans and external loans)
            var externalLoanAmount = calculation.ExternalLoans?.Sum(el => el.Amount) ?? 0;
            calculation.NonLoanDebt = calculation.TotalLiabilities - calculation.TotalLoanDebt - externalLoanAmount;

            _logger.LogInformation("Balance sheet calculation for debtor {DebtorId}: Assets={Assets}, Liabilities={Liabilities}, Equity={Equity}",
                debtorId, calculation.TotalAssets, calculation.TotalLiabilities, calculation.Equity);

            return calculation;
        }

        /// <summary>
        /// Get the outstanding amount for a specific loan from the most recent balance sheet
        /// </summary>
        public async Task<decimal?> GetLoanOutstandingAmount(int debtorId, int loanId)
        {
            // Get the most recent balance sheet for this debtor
            var balanceSheet = await _dbContext.DebtorBalanceSheets
                .Where(bs => bs.DebtorID == debtorId)
                .OrderByDescending(bs => bs.BookYear)
                .FirstOrDefaultAsync();

            if (balanceSheet == null)
            {
                return null;
            }

            // Find the line item for this loan
            var loanLineItem = await _dbContext.BalanceSheetLineItems
                .Where(li => li.BalanceSheetId == balanceSheet.Id && li.LoanId == loanId)
                .FirstOrDefaultAsync();

            return loanLineItem?.Amount;
        }

        /// <summary>
        /// Get outstanding amounts for multiple loans from the most recent balance sheet
        /// Returns a dictionary of loanId -> outstanding amount
        /// </summary>
        public async Task<Dictionary<int, decimal>> GetLoanOutstandingAmounts(int debtorId, List<int> loanIds)
        {
            var result = new Dictionary<int, decimal>();

            // Get the most recent balance sheet for this debtor
            var balanceSheet = await _dbContext.DebtorBalanceSheets
                .Where(bs => bs.DebtorID == debtorId)
                .OrderByDescending(bs => bs.BookYear)
                .FirstOrDefaultAsync();

            if (balanceSheet == null)
            {
                return result;
            }

            // Get all line items for these loans
            var loanLineItems = await _dbContext.BalanceSheetLineItems
                .Where(li => li.BalanceSheetId == balanceSheet.Id && 
                            li.LoanId.HasValue && 
                            loanIds.Contains(li.LoanId.Value))
                .ToListAsync();

            foreach (var lineItem in loanLineItems)
            {
                if (lineItem.LoanId.HasValue)
                {
                    result[lineItem.LoanId.Value] = lineItem.Amount;
                }
            }

            return result;
        }

        /// <summary>
        /// Check if a line item represents an external loan (1st lien mortgage)
        /// </summary>
        private bool IsExternalLoan(BalanceSheetLineItem item)
        {
            var label = item.Label?.ToLower() ?? "";
            return label.Contains("external") || 
                   label.Contains("1st lien") ||
                   label.Contains("eerste hypotheek") ||
                   label.Contains("1e hypotheek");
        }

        /// <summary>
        /// Parse external loan details from a line item
        /// </summary>
        private ExternalLoanInfo ParseExternalLoanDetails(BalanceSheetLineItem item)
        {
            var externalLoan = new ExternalLoanInfo
            {
                LineItemId = item.Id,
                Amount = item.Amount,
                Label = item.Label ?? "External Loan"
            };

            // Parse interest rate from label (format: "@ 4.00%")
            var rateMatch = System.Text.RegularExpressions.Regex.Match(item.Label ?? "", @"@\s*([\d.]+)%");
            if (rateMatch.Success && decimal.TryParse(rateMatch.Groups[1].Value, out decimal rate))
            {
                externalLoan.InterestRate = rate;
            }

            // Parse details from Notes field if present
            if (!string.IsNullOrEmpty(item.Notes))
            {
                // Parse tenor (format: "Tenor: 360 months")
                var tenorMatch = System.Text.RegularExpressions.Regex.Match(item.Notes, @"Tenor:\s*(\d+)\s*months");
                if (tenorMatch.Success && int.TryParse(tenorMatch.Groups[1].Value, out int tenor))
                {
                    externalLoan.TenorMonths = tenor;
                }

                // Parse redemption schedule (format: "Schedule: Annuity")
                var scheduleMatch = System.Text.RegularExpressions.Regex.Match(item.Notes, @"Schedule:\s*(\w+)");
                if (scheduleMatch.Success)
                {
                    externalLoan.RedemptionSchedule = scheduleMatch.Groups[1].Value;
                }
            }

            return externalLoan;
        }
    }

    /// <summary>
    /// Result of balance sheet calculation from line items
    /// </summary>
    public class BalanceSheetCalculation
    {
        public int DebtorId { get; set; }
        public int BalanceSheetId { get; set; }
        public int BookYear { get; set; }

        // Assets
        public decimal CurrentAssets { get; set; }
        public decimal FixedAssets { get; set; }
        public decimal TotalAssets { get; set; }

        // Liabilities
        public decimal CurrentLiabilities { get; set; }
        public decimal LongTermLiabilities { get; set; }
        public decimal TotalLiabilities { get; set; }

        // Equity
        public decimal Equity { get; set; }

        // Loan information
        public decimal TotalLoanDebt { get; set; }
        public List<int> LoanIds { get; set; } = new List<int>();

        // Collateral information
        public decimal TotalCollateralValue { get; set; }
        public List<int> CollateralIds { get; set; } = new List<int>();

        // External loans (first lien mortgages)
        public List<ExternalLoanInfo>? ExternalLoans { get; set; }

        // Non-loan debt (other liabilities)
        public decimal NonLoanDebt { get; set; }
    }

    /// <summary>
    /// Information about an external loan (1st lien mortgage)
    /// </summary>
    public class ExternalLoanInfo
    {
        public int LineItemId { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal? InterestRate { get; set; }
        public int? TenorMonths { get; set; }
        public string? RedemptionSchedule { get; set; }
    }
}
