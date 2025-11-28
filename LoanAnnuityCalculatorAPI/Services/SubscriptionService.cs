using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LoanAnnuityCalculatorAPI.Services
{
    public interface ISubscriptionService
    {
        Task<TenantSubscription?> GetTenantSubscription(int tenantId);
        Task<TenantUsageSummary> GetUsageSummary(int tenantId);
        Task<bool> CanAddUser(int tenantId);
        Task<bool> CanAddFund(int tenantId);
        Task<bool> CanAddDebtor(int tenantId);
        Task<bool> CanAddLoan(int tenantId);
        Task<bool> IsFeatureEnabled(int tenantId, string feature);
        Task UpdateUsageTracking(int tenantId);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly LoanDbContext _dbContext;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(LoanDbContext dbContext, ILogger<SubscriptionService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<TenantSubscription?> GetTenantSubscription(int tenantId)
        {
            return await _dbContext.TenantSubscriptions
                .Include(s => s.PaymentPlan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active);
        }

        public async Task<TenantUsageSummary> GetUsageSummary(int tenantId)
        {
            var subscription = await GetTenantSubscription(tenantId);
            
            var userCount = await _dbContext.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive);
            var fundCount = await _dbContext.Funds.CountAsync(f => f.TenantId == tenantId && f.IsActive);
            var debtorCount = await _dbContext.DebtorDetails.CountAsync(d => d.TenantId == tenantId);
            var loanCount = await _dbContext.Loans.CountAsync(l => l.TenantId == tenantId);

            // Calculate storage (simplified - in production, calculate actual file sizes)
            var storageUsed = (debtorCount + loanCount) * 0.1m; // Rough estimate: 0.1 MB per record

            return new TenantUsageSummary
            {
                TenantId = tenantId,
                CurrentUsers = userCount,
                CurrentFunds = fundCount,
                CurrentDebtors = debtorCount,
                CurrentLoans = loanCount,
                StorageUsedMB = storageUsed,
                MaxUsers = subscription?.GetEffectiveMaxUsers() ?? int.MaxValue,
                MaxFunds = subscription?.GetEffectiveMaxFunds() ?? int.MaxValue,
                MaxDebtors = subscription?.GetEffectiveMaxDebtors() ?? int.MaxValue,
                MaxLoans = subscription?.GetEffectiveMaxLoans() ?? int.MaxValue,
                StorageLimitMB = subscription?.GetEffectiveStorageLimit() ?? int.MaxValue,
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<bool> CanAddUser(int tenantId)
        {
            var summary = await GetUsageSummary(tenantId);
            return !summary.IsUserLimitExceeded;
        }

        public async Task<bool> CanAddFund(int tenantId)
        {
            var summary = await GetUsageSummary(tenantId);
            return !summary.IsFundLimitExceeded;
        }

        public async Task<bool> CanAddDebtor(int tenantId)
        {
            var summary = await GetUsageSummary(tenantId);
            return !summary.IsDebtorLimitExceeded;
        }

        public async Task<bool> CanAddLoan(int tenantId)
        {
            var summary = await GetUsageSummary(tenantId);
            return !summary.IsLoanLimitExceeded;
        }

        public async Task<bool> IsFeatureEnabled(int tenantId, string feature)
        {
            var subscription = await GetTenantSubscription(tenantId);
            if (subscription == null)
                return false;

            return subscription.IsFeatureEnabled(feature);
        }

        public async Task UpdateUsageTracking(int tenantId)
        {
            var now = DateTime.UtcNow;
            var summary = await GetUsageSummary(tenantId);

            var tracking = new UsageTracking
            {
                TenantId = tenantId,
                RecordDate = now,
                Year = now.Year,
                Month = now.Month,
                ActiveUserCount = summary.CurrentUsers,
                TotalUserCount = await _dbContext.Users.CountAsync(u => u.TenantId == tenantId),
                FundCount = summary.CurrentFunds,
                DebtorCount = summary.CurrentDebtors,
                LoanCount = summary.CurrentLoans,
                StorageUsedMB = summary.StorageUsedMB
            };

            _dbContext.UsageTrackings.Add(tracking);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Usage tracking updated for tenant {TenantId}", tenantId);
        }
    }
}
