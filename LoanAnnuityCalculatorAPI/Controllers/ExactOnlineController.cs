using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using LoanAnnuityCalculatorAPI.Services;
using Newtonsoft.Json.Linq;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/exact")]
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

        [HttpPost("token")]
        public async Task<IActionResult> ExchangeCode([FromBody] string code)
        {
            var token = await _exactService.ExchangeCodeForTokenAsync(code);
            return Ok(new { access_token = token });
        }

        [HttpPost("invoice")]
        public async Task<IActionResult> PostInvoice([FromHeader] string accessToken, [FromBody] JObject invoiceData)
        {
            var response = await _exactService.PostInvoiceAsync(accessToken, invoiceData);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
    }
}
