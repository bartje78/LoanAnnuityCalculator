using System.Security.Claims;

namespace LoanAnnuityCalculatorAPI.Middleware
{
    /// <summary>
    /// Middleware to extract and validate tenant context from JWT claims
    /// Ensures every request has proper tenant isolation
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip tenant validation for auth endpoints
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/api/auth/login") || path.Contains("/api/auth/register") || path.Contains("/swagger"))
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var tenantIdClaim = context.User.FindFirst("TenantId");
                var isSystemAdmin = context.User.FindFirst("IsSystemAdmin")?.Value == "true";

                if (tenantIdClaim == null && !isSystemAdmin)
                {
                    _logger.LogWarning("Authenticated user {User} has no TenantId claim and is not system admin", 
                        context.User.Identity.Name);
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "No tenant access" });
                    return;
                }

                if (tenantIdClaim != null)
                {
                    // Store tenant ID in HttpContext items for easy access
                    if (int.TryParse(tenantIdClaim.Value, out int tenantId))
                    {
                        context.Items["TenantId"] = tenantId;
                        _logger.LogDebug("Request for tenant {TenantId} by user {User}", 
                            tenantId, context.User.Identity.Name);
                    }
                }
            }

            await _next(context);
        }
    }

    public static class TenantMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantMiddleware>();
        }
    }
}
