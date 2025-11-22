using Microsoft.AspNetCore.Mvc;
using LoanAnnuityCalculatorAPI.Services;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EcbController : ControllerBase
    {
        private readonly EcbApiService _ecbService;
        private readonly ILogger<EcbController> _logger;

        public EcbController(EcbApiService ecbService, ILogger<EcbController> logger)
        {
            _ecbService = ecbService;
            _logger = logger;
        }

        /// <summary>
        /// Get the latest yield curve for AAA-rated euro area government bonds
        /// Used for loan pricing (tariff base rate)
        /// </summary>
        [HttpGet("yield-curve")]
        public async Task<ActionResult<YieldCurveResponse>> GetYieldCurve()
        {
            try
            {
                var result = await _ecbService.GetYieldCurveAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving yield curve");
                return StatusCode(500, new { message = "Error retrieving yield curve from ECB", error = ex.Message });
            }
        }

        /// <summary>
        /// Get the BSE reference rate based on 1-year EURIBOR
        /// Calculates average of September, October, November from previous year
        /// This is the discount rate used for BSE (Bruto Steun Equivalent) calculations
        /// </summary>
        [HttpGet("bse-reference-rate")]
        public async Task<ActionResult<BseReferenceRateResponse>> GetBseReferenceRate()
        {
            try
            {
                var result = await _ecbService.GetBseReferenceRateAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving BSE reference rate");
                return StatusCode(500, new { message = "Error retrieving BSE reference rate from ECB", error = ex.Message });
            }
        }
    }
}
