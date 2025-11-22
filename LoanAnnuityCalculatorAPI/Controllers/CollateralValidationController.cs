using Microsoft.AspNetCore.Mvc;
using LoanAnnuityCalculatorAPI.Services;
using LoanAnnuityCalculatorAPI.Models;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/collateral")]
    public class CollateralValidationController : ControllerBase
    {
        private readonly CollateralValidationService _collateralValidationService;

        public CollateralValidationController(CollateralValidationService collateralValidationService)
        {
            _collateralValidationService = collateralValidationService;
        }

        /// <summary>
        /// Test endpoint to demonstrate collateral validation functionality
        /// </summary>
        [HttpPost("test-validation")]
        public async Task<IActionResult> TestCollateralValidation([FromBody] TestCollateralRequest request)
        {
            try
            {
                var collateral = new Collateral
                {
                    CollateralType = request.CollateralType,
                    Description = request.Description,
                    AppraisalValue = request.AppraisalValue,
                    AppraisalDate = request.AppraisalDate,
                    PropertyType = request.PropertyType,
                    SecurityType = request.SecurityType,
                    FirstMortgageAmount = request.FirstMortgageAmount,
                    PropertyAddress = request.PropertyAddress,
                    LandRegistryCode = request.LandRegistryCode,
                    PostalCode = request.PostalCode,
                    HouseNumber = request.HouseNumber,
                    AssetUniqueId = request.AssetUniqueId
                };

                var result = await _collateralValidationService.CreateOrLinkCollateralAsync(request.LoanId, collateral);

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    collateralId = result.CollateralId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during validation",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all loans that are secured by the same collateral
        /// </summary>
        [HttpGet("{collateralId}/loans")]
        public async Task<IActionResult> GetLoansSecuredByCollateral(int collateralId)
        {
            try
            {
                var loans = await _collateralValidationService.GetLoansSecuredByCollateralAsync(collateralId);
                
                return Ok(new
                {
                    collateralId = collateralId,
                    loanCount = loans.Count,
                    loans = loans.Select(l => new
                    {
                        loanId = l.LoanID,
                        debtorName = l.DebtorDetails?.DebtorName,
                        loanAmount = l.LoanAmount,
                        status = l.Status,
                        startDate = l.StartDate
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving loans",
                    error = ex.Message
                });
            }
        }
    }

    public class TestCollateralRequest
    {
        public int LoanId { get; set; }
        public string CollateralType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? AppraisalValue { get; set; }
        public DateTime? AppraisalDate { get; set; }
        public string? PropertyType { get; set; }
        public string? SecurityType { get; set; }
        public decimal? FirstMortgageAmount { get; set; }
        public string? PropertyAddress { get; set; }
        
        // Unique identifiers - use either LandRegistryCode OR PostalCode+HouseNumber OR AssetUniqueId
        public string? LandRegistryCode { get; set; }  // For plots of land (kadastrale aanduiding)
        public string? PostalCode { get; set; }        // Dutch postal code (e.g., "1234AB")
        public string? HouseNumber { get; set; }       // House number with additions (e.g., "123A", "123-1")
        public string? AssetUniqueId { get; set; }     // For non-real estate assets (VIN, serial number, etc.)
    }
}