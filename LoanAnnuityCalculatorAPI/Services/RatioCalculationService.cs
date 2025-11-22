using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models.Debtor; // Updated to use DebtorBalanceSheet and DebtorPL
using Microsoft.EntityFrameworkCore;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class RatioCalculationService
    {
        private readonly LoanDbContext _dbContext;

        public RatioCalculationService(LoanDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Fetch all debtors
        public async Task<List<DebtorDetails>> GetDebtorsAsync()
        {
            return await _dbContext.DebtorDetails.ToListAsync();
        }

        // Fetch the latest balance sheet and profit & loss statement for a debtor
        public async Task<(DebtorBalanceSheet? LatestBalanceSheet, DebtorPL? LatestPL)> GetLatestFinancialDataAsync(int debtorId)
        {
            var debtor = await _dbContext.DebtorDetails
                .Include(d => d.BalanceSheets)
                .Include(d => d.ProfitAndLossStatements)
                .FirstOrDefaultAsync(d => d.DebtorID == debtorId);

            if (debtor == null)
            {
                return (null, null);
            }

            var latestBalanceSheet = debtor.BalanceSheets.OrderByDescending(bs => bs.BookYear).FirstOrDefault();
            var latestPL = debtor.ProfitAndLossStatements.OrderByDescending(pl => pl.BookYear).FirstOrDefault();

            return (latestBalanceSheet, latestPL);
        }

        // Calculate financial ratios
        public object CalculateRatios(DebtorBalanceSheet? latestBalanceSheet, DebtorPL? latestPL)
        {
            if (latestBalanceSheet == null || latestPL == null)
            {
                throw new ArgumentNullException("Balance sheet or profit and loss statement cannot be null.");
            }

            return new
            {
                DebtToEquity = latestBalanceSheet.OwnersEquity != 0
                    ? latestBalanceSheet.LongTermLiabilities / latestBalanceSheet.OwnersEquity
                    : 0, // Avoid division by zero
                CurrentRatio = latestBalanceSheet.CurrentLiabilities != 0
                    ? latestBalanceSheet.CurrentAssets / latestBalanceSheet.CurrentLiabilities
                    : 0, // Avoid division by zero
                NetProfitMargin = latestPL.Revenue != 0
                    ? latestPL.NetIncome / latestPL.Revenue
                    : 0, // Avoid division by zero
                InterestCoverage = latestPL.InterestExpense != 0
                    ? latestPL.EBITDA / latestPL.InterestExpense
                    : 0, // Avoid division by zero
                CreditRating = "To Be Calculated" // Placeholder for credit rating logic
            };
        }

        // Fetch all balance sheets for a debtor
        public async Task<List<DebtorBalanceSheet>> GetBalanceSheetsAsync(int debtorId)
        {
            return await _dbContext.DebtorBalanceSheets
                .Where(bs => bs.DebtorID == debtorId)
                .ToListAsync();
        }

        // Fetch a specific year's balance sheet for a debtor
        public async Task<DebtorBalanceSheet?> GetBalanceSheetForYearAsync(int debtorId, int year)
        {
            return await _dbContext.DebtorBalanceSheets
                .Where(bs => bs.DebtorID == debtorId && bs.BookYear == year)
                .FirstOrDefaultAsync();
        }
    }
}