using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace LoanAnnuityCalculatorAPI.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityType, int? entityId, object? changes = null, bool success = true, string? details = null);
        Task LogLoginAttemptAsync(string username, bool success, string? details = null);
        Task LogDataAccessAsync(string entityType, int entityId, string action = "Read");
    }

    public class AuditService : IAuditService
    {
        private readonly LoanDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            LoanDbContext context, 
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogAsync(string action, string entityType, int? entityId, object? changes = null, bool success = true, string? details = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return;

                var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
                var userName = httpContext.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
                var tenantId = httpContext.User?.FindFirst("TenantId")?.Value;

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    TenantId = tenantId != null ? int.Parse(tenantId) : null,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Changes = changes != null ? JsonSerializer.Serialize(changes) : null,
                    Details = details,
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                    RequestPath = httpContext.Request.Path,
                    HttpMethod = httpContext.Request.Method,
                    Success = success,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Don't fail the request if audit logging fails
                _logger.LogError(ex, "Failed to write audit log for action {Action}", action);
            }
        }

        public async Task LogLoginAttemptAsync(string username, bool success, string? details = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return;

                var auditLog = new AuditLog
                {
                    UserId = username,
                    UserName = username,
                    Action = success ? "Login" : "LoginFailed",
                    EntityType = "Authentication",
                    Details = details,
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                    RequestPath = httpContext.Request.Path,
                    HttpMethod = httpContext.Request.Method,
                    Success = success,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write login audit log for user {Username}", username);
            }
        }

        public async Task LogDataAccessAsync(string entityType, int entityId, string action = "Read")
        {
            // Log access to sensitive financial data
            await LogAsync(action, entityType, entityId);
        }
    }
}
