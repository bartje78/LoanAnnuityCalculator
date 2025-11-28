using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models.Loan;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using LoanAnnuityCalculatorAPI.Models.Debtor;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/loan")]
    [Authorize] // Require authentication for all loan endpoints
    public class LoanController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;
        private readonly AnnuityCalculator _annuityCalculatorService;
        private readonly PaymentCalculatorService _paymentCalculatorService;
        private readonly LoanService _loanService;
        private readonly LoanDateHelper _loanDateHelper;
        private readonly CollateralValidationService _collateralValidationService;
        private readonly IStatusCalculationService _statusCalculationService;
        private readonly FractionalPaymentCalculator _fractionalPaymentCalculator;

        public LoanController(
            LoanDbContext dbContext,
            AnnuityCalculator annuityCalculatorService,
            PaymentCalculatorService paymentCalculatorService,
            LoanService loanService,
            LoanDateHelper loanDateHelper,
            CollateralValidationService collateralValidationService,
            IStatusCalculationService statusCalculationService,
            FractionalPaymentCalculator fractionalPaymentCalculator)
        {
            _dbContext = dbContext;
            _annuityCalculatorService = annuityCalculatorService;
            _paymentCalculatorService = paymentCalculatorService;
            _loanService = loanService;
            _loanDateHelper = loanDateHelper;
            _collateralValidationService = collateralValidationService;
            _statusCalculationService = statusCalculationService;
            _fractionalPaymentCalculator = fractionalPaymentCalculator;
        }

[HttpGet]
public async Task<IActionResult> GetLoansByDebtorId([FromQuery] int debtorId)
{
    try
    {
        // Fetch loans for the specified DebtorID
        var loans = await _dbContext.Loans
            .Where(l => l.DebtorID == debtorId)
            .Include(l => l.DebtorDetails) // Include debtor details
            .Include(l => l.LoanCollaterals)
                .ThenInclude(lc => lc.Collateral) // Include collateral details through junction table
            .ToListAsync();

        if (!loans.Any())
        {
            return Ok(new List<Loan>());
        }

        // Map loans to a clean response format
        var cleanLoans = new List<object>();
        
        foreach (var loan in loans)
        {
            // Calculate status using the configurable service
            loan.Status = await _statusCalculationService.CalculateStatusAsync(loan);

            cleanLoans.Add(new
            {
                LoanID = loan.LoanID,
                DebtorName = loan.DebtorDetails?.DebtorName ?? "N/A",
                ContactPerson = loan.DebtorDetails?.ContactPerson ?? "",
                Address = loan.DebtorDetails?.Address ?? "",
                Email = loan.DebtorDetails?.Email ?? "",
                LoanAmount = loan.LoanAmount,
                OutstandingAmount = loan.OutstandingAmount,
                AnnualInterestRate = loan.AnnualInterestRate,
                TenorMonths = loan.TenorMonths,
                InterestOnlyMonths = loan.InterestOnlyMonths,
                StartDate = loan.StartDate,
                Status = loan.Status,
                RedemptionSchedule = loan.RedemptionSchedule,
                CreditLimit = loan.CreditLimit,
                AmountDrawn = loan.AmountDrawn,
                Collaterals = loan.LoanCollaterals?.Select(lc => new
                {
                    CollateralId = lc.Collateral!.CollateralId,
                    CollateralType = lc.Collateral.CollateralType,
                    Description = lc.Collateral.Description,
                    AppraisalValue = lc.Collateral.AppraisalValue,
                    AppraisalDate = lc.Collateral.AppraisalDate,
                    PropertyType = lc.Collateral.PropertyType,
                    // Security type fields
                    SecurityType = lc.Collateral.SecurityType,
                    FirstMortgageAmount = lc.Collateral.FirstMortgageAmount,
                    LiquidityHaircut = lc.Collateral.LiquidityHaircut,
                    // Unique identifier fields
                    PropertyAddress = lc.Collateral.PropertyAddress,
                    LandRegistryCode = lc.Collateral.LandRegistryCode,
                    PostalCode = lc.Collateral.PostalCode,
                    HouseNumber = lc.Collateral.HouseNumber,
                    AssetUniqueId = lc.Collateral.AssetUniqueId,
                    // Junction table specific fields
                    Priority = lc.Priority,
                    AllocationPercentage = lc.AllocationPercentage,
                    AssignedDate = lc.AssignedDate
                }) ?? Enumerable.Empty<object>()
            });
        }

        return Ok(cleanLoans);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "An error occurred while retrieving loans.", error = ex.Message });
    }
}
[HttpPost("uploadLoans")]
        public async Task<IActionResult> UploadLoans(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded or file is empty." });
            }
        
            var loans = new List<Loan>();
        
            try
            {
                // Use EPPlus to process the Excel file
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0]; // Get the first worksheet
                        int rowCount = worksheet.Dimension.Rows;
        
                        for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip headers
                        {
                            // Extract debtor details
                            var debtorName = worksheet.Cells[row, 1].Text.Trim(); // DebtorName is now in column 1
                            if (string.IsNullOrEmpty(debtorName))
                            {
                                return BadRequest(new { message = $"Debtor name is missing in row {row}." });
                            }
        
                            // Check if the debtor already exists
                            var debtor = await _dbContext.DebtorDetails
                                .FirstOrDefaultAsync(d => d.DebtorName == debtorName);
        
                            int debtorId;
                            if (debtor == null)
                            {
                                // Create a new debtor entry
                                debtor = new DebtorDetails
                                {
                                    DebtorName = debtorName
                                };
        
                                _dbContext.DebtorDetails.Add(debtor);
                                await _dbContext.SaveChangesAsync(); // Save to generate the DebtorID
                                debtorId = debtor.DebtorID;
                            }
                            else
                            {
                                debtorId = debtor.DebtorID;
                            }
        
                            // Extract loan details
                            var loan = new Loan
                            {
                                LoanAmount = decimal.Parse(worksheet.Cells[row, 2].Text), // Shifted to column 2
                                AnnualInterestRate = decimal.Parse(worksheet.Cells[row, 3].Text), // Shifted to column 3
                                TenorMonths = int.Parse(worksheet.Cells[row, 4].Text), // Shifted to column 4
                                InterestOnlyMonths = int.Parse(worksheet.Cells[row, 5].Text), // Shifted to column 5
                                StartDate = DateTime.Parse(worksheet.Cells[row, 6].Text), // Shifted to column 6
                                DebtorID = debtorId // Associate the loan with the debtor
                            };
        
                            loans.Add(loan);
                        }
                    }
                }
        
                // Add loans to the database
                await _dbContext.Loans.AddRangeAsync(loans);
                await _dbContext.SaveChangesAsync();
        
                return Ok(new { message = "Loans and debtor details uploaded successfully.", count = loans.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing the file.", error = ex.Message });
            }
        }

        [HttpGet("calculateMonthsDifference")]
        public IActionResult CalculateMonthsDifference(int loanId)
        {
            var loan = _loanService.GetLoanById(loanId);
            if (loan == null)
            {
                return NotFound(new { message = "Loan not found." });
            }

            var monthsDifference = _loanDateHelper.CalculateMonthsDifference(loan.StartDate);

            if (monthsDifference < 0)
            {
                monthsDifference = 0;
            }

            return Ok(new { monthsDifference });
        }
[HttpGet("calculateAnnuityDetails")]
    public IActionResult CalculateAnnuityDetails(int loanId, int period)
    {
        // Fetch the loan details by loanId
        var loan = _loanService.GetLoanById(loanId);
        if (loan == null)
        {
            return NotFound(new { message = "Loan not found." });
        }

        if (period < 0 || period >= loan.TenorMonths)
        {
            return BadRequest(new { message = "Invalid period. It must be between 0 and the loan tenor minus 1." });
        }

        // Calculate the target month (next payment after the current period)
        int targetMonth = period + 1;

        try
        {
            // For BuildingDepot loans, use AmountDrawn instead of LoanAmount
            decimal calculationAmount = loan.LoanAmount;
            if (loan.RedemptionSchedule == "BuildingDepot")
            {
                if (!loan.AmountDrawn.HasValue || loan.AmountDrawn.Value <= 0)
                {
                    return BadRequest(new { message = "Building Depot loans require a valid Amount Drawn value." });
                }
                calculationAmount = loan.AmountDrawn.Value;
            }

            var (interestComponent, capitalComponent, remainingLoan) = _paymentCalculatorService.CalculateForSpecificMonth(
                calculationAmount,
                loan.AnnualInterestRate,
                loan.TenorMonths,
                targetMonth,
                loan.InterestOnlyMonths,
                loan.RedemptionSchedule
            );

            return Ok(new
            {
                Month = targetMonth,
                InterestComponent = interestComponent,
                CapitalComponent = capitalComponent,
                RemainingLoan = remainingLoan,
                RedemptionSchedule = loan.RedemptionSchedule.ToString()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            try
            {
                var loans = await _dbContext.Loans
                    .Include(l => l.DebtorDetails)
                    .Include(l => l.LoanCollaterals)
                        .ThenInclude(lc => lc.Collateral)
                    .ToListAsync();
        
                var cleanLoans = new List<object>();
                
                foreach (var loan in loans)
                {
                    // Calculate status using the configurable service
                    loan.Status = await _statusCalculationService.CalculateStatusAsync(loan);
        
                    cleanLoans.Add(new
                    {
                        LoanID = loan.LoanID,
                        DebtorName = loan.DebtorDetails?.DebtorName ?? "N/A",
                        ContactPerson = loan.DebtorDetails?.ContactPerson ?? "",
                        Address = loan.DebtorDetails?.Address ?? "",
                        Email = loan.DebtorDetails?.Email ?? "",
                        LoanAmount = loan.LoanAmount,
                        OutstandingAmount = loan.OutstandingAmount,
                        AnnualInterestRate = loan.AnnualInterestRate,
                        TenorMonths = loan.TenorMonths,
                        InterestOnlyMonths = loan.InterestOnlyMonths,
                        StartDate = loan.StartDate,
                        Status = loan.Status,
                        RedemptionSchedule = loan.RedemptionSchedule,
                        CreditLimit = loan.CreditLimit,
                        AmountDrawn = loan.AmountDrawn,
                        Collaterals = loan.LoanCollaterals?.Select(lc => new
                        {
                            CollateralId = lc.Collateral!.CollateralId,
                            CollateralType = lc.Collateral.CollateralType,
                            Description = lc.Collateral.Description,
                            AppraisalValue = lc.Collateral.AppraisalValue,
                            AppraisalDate = lc.Collateral.AppraisalDate,
                            PropertyType = lc.Collateral.PropertyType,
                            // Security type fields
                            SecurityType = lc.Collateral.SecurityType,
                            FirstMortgageAmount = lc.Collateral.FirstMortgageAmount,
                            LiquidityHaircut = lc.Collateral.LiquidityHaircut,
                            // Unique identifier fields
                            PropertyAddress = lc.Collateral.PropertyAddress,
                            LandRegistryCode = lc.Collateral.LandRegistryCode,
                            PostalCode = lc.Collateral.PostalCode,
                            HouseNumber = lc.Collateral.HouseNumber,
                            AssetUniqueId = lc.Collateral.AssetUniqueId,
                            // Junction table specific fields
                            Priority = lc.Priority,
                            AllocationPercentage = lc.AllocationPercentage,
                            AssignedDate = lc.AssignedDate
                        }) ?? Enumerable.Empty<object>()
                    });
                }
        
                return Ok(cleanLoans);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving loans.", Error = ex.Message });
            }
        }

        

        [HttpGet("calculateAnnuityDetailsForEntireTenor")]
        public IActionResult CalculateAnnuityDetailsForEntireTenor(int loanId)
        {
            var loan = _loanService.GetLoanById(loanId);
            if (loan == null)
            {
                return NotFound("Loan not found.");
            }

            if (loan.TenorMonths <= 0)
            {
                return BadRequest("Tenor months must be greater than zero.");
            }

            try
            {
                // For BuildingDepot loans, use AmountDrawn instead of LoanAmount
                decimal calculationAmount = loan.LoanAmount;
                if (loan.RedemptionSchedule == "BuildingDepot")
                {
                    if (!loan.AmountDrawn.HasValue || loan.AmountDrawn.Value <= 0)
                    {
                        return BadRequest("Building Depot loans require a valid Amount Drawn value.");
                    }
                    calculationAmount = loan.AmountDrawn.Value;
                }

                var calculations = _paymentCalculatorService.CalculateForEntireTenor(
                    calculationAmount,
                    loan.AnnualInterestRate,
                    loan.TenorMonths,
                    loan.InterestOnlyMonths,
                    loan.RedemptionSchedule
                );

                var results = calculations.Select(c => new AnnuityDetail
                {
                    Month = c.month,
                    InterestComponent = c.interestComponent,
                    CapitalComponent = c.capitalComponent,
                    RemainingLoan = c.remainingLoan
                }).ToList();

                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/loan
        [HttpPost]
        public async Task<IActionResult> CreateLoan([FromBody] Loan newLoan)
        {
            try
            {
                if (newLoan == null)
                {
                    return BadRequest(new { Message = "Loan data is required." });
                }

                // Validate required fields
                if (newLoan.DebtorID <= 0)
                {
                    return BadRequest(new { Message = "Valid DebtorID is required." });
                }

                if (newLoan.LoanAmount <= 0)
                {
                    return BadRequest(new { Message = "Loan amount must be greater than 0." });
                }

                // Verify that the debtor exists
                var debtorExists = await _dbContext.DebtorDetails
                    .AnyAsync(d => d.DebtorID == newLoan.DebtorID);

                if (!debtorExists)
                {
                    return BadRequest(new { Message = "Debtor not found." });
                }

                // Reset LoanID to 0 so Entity Framework will generate a new one
                newLoan.LoanID = 0;

                // Set default status if not provided
                if (string.IsNullOrEmpty(newLoan.Status))
                {
                    newLoan.Status = "Active";
                }

                _dbContext.Loans.Add(newLoan);
                await _dbContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetLoansByDebtorId), new { debtorId = newLoan.DebtorID }, new { 
                    Message = "Loan created successfully.", 
                    Loan = new {
                        LoanID = newLoan.LoanID,
                        DebtorID = newLoan.DebtorID,
                        LoanAmount = newLoan.LoanAmount,
                        OutstandingAmount = newLoan.OutstandingAmount,
                        AnnualInterestRate = newLoan.AnnualInterestRate,
                        TenorMonths = newLoan.TenorMonths,
                        InterestOnlyMonths = newLoan.InterestOnlyMonths,
                        StartDate = newLoan.StartDate,
                        Status = newLoan.Status,
                        RedemptionSchedule = newLoan.RedemptionSchedule,
                        CreditLimit = newLoan.CreditLimit,
                        AmountDrawn = newLoan.AmountDrawn
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while creating the loan.", Error = ex.Message });
            }
        }

        // PUT: api/loan/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLoan(int id, [FromBody] Loan updatedLoan)
        {
            try
            {
                // Log what we received
                Console.WriteLine($"=== UpdateLoan called for ID: {id} ===");
                Console.WriteLine($"Received RedemptionSchedule: {updatedLoan.RedemptionSchedule} (type: {updatedLoan.RedemptionSchedule.GetType().Name})");
                Console.WriteLine($"LoanAmount: {updatedLoan.LoanAmount}");
                Console.WriteLine($"Status: {updatedLoan.Status}");
                
                if (id != updatedLoan.LoanID)
                {
                    return BadRequest(new { Message = "Loan ID mismatch." });
                }

                var existingLoan = await _dbContext.Loans
                    .FirstOrDefaultAsync(l => l.LoanID == id);

                if (existingLoan == null)
                {
                    return NotFound(new { Message = "Loan not found." });
                }

                // Update the loan properties
                existingLoan.LoanAmount = updatedLoan.LoanAmount;
                existingLoan.OutstandingAmount = updatedLoan.OutstandingAmount; // Allow manual correction for import fixes
                existingLoan.AnnualInterestRate = updatedLoan.AnnualInterestRate;
                existingLoan.TenorMonths = updatedLoan.TenorMonths;
                existingLoan.InterestOnlyMonths = updatedLoan.InterestOnlyMonths;
                existingLoan.StartDate = updatedLoan.StartDate;
                existingLoan.Status = updatedLoan.Status;
                existingLoan.RedemptionSchedule = updatedLoan.RedemptionSchedule;
                existingLoan.CreditLimit = updatedLoan.CreditLimit;
                existingLoan.AmountDrawn = updatedLoan.AmountDrawn;

                await _dbContext.SaveChangesAsync();

                // Return a clean DTO instead of the entity
                return Ok(new { 
                    Message = "Loan updated successfully.", 
                    Loan = new {
                        LoanID = existingLoan.LoanID,
                        DebtorID = existingLoan.DebtorID,
                        LoanAmount = existingLoan.LoanAmount,
                        OutstandingAmount = existingLoan.OutstandingAmount,
                        AnnualInterestRate = existingLoan.AnnualInterestRate,
                        TenorMonths = existingLoan.TenorMonths,
                        InterestOnlyMonths = existingLoan.InterestOnlyMonths,
                        StartDate = existingLoan.StartDate,
                        Status = existingLoan.Status,
                        RedemptionSchedule = existingLoan.RedemptionSchedule,
                        CreditLimit = existingLoan.CreditLimit,
                        AmountDrawn = existingLoan.AmountDrawn
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while updating the loan.", Error = ex.Message });
            }
        }

        // DELETE: api/loan/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLoan(int id)
        {
            try
            {
                var loan = await _dbContext.Loans
                    .FirstOrDefaultAsync(l => l.LoanID == id);

                if (loan == null)
                {
                    return NotFound(new { Message = "Loan not found." });
                }

                _dbContext.Loans.Remove(loan);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = "Loan deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting the loan.", Error = ex.Message });
            }
        }

        // Collateral Management Endpoints

        [HttpPost("{loanId}/collateral")]
        public async Task<IActionResult> AddCollateral(int loanId, [FromBody] Collateral collateral)
        {
            try
            {
                var result = await _collateralValidationService.CreateOrLinkCollateralAsync(loanId, collateral);
                
                if (result.Success)
                {
                    return Ok(new { 
                        message = result.Message, 
                        collateralId = result.CollateralId,
                        success = true
                    });
                }
                else
                {
                    return BadRequest(new { 
                        message = result.Message, 
                        success = false 
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "An error occurred while processing collateral.", 
                    error = ex.Message,
                    success = false
                });
            }
        }

        [HttpPut("collateral/{collateralId}")]
        public async Task<IActionResult> UpdateCollateral(int collateralId, [FromBody] Collateral updatedCollateral)
        {
            try
            {
                var existingCollateral = await _dbContext.Collaterals.FindAsync(collateralId);
                if (existingCollateral == null)
                {
                    return NotFound(new { message = "Collateral not found." });
                }

                // Clean up empty strings before updating
                CleanCollateralStrings(updatedCollateral);

                // Update the collateral properties
                existingCollateral.CollateralType = updatedCollateral.CollateralType;
                existingCollateral.Description = updatedCollateral.Description;
                existingCollateral.AppraisalValue = updatedCollateral.AppraisalValue;
                existingCollateral.AppraisalDate = updatedCollateral.AppraisalDate;
                existingCollateral.PropertyType = updatedCollateral.PropertyType;
                // Update security type fields
                existingCollateral.SecurityType = updatedCollateral.SecurityType;
                existingCollateral.FirstMortgageAmount = updatedCollateral.FirstMortgageAmount;
                existingCollateral.LiquidityHaircut = updatedCollateral.LiquidityHaircut;
                // Update unique identifier fields
                existingCollateral.PropertyAddress = updatedCollateral.PropertyAddress;
                existingCollateral.LandRegistryCode = updatedCollateral.LandRegistryCode;
                existingCollateral.PostalCode = updatedCollateral.PostalCode;
                existingCollateral.HouseNumber = updatedCollateral.HouseNumber;
                existingCollateral.AssetUniqueId = updatedCollateral.AssetUniqueId;
                existingCollateral.LastUpdatedDate = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Collateral updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating collateral.", error = ex.Message });
            }
        }

        [HttpDelete("collateral/{collateralId}")]
        public async Task<IActionResult> DeleteCollateral(int collateralId)
        {
            try
            {
                var collateral = await _dbContext.Collaterals.FindAsync(collateralId);
                if (collateral == null)
                {
                    return NotFound(new { message = "Collateral not found." });
                }

                // Remove any LoanCollateral relationships first
                var loanCollaterals = await _dbContext.LoanCollaterals
                    .Where(lc => lc.CollateralId == collateralId)
                    .ToListAsync();
                
                _dbContext.LoanCollaterals.RemoveRange(loanCollaterals);
                _dbContext.Collaterals.Remove(collateral);

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Collateral deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting collateral.", error = ex.Message });
            }
        }

        [HttpGet("collateral/shared-assets")]
        public async Task<IActionResult> GetSharedAssets()
        {
            try
            {
                // Find collaterals that are used by multiple loans (shared assets)
                var sharedAssets = await _dbContext.Collaterals
                    .Include(c => c.LoanCollaterals)
                        .ThenInclude(lc => lc.Loan)
                            .ThenInclude(l => l!.DebtorDetails)
                    .Where(c => c.LoanCollaterals.Count > 1)
                    .ToListAsync();

                var result = sharedAssets.Select(c => new
                {
                    CollateralId = c.CollateralId,
                    TotalLoans = c.LoanCollaterals.Count,
                    TotalValue = c.AppraisalValue,
                    AssetIdentifier = new
                    {
                        c.PropertyAddress,
                        c.LandRegistryCode,
                        c.PostalCode,
                        c.HouseNumber,
                        c.AssetUniqueId
                    },
                    LoanDetails = c.LoanCollaterals.Select(lc => new
                    {
                        LoanId = lc.LoanId,
                        LoanAmount = lc.Loan?.LoanAmount ?? 0,
                        DebtorName = lc.Loan?.DebtorDetails?.DebtorName ?? "Unknown",
                        Priority = lc.Priority,
                        AllocationPercentage = lc.AllocationPercentage,
                        AssignedDate = lc.AssignedDate
                    }).ToList(),
                    CollateralDetails = new
                    {
                        c.CollateralType,
                        c.Description,
                        c.AppraisalValue,
                        c.AppraisalDate,
                        c.PropertyType,
                        c.SecurityType,
                        c.FirstMortgageAmount
                    }
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving shared assets.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all collateral for a specific debtor
        /// </summary>
        [HttpGet("collateral/by-debtor/{debtorId}")]
        public async Task<IActionResult> GetCollateralByDebtor(int debtorId)
        {
            try
            {
                // Find all loans for this debtor
                var loans = await _dbContext.Loans
                    .Where(l => l.DebtorID == debtorId)
                    .Select(l => l.LoanID)
                    .ToListAsync();

                if (!loans.Any())
                {
                    return Ok(new List<object>()); // Return empty list if no loans found
                }

                // Find all collaterals linked to these loans
                var collaterals = await _dbContext.Collaterals
                    .Include(c => c.LoanCollaterals)
                    .Where(c => c.LoanCollaterals.Any(lc => loans.Contains(lc.LoanId)))
                    .Select(c => new
                    {
                        CollateralID = c.CollateralId,
                        c.CollateralType,
                        c.Description,
                        c.AppraisalValue,
                        c.AppraisalDate,
                        c.PropertyType,
                        c.SecurityType,
                        c.FirstMortgageAmount,
                        c.LiquidityHaircut,
                        c.PropertyAddress,
                        c.LandRegistryCode,
                        c.PostalCode,
                        c.HouseNumber,
                        c.AssetUniqueId,
                        MarketValue = c.AppraisalValue,
                        SubordinationAmount = c.FirstMortgageAmount ?? 0
                    })
                    .Distinct()
                    .ToListAsync();

                return Ok(collaterals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving collateral.", error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate fractional payment amounts based on invoice day setting
        /// </summary>
        [HttpGet("calculateFractionalPayment")]
        public async Task<IActionResult> CalculateFractionalPayment(int loanId, int invoiceDay)
        {
            var loan = _loanService.GetLoanById(loanId);
            if (loan == null)
            {
                return NotFound(new { message = "Loan not found." });
            }

            if (invoiceDay < 1 || invoiceDay > 31)
            {
                return BadRequest(new { message = "Invoice day must be between 1 and 31." });
            }

            try
            {
                // For BuildingDepot loans, use AmountDrawn instead of LoanAmount
                decimal calculationAmount = loan.LoanAmount;
                if (loan.RedemptionSchedule == "BuildingDepot")
                {
                    if (!loan.AmountDrawn.HasValue || loan.AmountDrawn.Value <= 0)
                    {
                        return BadRequest(new { message = "Building Depot loans require a valid Amount Drawn value." });
                    }
                    calculationAmount = loan.AmountDrawn.Value;
                }

                // Calculate the period difference including fractional months
                decimal periodDifference = _fractionalPaymentCalculator.CalculatePeriodDifference(loan.StartDate, invoiceDay);
                
                // Calculate the next invoice date
                DateTime nextInvoiceDate = _fractionalPaymentCalculator.CalculateNextInvoiceDate(loan.StartDate, invoiceDay);

                // Check if loan is completed
                if (periodDifference >= loan.TenorMonths)
                {
                    return Ok(new
                    {
                        LoanId = loanId,
                        PeriodDifference = periodDifference,
                        NextInvoiceDate = nextInvoiceDate.ToString("yyyy-MM-dd"),
                        InterestComponent = 0,
                        CapitalComponent = 0,
                        IsLoanCompleted = true,
                        Message = "Loan tenor completed"
                    });
                }

                // Calculate fractional payment using the correct amount
                var paymentResult = _fractionalPaymentCalculator.CalculateFractionalPayment(
                    calculationAmount,
                    loan.AnnualInterestRate,
                    loan.TenorMonths,
                    loan.InterestOnlyMonths,
                    periodDifference,
                    invoiceDay,
                    loan.StartDate
                );

                return Ok(new
                {
                    LoanId = loanId,
                    PeriodDifference = periodDifference,
                    NextInvoiceDate = nextInvoiceDate.ToString("yyyy-MM-dd"),
                    InterestComponent = Math.Round(paymentResult.InterestComponent, 2),
                    CapitalComponent = Math.Round(paymentResult.CapitalComponent, 2),
                    IsInterestOnlyPeriod = paymentResult.IsInterestOnlyPeriod,
                    FractionalDays = paymentResult.FractionalDays,
                    TotalDaysInPeriod = paymentResult.TotalDaysInPeriod,
                    IsLoanCompleted = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error calculating fractional payment", error = ex.Message });
            }
        }

        /// <summary>
        /// Get the next invoice date for a loan based on invoice day setting
        /// </summary>
        [HttpGet("{loanId}/nextInvoiceDate")]
        public IActionResult GetNextInvoiceDate(int loanId, int invoiceDay)
        {
            var loan = _loanService.GetLoanById(loanId);
            if (loan == null)
            {
                return NotFound(new { message = "Loan not found." });
            }

            if (invoiceDay < 1 || invoiceDay > 31)
            {
                return BadRequest(new { message = "Invoice day must be between 1 and 31." });
            }

            try
            {
                DateTime nextInvoiceDate = _fractionalPaymentCalculator.CalculateNextInvoiceDate(loan.StartDate, invoiceDay);
                decimal periodDifference = _fractionalPaymentCalculator.CalculatePeriodDifference(loan.StartDate, invoiceDay);

                bool isLoanCompleted = periodDifference >= loan.TenorMonths;

                return Ok(new
                {
                    LoanId = loanId,
                    NextInvoiceDate = nextInvoiceDate.ToString("yyyy-MM-dd"),
                    PeriodDifference = periodDifference,
                    IsLoanCompleted = isLoanCompleted
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error calculating next invoice date", error = ex.Message });
            }
        }

        [HttpGet("historical-payments")]
        public async Task<IActionResult> GetHistoricalPayments([FromQuery] int year, [FromQuery] int month)
        {
            try
            {
                // Create start and end dates for the specified month/year
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Get all loan payments for the specified period with loan and debtor details
                var paymentsWithLoans = await _dbContext.LoanPayments
                    .Include(p => p.Loan!)
                        .ThenInclude(l => l.DebtorDetails)
                    .Include(p => p.Loan!)
                        .ThenInclude(l => l.LoanCollaterals!)
                            .ThenInclude(lc => lc.Collateral)
                    .Where(p => p.DueDate >= startDate && p.DueDate <= endDate)
                    .ToListAsync();

                var historicalData = paymentsWithLoans.Where(p => p.Loan != null).Select(payment => new
                {
                    LoanID = payment.Loan!.LoanID,
                    DebtorName = payment.Loan.DebtorDetails?.DebtorName ?? "N/A",
                    ContactPerson = payment.Loan.DebtorDetails?.ContactPerson ?? "",
                    Address = payment.Loan.DebtorDetails?.Address ?? "",
                    Email = payment.Loan.DebtorDetails?.Email ?? "",
                    LoanAmount = payment.Loan.LoanAmount,
                    OutstandingAmount = payment.Loan.OutstandingAmount,
                    AnnualInterestRate = payment.Loan.AnnualInterestRate,
                    TenorMonths = payment.Loan.TenorMonths,
                    StartDate = payment.Loan.StartDate,
                    Status = payment.Loan.Status,
                    PaymentStatus = payment.PaymentStatus,
                    DueDate = payment.DueDate,
                    PaymentDate = payment.PaymentDate,
                    InterestAmount = payment.InterestAmount,
                    CapitalAmount = payment.CapitalAmount,
                    TotalAmount = payment.TotalAmount,
                    DaysLate = payment.DaysLate,
                    RemainingBalance = payment.RemainingBalance,
                    Collaterals = payment.Loan.LoanCollaterals?.Select(lc => new
                    {
                        CollateralId = lc.Collateral!.CollateralId,
                        CollateralType = lc.Collateral.CollateralType,
                        Description = lc.Collateral.Description,
                        AppraisalValue = lc.Collateral.AppraisalValue,
                        AppraisalDate = lc.Collateral.AppraisalDate,
                        LandRegistryCode = lc.Collateral.LandRegistryCode,
                        PostalCode = lc.Collateral.PostalCode,
                        HouseNumber = lc.Collateral.HouseNumber,
                        PropertyAddress = lc.Collateral.PropertyAddress,
                        AssetUniqueId = lc.Collateral.AssetUniqueId
                    }).ToList()
                }).ToList();

                return Ok(historicalData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving historical payments", error = ex.Message });
            }
        }

        /// <summary>
        /// Cleans up collateral strings by converting empty strings to null
        /// This prevents unique constraint violations in the database
        /// </summary>
        private void CleanCollateralStrings(Collateral collateral)
        {
            // Convert empty strings to null for unique identifier fields
            if (string.IsNullOrWhiteSpace(collateral.LandRegistryCode))
                collateral.LandRegistryCode = null;
            
            if (string.IsNullOrWhiteSpace(collateral.PostalCode))
                collateral.PostalCode = null;
            
            if (string.IsNullOrWhiteSpace(collateral.HouseNumber))
                collateral.HouseNumber = null;
            
            if (string.IsNullOrWhiteSpace(collateral.AssetUniqueId))
                collateral.AssetUniqueId = null;
            
            if (string.IsNullOrWhiteSpace(collateral.PropertyAddress))
                collateral.PropertyAddress = null;
        }
    }
}