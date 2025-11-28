using System.Security.Claims;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LoanAnnuityCalculatorAPI.Services
{
    public interface ITenantService
    {
        int? GetCurrentTenantId();
        Task<bool> HasFundAccess(int fundId, string? requiredRole = null);
        Task<List<int>> GetUserFundIds();
        Task<bool> ValidateFundBelongsToTenant(int fundId);
    }

    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LoanDbContext _dbContext;
        private readonly ILogger<TenantService> _logger;

        public TenantService(
            IHttpContextAccessor httpContextAccessor,
            LoanDbContext dbContext,
            ILogger<TenantService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
            _logger = logger;
        }

        public int? GetCurrentTenantId()
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId");
            if (tenantIdClaim != null && int.TryParse(tenantIdClaim.Value, out int tenantId))
            {
                return tenantId;
            }
            return null;
        }

        public async Task<bool> HasFundAccess(int fundId, string? requiredRole = null)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return false;

            var isSystemAdmin = _httpContextAccessor.HttpContext?.User?.FindFirst("IsSystemAdmin")?.Value == "true";
            if (isSystemAdmin)
                return true;

            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
                return false;

            // Check if fund belongs to user's tenant
            var fund = await _dbContext.Funds.FindAsync(fundId);
            if (fund == null || fund.TenantId != tenantId.Value)
                return false;

            // Check user has access to this fund
            var access = await _dbContext.UserFundAccesses
                .FirstOrDefaultAsync(ufa => 
                    ufa.UserId == userId && 
                    ufa.FundId == fundId &&
                    ufa.RevokedAt == null);

            if (access == null)
                return false;

            // Check role if specified
            if (requiredRole != null)
            {
                var roleHierarchy = new Dictionary<string, int>
                {
                    { FundRoles.Viewer, 1 },
                    { FundRoles.Editor, 2 },
                    { FundRoles.Manager, 3 }
                };

                var userRoleLevel = roleHierarchy.GetValueOrDefault(access.Role, 0);
                var requiredRoleLevel = roleHierarchy.GetValueOrDefault(requiredRole, 0);

                return userRoleLevel >= requiredRoleLevel;
            }

            return true;
        }

        public async Task<List<int>> GetUserFundIds()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return new List<int>();

            var isSystemAdmin = _httpContextAccessor.HttpContext?.User?.FindFirst("IsSystemAdmin")?.Value == "true";
            if (isSystemAdmin)
            {
                // System admin sees all funds
                return await _dbContext.Funds.Select(f => f.FundId).ToListAsync();
            }

            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
                return new List<int>();

            return await _dbContext.UserFundAccesses
                .Where(ufa => ufa.UserId == userId && ufa.RevokedAt == null)
                .Select(ufa => ufa.FundId)
                .ToListAsync();
        }

        public async Task<bool> ValidateFundBelongsToTenant(int fundId)
        {
            var tenantId = GetCurrentTenantId();
            if (!tenantId.HasValue)
                return false;

            var fund = await _dbContext.Funds.FindAsync(fundId);
            return fund != null && fund.TenantId == tenantId.Value;
        }
    }
}
