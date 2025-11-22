using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Models.Loan;
using Microsoft.EntityFrameworkCore;

namespace LoanAnnuityCalculatorAPI.Services
{
    public interface IStatusCalculationService
    {
        Task<string> CalculateStatusAsync(Loan loan);
        Task<string> GetDefaultActiveStatusAsync();
        Task<string> GetCompletedStatusAsync();
    }

    public class StatusCalculationService : IStatusCalculationService
    {
        private readonly LoanDbContext _context;

        public StatusCalculationService(LoanDbContext context)
        {
            _context = context;
        }

        public async Task<string> CalculateStatusAsync(Loan loan)
        {
            // If the status is already set in the database and not empty, return it
            if (!string.IsNullOrEmpty(loan.Status))
            {
                return loan.Status;
            }

            // Calculate monthsDifference
            var monthsDifference = (DateTime.Now.Year - loan.StartDate.Year) * 12 + DateTime.Now.Month - loan.StartDate.Month;

            // Get the appropriate status based on calculation logic
            if (monthsDifference <= loan.TenorMonths)
            {
                return await GetDefaultActiveStatusAsync();
            }
            else
            {
                return await GetCompletedStatusAsync();
            }
        }

        public async Task<string> GetDefaultActiveStatusAsync()
        {
            // Get the default active status or the first active status with ActiveTenor calculation type
            var activeStatus = await _context.LoanStatuses
                .Where(s => s.IsActive && s.CalculationType == "ActiveTenor")
                .OrderBy(s => s.IsDefault ? 0 : 1)
                .ThenBy(s => s.SortOrder)
                .FirstOrDefaultAsync();

            return activeStatus?.StatusName ?? "Aktief"; // Fallback
        }

        public async Task<string> GetCompletedStatusAsync()
        {
            // Get the completed status
            var completedStatus = await _context.LoanStatuses
                .Where(s => s.IsActive && s.CalculationType == "Completed")
                .OrderBy(s => s.SortOrder)
                .FirstOrDefaultAsync();

            return completedStatus?.StatusName ?? "Afgelost"; // Fallback
        }
    }
}