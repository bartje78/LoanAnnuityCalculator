using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanAnnuityCalculatorAPI.Models.DTOs;
using LoanAnnuityCalculatorAPI.Services;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for tariff calculator
    public class TariffCalculatorController : ControllerBase
    {
        private readonly TariffCalculatorService _tariffCalculatorService;
        private readonly ILogger<TariffCalculatorController> _logger;

        public TariffCalculatorController(
            TariffCalculatorService tariffCalculatorService,
            ILogger<TariffCalculatorController> logger)
        {
            _tariffCalculatorService = tariffCalculatorService;
            _logger = logger;
        }

        /// <summary>
        /// Calculate tariff based on loan parameters
        /// </summary>
        /// <param name="request">Tariff calculation request containing loan details</param>
        /// <returns>Complete tariff calculation including BSE and payment schedule</returns>
        [HttpPost("calculate")]
        public async Task<ActionResult<TariffCalculationResponse>> CalculateTariff([FromBody] TariffCalculationRequest request)
        {
            try
            {
                _logger.LogInformation("Calculating tariff for loan amount: {LoanAmount}, term: {LoanTerm} months",
                    request.LoanAmount, request.LoanTerm);

                // Validate request
                if (request.LoanAmount <= 0)
                    return BadRequest("Loan amount must be greater than zero.");

                if (request.LoanTerm <= 0)
                    return BadRequest("Loan term must be greater than zero.");

                // Allow CollateralValue = 0 for unsecured loans
                if (request.CollateralValue < 0)
                    return BadRequest("Collateral value cannot be negative.");

                if (request.InterestOnlyPeriod < 0)
                    return BadRequest("Interest-only period cannot be negative.");

                if (request.InterestOnlyPeriod > request.LoanTerm)
                    return BadRequest("Interest-only period cannot exceed loan term.");

                // For secured loans, validate effective collateral
                if (request.CollateralValue > 0)
                {
                    // Calculate effective collateral
                    decimal effectiveCollateral = request.CollateralValue - request.SubordinationAmount;
                    effectiveCollateral = effectiveCollateral * (1 - request.LiquidityHaircut / 100);

                    if (effectiveCollateral <= 0)
                        return BadRequest("Effective collateral must be greater than zero after subordination and haircut.");

                    if (request.LoanAmount > effectiveCollateral)
                    {
                        decimal ltv = request.LoanAmount / effectiveCollateral * 100;
                        _logger.LogWarning("High LTV detected: {LTV}%", ltv);
                    }
                }
                else
                {
                    // Unsecured loan
                    _logger.LogInformation("Processing unsecured loan (no collateral)");
                }

                var result = await _tariffCalculatorService.CalculateTariffAsync(request);

                _logger.LogInformation("Tariff calculation completed. Interest rate: {InterestRate}%, BSE: {BSE}",
                    result.InterestRate, result.BSE);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating tariff");
                return StatusCode(500, new { error = "An error occurred while calculating the tariff.", details = ex.Message });
            }
        }
    }
}
