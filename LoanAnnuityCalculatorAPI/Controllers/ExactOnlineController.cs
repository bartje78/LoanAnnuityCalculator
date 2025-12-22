using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Security.Claims;
using LoanAnnuityCalculatorAPI.Services;
using Newtonsoft.Json.Linq;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/exact")]
    [Authorize]
    public class ExactOnlineController : ControllerBase
    {
        private readonly ExactOnlineService _exactService;

        public ExactOnlineController(ExactOnlineService exactService)
        {
            _exactService = exactService;
        }

        [HttpGet("auth-url")]
        public IActionResult GetAuthUrl()
        {
            var url = _exactService.GetAuthorizationUrl();
            return Ok(new { url });
        }

        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { error = "Authorization code is missing" });
            }

            try
            {
                // In a real scenario, validate the state parameter to prevent CSRF
                // For now, we'll extract tenantId from state or use the authenticated user's tenant
                var tenantId = state ?? User.FindFirst("TenantId")?.Value ?? "1";
                
                var token = await _exactService.ExchangeCodeForTokenAsync(code, tenantId);
                
                // Redirect back to the frontend with success
                var frontendUrl = "http://localhost:4200/settings/integrations?exact=success";
                return Redirect(frontendUrl);
            }
            catch (Exception ex)
            {
                // Redirect to frontend with error
                var frontendUrl = $"http://localhost:4200/settings/integrations?exact=error&message={Uri.EscapeDataString(ex.Message)}";
                return Redirect(frontendUrl);
            }
        }

        [HttpPost("token")]
        public async Task<IActionResult> ExchangeCode([FromBody] TokenRequest request)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(new { error = "Tenant ID not found" });
            }

            try
            {
                var token = await _exactService.ExchangeCodeForTokenAsync(request.Code, tenantId);
                return Ok(new { 
                    success = true, 
                    division = token.Division,
                    expiresAt = token.ExpiresAt 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("invoice")]
        public async Task<IActionResult> PostInvoice([FromBody] JObject invoiceData)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(new { error = "Tenant ID not found" });
            }

            try
            {
                var response = await _exactService.PostInvoiceAsync(tenantId, invoiceData);
                var content = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetConnectionStatus()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(new { error = "Tenant ID not found" });
            }

            try
            {
                var accessToken = await _exactService.GetValidAccessTokenAsync(tenantId);
                return Ok(new { connected = true, hasValidToken = !string.IsNullOrEmpty(accessToken) });
            }
            catch
            {
                return Ok(new { connected = false, hasValidToken = false });
            }
        }
    }

    public class TokenRequest
    {
        public string Code { get; set; } = string.Empty;
    }
}
