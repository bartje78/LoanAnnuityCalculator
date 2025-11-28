using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Services;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TenantController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;
        private readonly ITenantService _tenantService;
        private readonly ILogger<TenantController> _logger;

        public TenantController(
            LoanDbContext dbContext,
            ITenantService tenantService,
            ILogger<TenantController> logger)
        {
            _dbContext = dbContext;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>
        /// Get all tenants (System Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "SystemAdminOnly")]
        public async Task<ActionResult<List<Tenant>>> GetTenants()
        {
            var tenants = await _dbContext.Tenants
                .Include(t => t.Funds)
                .Include(t => t.Users)
                .ToListAsync();

            return Ok(tenants);
        }

        /// <summary>
        /// Get current user's tenant
        /// </summary>
        [HttpGet("current")]
        public async Task<ActionResult<Tenant>> GetCurrentTenant()
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            if (!tenantId.HasValue)
            {
                return NotFound(new { message = "No tenant assigned to user" });
            }

            var tenant = await _dbContext.Tenants
                .Include(t => t.Funds)
                .FirstOrDefaultAsync(t => t.TenantId == tenantId.Value);

            if (tenant == null)
            {
                return NotFound();
            }

            return Ok(tenant);
        }

        /// <summary>
        /// Create new tenant (System Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "SystemAdminOnly")]
        public async Task<ActionResult<Tenant>> CreateTenant([FromBody] CreateTenantRequest request)
        {
            // Create tenant
            var tenant = new Tenant
            {
                Name = request.Name,
                Description = request.Description,
                DatabaseKey = Guid.NewGuid().ToString(), // Unique encryption key
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Tenants.Add(tenant);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Tenant {TenantName} created with ID {TenantId}", tenant.Name, tenant.TenantId);

            // Create subscription if payment plan specified
            if (request.PaymentPlanId.HasValue)
            {
                var paymentPlan = await _dbContext.PaymentPlans.FindAsync(request.PaymentPlanId.Value);
                if (paymentPlan != null)
                {
                    var subscription = new TenantSubscription
                    {
                        TenantId = tenant.TenantId,
                        PaymentPlanId = request.PaymentPlanId.Value,
                        Status = SubscriptionStatus.Trial,
                        BillingPeriod = "Monthly",
                        StartDate = DateTime.UtcNow,
                        NextBillingDate = DateTime.UtcNow.AddMonths(1)
                    };
                    _dbContext.TenantSubscriptions.Add(subscription);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // Create admin user if specified
            if (request.AdminUser != null)
            {
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
                var adminUser = new ApplicationUser
                {
                    UserName = request.AdminUser.Email,
                    Email = request.AdminUser.Email,
                    EmailConfirmed = true,
                    FirstName = request.AdminUser.FirstName,
                    LastName = request.AdminUser.LastName,
                    TenantId = tenant.TenantId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                adminUser.PasswordHash = hasher.HashPassword(adminUser, request.AdminUser.Password);

                _dbContext.Users.Add(adminUser);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Admin user {Email} created for tenant {TenantId}", request.AdminUser.Email, tenant.TenantId);
            }

            // Return tenant without navigation properties to avoid circular reference
            var result = new
            {
                tenant.TenantId,
                tenant.Name,
                tenant.Description,
                tenant.DatabaseKey,
                tenant.IsActive,
                tenant.CreatedAt
            };

            return CreatedAtAction(nameof(GetCurrentTenant), new { id = tenant.TenantId }, result);
        }

        /// <summary>
        /// Update tenant (System Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "SystemAdminOnly")]
        public async Task<ActionResult<Tenant>> UpdateTenant(int id, [FromBody] UpdateTenantRequest request)
        {
            var tenant = await _dbContext.Tenants
                .Include(t => t.Subscription)
                .FirstOrDefaultAsync(t => t.TenantId == id);
            
            if (tenant == null)
            {
                return NotFound();
            }

            tenant.Name = request.Name;
            tenant.Description = request.Description;
            tenant.IsActive = request.IsActive;

            if (!request.IsActive && !tenant.DeactivatedAt.HasValue)
            {
                tenant.DeactivatedAt = DateTime.UtcNow;
            }

            // Update subscription payment plan if provided and changed
            if (request.PaymentPlanId.HasValue && tenant.Subscription != null)
            {
                if (tenant.Subscription.PaymentPlanId != request.PaymentPlanId.Value)
                {
                    tenant.Subscription.PaymentPlanId = request.PaymentPlanId.Value;
                    _logger.LogInformation("Tenant {TenantId} payment plan updated to {PaymentPlanId}", id, request.PaymentPlanId.Value);
                }
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Tenant {TenantId} updated", id);

            return Ok(tenant);
        }

        /// <summary>
        /// Deactivate tenant (System Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "SystemAdminOnly")]
        public async Task<ActionResult> DeactivateTenant(int id)
        {
            var tenant = await _dbContext.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            tenant.IsActive = false;
            tenant.DeactivatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogWarning("Tenant {TenantId} deactivated", id);

            return NoContent();
        }
    }

    public class CreateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ContactEmail { get; set; }
        public int? PaymentPlanId { get; set; }
        public bool IsActive { get; set; } = true;
        public AdminUserRequest? AdminUser { get; set; }
    }

    public class AdminUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateTenantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ContactEmail { get; set; }
        public int? PaymentPlanId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
