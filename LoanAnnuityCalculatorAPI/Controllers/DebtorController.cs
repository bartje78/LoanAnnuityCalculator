using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Services; // Correctly reference the namespace
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Models.Debtor;
using LoanAnnuityCalculatorAPI.Models.DTOs;
using LoanAnnuityCalculatorAPI.Models.Loan;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/debtor")]
    [Authorize] // Require authentication for all debtor endpoints
    public class DebtorController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;
        private readonly RatioCalculationService _ratioCalculationService;
        private readonly LoanFinancialCalculatorService _loanFinancialCalculator;
        private readonly BalanceSheetMigrationService _migrationService;

        public DebtorController(
            LoanDbContext dbContext, 
            RatioCalculationService ratioCalculationService,
            BalanceSheetMigrationService migrationService)
        {
            _dbContext = dbContext;
            _ratioCalculationService = ratioCalculationService;
            _loanFinancialCalculator = new LoanFinancialCalculatorService();
            _migrationService = migrationService;
        }

        // GET: api/debtor
        [HttpGet]
        public async Task<IActionResult> GetDebtors([FromQuery] bool includeProspects = false)
        {
            try
            {
                // Fetch debtors from the database, filtering out prospects by default
                var query = _dbContext.DebtorDetails.AsQueryable();
                
                if (!includeProspects)
                {
                    query = query.Where(d => !d.IsProspect);
                }
                
                var debtors = await query.ToListAsync();

                if (!debtors.Any())
                {
                    return NotFound(new { Message = "No debtors found." });
                }

                return Ok(debtors); // Return the full DebtorDetails model
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving debtors.", Error = ex.Message });
            }
        }

        // GET: api/debtor/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDebtorById(int id)
        {
            try
            {
                var debtor = await _dbContext.DebtorDetails
                    .FirstOrDefaultAsync(d => d.DebtorID == id);

                if (debtor == null)
                {
                    return NotFound(new { Message = "Debtor not found." });
                }

                return Ok(debtor);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving the debtor.", Error = ex.Message });
            }
        }

        // PUT: api/debtor/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDebtor(int id, [FromBody] DebtorDetails updatedDebtor)
        {
            try
            {
                Console.WriteLine($"UpdateDebtor called for ID: {id}");
                Console.WriteLine($"Received CorporateTaxRate: {updatedDebtor.CorporateTaxRate}");
                
                if (id != updatedDebtor.DebtorID)
                {
                    return BadRequest(new { Message = "Debtor ID mismatch." });
                }

                var existingDebtor = await _dbContext.DebtorDetails
                    .FirstOrDefaultAsync(d => d.DebtorID == id);

                if (existingDebtor == null)
                {
                    return NotFound(new { Message = "Debtor not found." });
                }

                // Update the debtor properties
                existingDebtor.DebtorName = updatedDebtor.DebtorName;
                existingDebtor.ContactPerson = updatedDebtor.ContactPerson;
                existingDebtor.Address = updatedDebtor.Address;
                existingDebtor.Email = updatedDebtor.Email;
                existingDebtor.IsProspect = updatedDebtor.IsProspect;
                
                // Update new contact details
                existingDebtor.ContactCallingName = updatedDebtor.ContactCallingName;
                existingDebtor.ContactFirstNames = updatedDebtor.ContactFirstNames;
                existingDebtor.ContactLastName = updatedDebtor.ContactLastName;
                
                // Update address details
                existingDebtor.Street = updatedDebtor.Street;
                existingDebtor.HouseNumber = updatedDebtor.HouseNumber;
                existingDebtor.PostalCode = updatedDebtor.PostalCode;
                existingDebtor.City = updatedDebtor.City;
                
                // Update signatories
                existingDebtor.Signatory1Name = updatedDebtor.Signatory1Name;
                existingDebtor.Signatory1Function = updatedDebtor.Signatory1Function;
                existingDebtor.Signatory2Name = updatedDebtor.Signatory2Name;
                existingDebtor.Signatory2Function = updatedDebtor.Signatory2Function;
                existingDebtor.Signatory3Name = updatedDebtor.Signatory3Name;
                existingDebtor.Signatory3Function = updatedDebtor.Signatory3Function;
                
                // Update corporate tax rate
                existingDebtor.CorporateTaxRate = updatedDebtor.CorporateTaxRate;
                Console.WriteLine($"Set CorporateTaxRate to: {existingDebtor.CorporateTaxRate}");

                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"Saved to database. Reading back: {existingDebtor.CorporateTaxRate}");

                return Ok(new { Message = "Debtor updated successfully.", Debtor = existingDebtor });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while updating the debtor.", Error = ex.Message });
            }
        }

        // POST: api/debtor
        [HttpPost]
        public async Task<IActionResult> CreateDebtor([FromBody] DebtorDetails newDebtor)
        {
            try
            {
                if (newDebtor == null)
                {
                    return BadRequest(new { Message = "Debtor data is required." });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(newDebtor.DebtorName))
                {
                    return BadRequest(new { Message = "Debtor name is required." });
                }

                // Reset DebtorID to 0 so Entity Framework will generate a new one
                newDebtor.DebtorID = 0;

                _dbContext.DebtorDetails.Add(newDebtor);
                await _dbContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetDebtorById), new { id = newDebtor.DebtorID }, new { 
                    Message = "Debtor created successfully.", 
                    Debtor = newDebtor 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while creating the debtor.", Error = ex.Message });
            }
        }

        // DELETE: api/debtor/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDebtor(int id)
        {
            try
            {
                var debtor = await _dbContext.DebtorDetails
                    .Include(d => d.Loans)
                    .Include(d => d.BalanceSheets)
                    .Include(d => d.ProfitAndLossStatements)
                    .FirstOrDefaultAsync(d => d.DebtorID == id);

                if (debtor == null)
                {
                    return NotFound(new { Message = "Debtor not found." });
                }

                // Log what will be deleted
                var relatedRecordsCount = new
                {
                    Loans = debtor.Loans.Count,
                    BalanceSheets = debtor.BalanceSheets.Count,
                    ProfitAndLoss = debtor.ProfitAndLossStatements.Count
                };

                Console.WriteLine($"Deleting debtor {debtor.DebtorName} (ID: {id}) with {relatedRecordsCount.Loans} loans, {relatedRecordsCount.BalanceSheets} balance sheets, and {relatedRecordsCount.ProfitAndLoss} P&L records.");

                // Delete the debtor (cascade delete will handle related records)
                _dbContext.DebtorDetails.Remove(debtor);
                await _dbContext.SaveChangesAsync();

                return Ok(new { 
                    Message = "Debtor and all related records deleted successfully.",
                    DeletedRecords = relatedRecordsCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting the debtor.", Error = ex.Message });
            }
        }


        // GET: api/debtor/ratios?debtorId=1
        [HttpGet("ratios")]
        public async Task<IActionResult> GetDebtorRatios([FromQuery] int debtorId)
        {
            try
            {
                // Fetch the debtor and related financial data
                var debtor = await _dbContext.DebtorDetails
                    .Include(d => d.BalanceSheets)
                    .Include(d => d.ProfitAndLossStatements)
                    .FirstOrDefaultAsync(d => d.DebtorID == debtorId);

                if (debtor == null)
                {
                    return NotFound(new { Message = "Debtor not found." });
                }

                // Get the latest balance sheet and profit & loss statement
                var latestBalanceSheet = debtor.BalanceSheets.OrderByDescending(bs => bs.BookYear).FirstOrDefault();
                var latestPL = debtor.ProfitAndLossStatements.OrderByDescending(pl => pl.BookYear).FirstOrDefault();

                if (latestBalanceSheet == null || latestPL == null)
                {
                    return NotFound(new { Message = "Financial data not found for the debtor." });
                }

                // Delegate ratio calculation to the service
                var ratios = _ratioCalculationService.CalculateRatios(latestBalanceSheet, latestPL);

                return Ok(ratios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while calculating ratios.", Error = ex.Message });
            }
        }

        // GET: api/debtor/ratios/historical?debtorId=1
        [HttpGet("ratios/historical")]
        public async Task<IActionResult> GetDebtorRatiosHistorical([FromQuery] int debtorId)
        {
            try
            {
                // Fetch the debtor and related financial data
                var debtor = await _dbContext.DebtorDetails
                    .Include(d => d.BalanceSheets)
                    .Include(d => d.ProfitAndLossStatements)
                    .FirstOrDefaultAsync(d => d.DebtorID == debtorId);

                if (debtor == null)
                {
                    return NotFound(new { Message = "Debtor not found." });
                }

                // Get all years where both balance sheet and P&L data exist
                var balanceSheetYears = debtor.BalanceSheets.Select(bs => bs.BookYear).ToHashSet();
                var plYears = debtor.ProfitAndLossStatements.Select(pl => pl.BookYear).ToHashSet();
                var commonYears = balanceSheetYears.Intersect(plYears).OrderBy(year => year).ToList();

                if (!commonYears.Any())
                {
                    return NotFound(new { Message = "No complete financial data found for the debtor." });
                }

                var historicalRatios = new List<object>();

                foreach (var year in commonYears)
                {
                    var balanceSheet = debtor.BalanceSheets.FirstOrDefault(bs => bs.BookYear == year);
                    var profitLoss = debtor.ProfitAndLossStatements.FirstOrDefault(pl => pl.BookYear == year);

                    if (balanceSheet != null && profitLoss != null)
                    {
                        var ratios = _ratioCalculationService.CalculateRatios(balanceSheet, profitLoss);
                        historicalRatios.Add(new
                        {
                            Year = year,
                            Ratios = ratios
                        });
                    }
                }

                return Ok(historicalRatios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while calculating historical ratios.", Error = ex.Message });
            }
        }


        // GET: api/debtor/balancesheet?debtorId=1
        [HttpGet("balancesheet")]
        public async Task<IActionResult> GetBalanceSheetForAllYears([FromQuery] int debtorId)
        {
            try
            {
                // Fetch all balance sheets for the given debtor
                var balanceSheets = await _dbContext.DebtorBalanceSheets
                    .Where(bs => bs.DebtorID == debtorId)
                    .ToListAsync();

                if (!balanceSheets.Any())
                {
                    return Ok(null);
                }

                return Ok(balanceSheets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving balance sheets.", Error = ex.Message });
            }
        }

        // GET: api/debtor/balancesheet/year?debtorId=1&year=2023
                [HttpGet("balancesheet/year")]
        public async Task<IActionResult> GetBalanceSheetForSpecificYear([FromQuery] int debtorId, [FromQuery] int year)
        {
            try
            {
                // Fetch the balance sheet for the given debtor and year
                var balanceSheet = await _dbContext.DebtorBalanceSheets
                    .Where(bs => bs.DebtorID == debtorId && bs.BookYear == year) // Add BookYear condition
                    .FirstOrDefaultAsync();
        
                if (balanceSheet == null)
                {
                    Console.WriteLine($"No balance sheet found for DebtorID: {debtorId}, Year: {year}");
                    return Ok(null);
                }
        
                return Ok(balanceSheet);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving the balance sheet.", Error = ex.Message });
            }
        }

        // GET: api/debtor/{id}/balance-sheet-details
        [HttpGet("{id}/balance-sheet-details")]
        public async Task<ActionResult<BalanceSheetWithLineItemsDto>> GetBalanceSheetDetails(int id)
        {
            try
            {
                var balanceSheet = await _dbContext.DebtorBalanceSheets
                    .Include(bs => bs.LineItems)
                        .ThenInclude(li => li.Loan)
                    .Include(bs => bs.LineItems)
                        .ThenInclude(li => li.Collateral)
                    .Where(bs => bs.DebtorID == id)
                    .OrderByDescending(bs => bs.BookYear)
                    .FirstOrDefaultAsync();

                if (balanceSheet == null)
                    return NotFound(new { Message = $"No balance sheet found for debtor {id}" });

                var dto = new BalanceSheetWithLineItemsDto
                {
                    Id = balanceSheet.Id,
                    DebtorID = balanceSheet.DebtorID,
                    BookYear = balanceSheet.BookYear,
                    IsProForma = balanceSheet.IsProForma,
                    CurrentAssets = balanceSheet.CurrentAssets,
                    LongTermAssets = balanceSheet.LongTermAssets,
                    CurrentLiabilities = balanceSheet.CurrentLiabilities,
                    LongTermLiabilities = balanceSheet.LongTermLiabilities,
                    OwnersEquity = balanceSheet.OwnersEquity,
                    LineItems = balanceSheet.LineItems
                        .OrderBy(li => li.Category)
                        .ThenBy(li => li.DisplayOrder)
                        .Select(li => new BalanceSheetLineItemDto
                        {
                            Id = li.Id,
                            Category = li.Category,
                            Label = li.Label,
                            Amount = li.Amount,
                            DisplayOrder = li.DisplayOrder,
                            Notes = li.Notes,
                            IsAutoGenerated = li.IsAutoGenerated,
                            LoanReference = li.Loan != null ? new LoanReferenceDto
                            {
                                LoanId = li.Loan.LoanID,
                                Rate = li.Loan.AnnualInterestRate,
                                OutstandingAmount = li.Loan.OutstandingAmount,
                                StartDate = li.Loan.StartDate,
                                TenorMonths = li.Loan.TenorMonths,
                                Status = li.Loan.Status
                            } : null,
                            CollateralReference = li.Collateral != null ? new CollateralReferenceDto
                            {
                                CollateralId = li.Collateral.CollateralId,
                                PropertyType = li.Collateral.PropertyType,
                                Address = li.Collateral.PropertyAddress,
                                AppraisalValue = li.Collateral.AppraisalValue,
                                AppraisalDate = li.Collateral.AppraisalDate,
                                CollateralType = li.Collateral.CollateralType
                            } : null
                        })
                        .ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving balance sheet details.", Error = ex.Message });
            }
        }

        // GET: api/debtor/balance-sheet/{balanceSheetId}/details
        [HttpGet("balance-sheet/{balanceSheetId}/details")]
        public async Task<ActionResult<BalanceSheetWithLineItemsDto>> GetBalanceSheetDetailsById(int balanceSheetId)
        {
            try
            {
                var balanceSheet = await _dbContext.DebtorBalanceSheets
                    .Include(bs => bs.LineItems)
                        .ThenInclude(li => li.Loan)
                    .Include(bs => bs.LineItems)
                        .ThenInclude(li => li.Collateral)
                    .Where(bs => bs.Id == balanceSheetId)
                    .FirstOrDefaultAsync();

                if (balanceSheet == null)
                    return NotFound(new { Message = $"Balance sheet with ID {balanceSheetId} not found" });

                var dto = new BalanceSheetWithLineItemsDto
                {
                    Id = balanceSheet.Id,
                    DebtorID = balanceSheet.DebtorID,
                    BookYear = balanceSheet.BookYear,
                    IsProForma = balanceSheet.IsProForma,
                    CurrentAssets = balanceSheet.CurrentAssets,
                    LongTermAssets = balanceSheet.LongTermAssets,
                    CurrentLiabilities = balanceSheet.CurrentLiabilities,
                    LongTermLiabilities = balanceSheet.LongTermLiabilities,
                    OwnersEquity = balanceSheet.OwnersEquity,
                    LineItems = balanceSheet.LineItems
                        .OrderBy(li => li.Category)
                        .ThenBy(li => li.DisplayOrder)
                        .Select(li => new BalanceSheetLineItemDto
                        {
                            Id = li.Id,
                            Category = li.Category,
                            Label = li.Label,
                            Amount = li.Amount,
                            DisplayOrder = li.DisplayOrder,
                            Notes = li.Notes,
                            IsAutoGenerated = li.IsAutoGenerated,
                            LoanReference = li.Loan != null ? new LoanReferenceDto
                            {
                                LoanId = li.Loan.LoanID,
                                Rate = li.Loan.AnnualInterestRate,
                                OutstandingAmount = li.Loan.OutstandingAmount,
                                StartDate = li.Loan.StartDate,
                                TenorMonths = li.Loan.TenorMonths,
                                Status = li.Loan.Status
                            } : null,
                            CollateralReference = li.Collateral != null ? new CollateralReferenceDto
                            {
                                CollateralId = li.Collateral.CollateralId,
                                PropertyType = li.Collateral.PropertyType,
                                Address = li.Collateral.PropertyAddress,
                                AppraisalValue = li.Collateral.AppraisalValue,
                                AppraisalDate = li.Collateral.AppraisalDate,
                                CollateralType = li.Collateral.CollateralType
                            } : null
                        })
                        .ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving balance sheet details.", Error = ex.Message });
            }
        }

        // PUT: api/debtor/balance-sheet/{balanceSheetId}/line-items
        [HttpPut("balance-sheet/{balanceSheetId}/line-items")]
        public async Task<IActionResult> UpdateBalanceSheetLineItems(int balanceSheetId, [FromBody] List<BalanceSheetLineItemDto> lineItems)
        {
            try
            {
                var balanceSheet = await _dbContext.DebtorBalanceSheets
                    .Include(bs => bs.LineItems)
                    .FirstOrDefaultAsync(bs => bs.Id == balanceSheetId);

                if (balanceSheet == null)
                    return NotFound(new { Message = $"Balance sheet with ID {balanceSheetId} not found" });

                // Remove all existing manual (non-auto-generated) line items
                var manualItems = balanceSheet.LineItems.Where(li => !li.IsAutoGenerated).ToList();
                _dbContext.BalanceSheetLineItems.RemoveRange(manualItems);

                // Update or add line items
                foreach (var dto in lineItems)
                {
                    if (dto.IsAutoGenerated)
                    {
                        // Check if this auto-generated item already exists
                        var existingAutoItem = balanceSheet.LineItems.FirstOrDefault(li => 
                            li.IsAutoGenerated && 
                            li.LoanId == dto.LoanId && 
                            li.CollateralId == dto.CollateralId &&
                            li.Category == dto.Category);
                        
                        if (existingAutoItem != null)
                        {
                            // Update existing auto-generated item (for external loans or edited items)
                            existingAutoItem.Amount = dto.Amount;
                            existingAutoItem.Label = dto.Label ?? existingAutoItem.Label;
                            existingAutoItem.Notes = dto.Notes ?? existingAutoItem.Notes;
                        }
                        else
                        {
                            // Add new auto-generated item (for newly created balance sheets)
                            var autoLineItem = new BalanceSheetLineItem
                            {
                                BalanceSheetId = balanceSheetId,
                                Category = dto.Category,
                                Label = dto.Label ?? "Auto-generated Item",
                                Amount = dto.Amount,
                                DisplayOrder = dto.DisplayOrder,
                                Notes = dto.Notes,
                                IsAutoGenerated = true,
                                LoanId = dto.LoanId,
                                CollateralId = dto.CollateralId
                            };
                            balanceSheet.LineItems.Add(autoLineItem);
                        }
                        continue;
                    }
                    
                    // Add new manual line items
                    var lineItem = new BalanceSheetLineItem
                    {
                        BalanceSheetId = balanceSheetId,
                        Category = dto.Category,
                        Label = dto.Label ?? "Unnamed Item",
                        Amount = dto.Amount,
                        DisplayOrder = dto.DisplayOrder,
                        Notes = dto.Notes,
                        IsAutoGenerated = false,
                        LoanId = dto.LoanId,
                        CollateralId = dto.CollateralId
                    };
                    balanceSheet.LineItems.Add(lineItem);
                }

                // Recalculate totals from line items
                balanceSheet.CurrentAssets = lineItems.Where(li => li.Category == "CurrentAssets").Sum(li => li.Amount);
                balanceSheet.LongTermAssets = lineItems.Where(li => li.Category == "FixedAssets").Sum(li => li.Amount);
                balanceSheet.CurrentLiabilities = lineItems.Where(li => li.Category == "CurrentLiabilities").Sum(li => li.Amount);
                balanceSheet.LongTermLiabilities = lineItems.Where(li => li.Category == "LongTermLiabilities").Sum(li => li.Amount);
                
                // Calculate equity
                var totalAssets = balanceSheet.CurrentAssets + balanceSheet.LongTermAssets;
                var totalLiabilities = balanceSheet.CurrentLiabilities + balanceSheet.LongTermLiabilities;
                balanceSheet.OwnersEquity = totalAssets - totalLiabilities;

                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = "Line items updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while updating line items.", Error = ex.Message });
            }
        }

        // POST/GET: api/debtor/{id}/migrate-balance-sheet
        [HttpPost("{id}/migrate-balance-sheet")]
        [HttpGet("{id}/migrate-balance-sheet")]
        public async Task<IActionResult> MigrateDebtorBalanceSheets(int id)
        {
            try
            {
                var debtor = await _dbContext.DebtorDetails.FindAsync(id);
                if (debtor == null)
                    return NotFound(new { Message = $"Debtor {id} not found" });

                var migratedCount = await _migrationService.MigrateDebtorBalanceSheets(id);
                
                return Ok(new
                {
                    Message = $"Successfully migrated {migratedCount} balance sheet(s) for debtor {id}",
                    DebtorId = id,
                    MigratedCount = migratedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while migrating balance sheets.", Error = ex.Message });
            }
        }

        // POST/GET: api/debtor/migrate-all-balance-sheets
        [HttpPost("migrate-all-balance-sheets")]
        [HttpGet("migrate-all-balance-sheets")]
        public async Task<IActionResult> MigrateAllBalanceSheets()
        {
            try
            {
                var migratedCount = await _migrationService.MigrateAllBalanceSheets();
                
                return Ok(new
                {
                    Message = $"Successfully migrated {migratedCount} balance sheet(s)",
                    MigratedCount = migratedCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while migrating all balance sheets.", Error = ex.Message });
            }
        }

        // POST: api/debtor/balance-sheet/{balanceSheetId}/refresh
        [HttpPost("balance-sheet/{balanceSheetId}/refresh")]
        public async Task<IActionResult> RefreshBalanceSheetLineItems(int balanceSheetId)
        {
            try
            {
                await _migrationService.RefreshAutoGeneratedLineItems(balanceSheetId);
                
                return Ok(new
                {
                    Message = $"Successfully refreshed auto-generated line items for balance sheet {balanceSheetId}",
                    BalanceSheetId = balanceSheetId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while refreshing line items.", Error = ex.Message });
            }
        }

        // GET: api/debtor/profitloss
        [HttpGet("profitloss")]
        public async Task<IActionResult> GetProfitLoss(int debtorId)
        {
            try
            {
                var profitLossStatements = await _dbContext.DebtorPLs
                    .Include(pl => pl.RevenueDetails) // Include revenue details
                    .Where(pl => pl.DebtorID == debtorId)
                    .OrderBy(pl => pl.BookYear)
                    .ToListAsync();

                if (!profitLossStatements.Any())
                {
                    return NotFound(new { Message = $"No profit and loss statements found for DebtorID: {debtorId}." });
                }

                // Map to clean response format to avoid circular references
                var cleanPLStatements = profitLossStatements.Select(pl => new
                {
                    Id = pl.Id,
                    DebtorID = pl.DebtorID,
                    BookYear = pl.BookYear,
                    Revenue = pl.Revenue,
                    OperatingExpenses = pl.OperatingExpenses,
                    CostOfGoodsSold = pl.CostOfGoodsSold,
                    EBITDA = pl.EBITDA,
                    InterestExpense = pl.InterestExpense,
                    TaxExpense = pl.TaxExpense,
                    NetIncome = pl.NetIncome,
                    RevenueDetails = pl.RevenueDetails?.Select(rd => new
                    {
                        RevenueDetailId = rd.RevenueDetailId,
                        RevenueCategory = rd.RevenueCategory,
                        Description = rd.Description,
                        Amount = rd.Amount,
                        IsRecurring = rd.IsRecurring,
                        Notes = rd.Notes
                    }) ?? Enumerable.Empty<object>()
                }).ToList();

                return Ok(cleanPLStatements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving profit and loss statements.", Error = ex.Message });
            }
        }

        // GET: api/debtor/profitloss/year
        [HttpGet("profitloss/year")]
        public async Task<IActionResult> GetProfitLossByYear(int debtorId, int year)
        {
            try
            {
                var profitLoss = await _dbContext.DebtorPLs
                    .Include(pl => pl.RevenueDetails) // Include revenue details
                    .Where(pl => pl.DebtorID == debtorId && pl.BookYear == year)
                    .FirstOrDefaultAsync();

                if (profitLoss == null)
                {
                    return NotFound(new { Message = $"No profit and loss statement found for DebtorID: {debtorId} and Year: {year}." });
                }

                // Return clean response to avoid circular references
                var cleanPL = new
                {
                    Id = profitLoss.Id,
                    DebtorID = profitLoss.DebtorID,
                    BookYear = profitLoss.BookYear,
                    Revenue = profitLoss.Revenue,
                    OperatingExpenses = profitLoss.OperatingExpenses,
                    CostOfGoodsSold = profitLoss.CostOfGoodsSold,
                    EBITDA = profitLoss.EBITDA,
                    InterestExpense = profitLoss.InterestExpense,
                    TaxExpense = profitLoss.TaxExpense,
                    NetIncome = profitLoss.NetIncome,
                    RevenueDetails = profitLoss.RevenueDetails?.Select(rd => new
                    {
                        RevenueDetailId = rd.RevenueDetailId,
                        RevenueCategory = rd.RevenueCategory,
                        Description = rd.Description,
                        Amount = rd.Amount,
                        IsRecurring = rd.IsRecurring,
                        Notes = rd.Notes
                    }) ?? Enumerable.Empty<object>()
                };

                return Ok(cleanPL);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while retrieving the profit and loss statement.", Error = ex.Message });
            }
        }

        // ===========================================
        // BALANCE SHEET CRUD OPERATIONS
        // ===========================================

        // POST: api/debtor/balancesheet
        [HttpPost("balancesheet")]
        public async Task<IActionResult> CreateBalanceSheet([FromBody] UpdateBalanceSheetDto balanceSheetDto)
        {
            try
            {
                // Validate debtor exists
                var debtor = await _dbContext.DebtorDetails.FindAsync(balanceSheetDto.DebtorID);
                if (debtor == null)
                {
                    return NotFound(new { Message = "Debtor not found." });
                }

                // Check if balance sheet for this year already exists
                var existing = await _dbContext.DebtorBalanceSheets
                    .FirstOrDefaultAsync(bs => bs.DebtorID == balanceSheetDto.DebtorID && bs.BookYear == balanceSheetDto.BookYear);
                
                if (existing != null)
                {
                    return BadRequest(new { Message = $"Balance sheet for year {balanceSheetDto.BookYear} already exists for this debtor." });
                }

                // Create new DebtorBalanceSheet from DTO
                var balanceSheet = new DebtorBalanceSheet
                {
                    DebtorID = balanceSheetDto.DebtorID,
                    BookYear = balanceSheetDto.BookYear,
                    IsProForma = balanceSheetDto.IsProForma,
                    CurrentAssets = balanceSheetDto.CurrentAssets,
                    LongTermAssets = balanceSheetDto.LongTermAssets,
                    CurrentLiabilities = balanceSheetDto.CurrentLiabilities,
                    LongTermLiabilities = balanceSheetDto.LongTermLiabilities,
                    OwnersEquity = balanceSheetDto.OwnersEquity,
                    FirstLienLoanAmount = balanceSheetDto.FirstLienLoanAmount,
                    FirstLienInterestRate = balanceSheetDto.FirstLienInterestRate,
                    FirstLienTenorMonths = balanceSheetDto.FirstLienTenorMonths,
                    FirstLienRedemptionSchedule = balanceSheetDto.FirstLienRedemptionSchedule,
                    DebtorDetails = debtor
                };

                _dbContext.DebtorBalanceSheets.Add(balanceSheet);
                await _dbContext.SaveChangesAsync();

                // Return clean response to avoid circular references
                var cleanResponse = new
                {
                    Id = balanceSheet.Id,
                    DebtorID = balanceSheet.DebtorID,
                    BookYear = balanceSheet.BookYear,
                    IsProForma = balanceSheet.IsProForma,
                    CurrentAssets = balanceSheet.CurrentAssets,
                    LongTermAssets = balanceSheet.LongTermAssets,
                    CurrentLiabilities = balanceSheet.CurrentLiabilities,
                    LongTermLiabilities = balanceSheet.LongTermLiabilities,
                    OwnersEquity = balanceSheet.OwnersEquity,
                    FirstLienLoanAmount = balanceSheet.FirstLienLoanAmount,
                    FirstLienInterestRate = balanceSheet.FirstLienInterestRate,
                    FirstLienTenorMonths = balanceSheet.FirstLienTenorMonths,
                    FirstLienRedemptionSchedule = balanceSheet.FirstLienRedemptionSchedule
                };

                return CreatedAtAction(nameof(GetBalanceSheetForSpecificYear), 
                    new { debtorId = balanceSheet.DebtorID, year = balanceSheet.BookYear }, 
                    cleanResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while creating the balance sheet.", Error = ex.Message });
            }
        }

        // PUT: api/debtor/balancesheet/{id}
        [HttpPut("balancesheet/{id}")]
        public async Task<IActionResult> UpdateBalanceSheet(int id, [FromBody] UpdateBalanceSheetDto balanceSheetDto)
        {
            try
            {
                Console.WriteLine($"Updating balance sheet with ID: {id}");
                Console.WriteLine($"Received balance sheet: {System.Text.Json.JsonSerializer.Serialize(balanceSheetDto)}");

                if (id != balanceSheetDto.Id)
                {
                    Console.WriteLine($"ID mismatch: URL ID {id} vs Object ID {balanceSheetDto.Id}");
                    return BadRequest(new { Message = "Balance sheet ID mismatch." });
                }

                var existing = await _dbContext.DebtorBalanceSheets.FindAsync(id);
                if (existing == null)
                {
                    Console.WriteLine($"Balance sheet with ID {id} not found");
                    return NotFound(new { Message = "Balance sheet not found." });
                }

                Console.WriteLine($"Found existing balance sheet: {System.Text.Json.JsonSerializer.Serialize(existing)}");

                // Update properties
                existing.BookYear = balanceSheetDto.BookYear;
                existing.IsProForma = balanceSheetDto.IsProForma;
                existing.CurrentAssets = balanceSheetDto.CurrentAssets;
                existing.LongTermAssets = balanceSheetDto.LongTermAssets;
                existing.CurrentLiabilities = balanceSheetDto.CurrentLiabilities;
                existing.LongTermLiabilities = balanceSheetDto.LongTermLiabilities;
                existing.OwnersEquity = balanceSheetDto.OwnersEquity;
                
                // Update first lien loan fields
                existing.FirstLienLoanAmount = balanceSheetDto.FirstLienLoanAmount;
                existing.FirstLienInterestRate = balanceSheetDto.FirstLienInterestRate;
                existing.FirstLienTenorMonths = balanceSheetDto.FirstLienTenorMonths;
                existing.FirstLienRedemptionSchedule = balanceSheetDto.FirstLienRedemptionSchedule;

                Console.WriteLine($"Updated existing balance sheet before save: {System.Text.Json.JsonSerializer.Serialize(existing)}");

                await _dbContext.SaveChangesAsync();

                Console.WriteLine("Balance sheet updated successfully");
                return Ok(existing);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating balance sheet: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "An error occurred while updating the balance sheet.", Error = ex.Message });
            }
        }

        // DELETE: api/debtor/balancesheet/{id}
        [HttpDelete("balancesheet/{id}")]
        public async Task<IActionResult> DeleteBalanceSheet(int id)
        {
            try
            {
                var balanceSheet = await _dbContext.DebtorBalanceSheets.FindAsync(id);
                if (balanceSheet == null)
                {
                    return NotFound(new { Message = "Balance sheet not found." });
                }

                _dbContext.DebtorBalanceSheets.Remove(balanceSheet);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = "Balance sheet deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting the balance sheet.", Error = ex.Message });
            }
        }

        // ===========================================
        // PROFIT & LOSS CRUD OPERATIONS
        // ===========================================

        // POST: api/debtor/profitloss
        [HttpPost("profitloss")]
        public async Task<IActionResult> CreateProfitLoss([FromBody] UpdateProfitLossDto profitLossDto)
        {
            try
            {
                // Validate debtor exists
                var debtor = await _dbContext.DebtorDetails.FindAsync(profitLossDto.DebtorID);
                if (debtor == null)
                {
                    return NotFound(new { Message = "Debtor not found." });
                }

                // Check if P&L for this year already exists
                var existing = await _dbContext.DebtorPLs
                    .FirstOrDefaultAsync(pl => pl.DebtorID == profitLossDto.DebtorID && pl.BookYear == profitLossDto.BookYear);
                
                if (existing != null)
                {
                    return BadRequest(new { Message = $"Profit & Loss for year {profitLossDto.BookYear} already exists for this debtor." });
                }

                // Create new DebtorPL from DTO
                var profitLoss = new DebtorPL
                {
                    DebtorID = profitLossDto.DebtorID,
                    BookYear = profitLossDto.BookYear,
                    IsProForma = profitLossDto.IsProForma,
                    Revenue = profitLossDto.Revenue,
                    OperatingExpenses = profitLossDto.OperatingExpenses,
                    CostOfGoodsSold = profitLossDto.CostOfGoodsSold,
                    EBITDA = profitLossDto.EBITDA,
                    InterestExpense = profitLossDto.InterestExpense,
                    TaxExpense = profitLossDto.TaxExpense,
                    NetIncome = profitLossDto.NetIncome,
                    DebtorDetails = debtor
                };

                _dbContext.DebtorPLs.Add(profitLoss);
                await _dbContext.SaveChangesAsync();

                // Return clean response to avoid circular references
                var cleanResponse = new
                {
                    Id = profitLoss.Id,
                    DebtorID = profitLoss.DebtorID,
                    BookYear = profitLoss.BookYear,
                    IsProForma = profitLoss.IsProForma,
                    Revenue = profitLoss.Revenue,
                    OperatingExpenses = profitLoss.OperatingExpenses,
                    CostOfGoodsSold = profitLoss.CostOfGoodsSold,
                    EBITDA = profitLoss.EBITDA,
                    InterestExpense = profitLoss.InterestExpense,
                    TaxExpense = profitLoss.TaxExpense,
                    NetIncome = profitLoss.NetIncome
                };

                return CreatedAtAction(nameof(GetProfitLossByYear), 
                    new { debtorId = profitLoss.DebtorID, year = profitLoss.BookYear }, 
                    cleanResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while creating the profit & loss statement.", Error = ex.Message });
            }
        }

        // PUT: api/debtor/profitloss/{id}
        [HttpPut("profitloss/{id}")]
        public async Task<IActionResult> UpdateProfitLoss(int id, [FromBody] UpdateProfitLossDto profitLossDto)
        {
            try
            {
                if (id != profitLossDto.Id)
                {
                    return BadRequest(new { Message = "Profit & Loss ID mismatch." });
                }

                var existing = await _dbContext.DebtorPLs.FindAsync(id);
                if (existing == null)
                {
                    return NotFound(new { Message = "Profit & Loss statement not found." });
                }

                // Update properties with new field names
                existing.BookYear = profitLossDto.BookYear;
                existing.IsProForma = profitLossDto.IsProForma;
                existing.Revenue = profitLossDto.Revenue;
                existing.OperatingExpenses = profitLossDto.OperatingExpenses;
                existing.CostOfGoodsSold = profitLossDto.CostOfGoodsSold;
                existing.EBITDA = profitLossDto.EBITDA;
                existing.InterestExpense = profitLossDto.InterestExpense;
                existing.TaxExpense = profitLossDto.TaxExpense;
                existing.NetIncome = profitLossDto.NetIncome;
                existing.RevenueSectorBreakdown = profitLossDto.RevenueSectorBreakdown;

                await _dbContext.SaveChangesAsync();

                // Return clean response
                var cleanResponse = new
                {
                    Id = existing.Id,
                    DebtorID = existing.DebtorID,
                    BookYear = existing.BookYear,
                    IsProForma = existing.IsProForma,
                    Revenue = existing.Revenue,
                    OperatingExpenses = existing.OperatingExpenses,
                    CostOfGoodsSold = existing.CostOfGoodsSold,
                    EBITDA = existing.EBITDA,
                    InterestExpense = existing.InterestExpense,
                    TaxExpense = existing.TaxExpense,
                    NetIncome = existing.NetIncome,
                    RevenueSectorBreakdown = existing.RevenueSectorBreakdown
                };

                return Ok(cleanResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while updating the profit & loss statement.", Error = ex.Message });
            }
        }

        // DELETE: api/debtor/profitloss/{id}
        [HttpDelete("profitloss/{id}")]
        public async Task<IActionResult> DeleteProfitLoss(int id)
        {
            try
            {
                var profitLoss = await _dbContext.DebtorPLs.FindAsync(id);
                if (profitLoss == null)
                {
                    return NotFound(new { Message = "Profit & Loss statement not found." });
                }

                _dbContext.DebtorPLs.Remove(profitLoss);
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = "Profit & Loss statement deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting the profit & loss statement.", Error = ex.Message });
            }
        }

        // Revenue Details Management Endpoints

        [HttpPost("profitloss/{plId}/revenuedetail")]
        public async Task<IActionResult> AddRevenueDetail(int plId, [FromBody] CreateRevenueDetailRequest request)
        {
            try
            {
                // Validate the model
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verify the P&L record exists
                var pl = await _dbContext.DebtorPLs.FindAsync(plId);
                if (pl == null)
                {
                    return NotFound(new { message = "Profit & Loss record not found." });
                }

                // Create the revenue detail from the request
                var revenueDetail = new RevenueDetail
                {
                    PLId = plId,
                    RevenueCategory = request.RevenueCategory,
                    Description = request.Description,
                    Amount = request.Amount,
                    IsRecurring = request.IsRecurring,
                    Notes = request.Notes,
                    DebtorPL = pl
                };

                _dbContext.RevenueDetails.Add(revenueDetail);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Revenue detail added successfully.", revenueDetailId = revenueDetail.RevenueDetailId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding revenue detail.", error = ex.Message });
            }
        }

        [HttpPut("revenuedetail/{revenueDetailId}")]
        public async Task<IActionResult> UpdateRevenueDetail(int revenueDetailId, [FromBody] UpdateRevenueDetailRequest request)
        {
            try
            {
                // Validate the model
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingRevenueDetail = await _dbContext.RevenueDetails.FindAsync(revenueDetailId);
                if (existingRevenueDetail == null)
                {
                    return NotFound(new { message = "Revenue detail not found." });
                }

                // Update the revenue detail properties
                existingRevenueDetail.RevenueCategory = request.RevenueCategory;
                existingRevenueDetail.Description = request.Description;
                existingRevenueDetail.Amount = request.Amount;
                existingRevenueDetail.IsRecurring = request.IsRecurring;
                existingRevenueDetail.Notes = request.Notes;

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Revenue detail updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating revenue detail.", error = ex.Message });
            }
        }

        [HttpDelete("revenuedetail/{revenueDetailId}")]
        public async Task<IActionResult> DeleteRevenueDetail(int revenueDetailId)
        {
            try
            {
                var revenueDetail = await _dbContext.RevenueDetails.FindAsync(revenueDetailId);
                if (revenueDetail == null)
                {
                    return NotFound(new { message = "Revenue detail not found." });
                }

                _dbContext.RevenueDetails.Remove(revenueDetail);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Revenue detail deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting revenue detail.", error = ex.Message });
            }
        }

        // GET: api/debtor/{id}/financials-with-loans
        [HttpGet("{id}/financials-with-loans")]
        public async Task<IActionResult> GetDebtorFinancialsWithLoans(int id)
        {
            try
            {
                var debtor = await _dbContext.DebtorDetails.FindAsync(id);
                if (debtor == null)
                {
                    return NotFound(new { message = "Debtor not found." });
                }

                // Get all loans for this debtor
                var loans = await _dbContext.Loans
                    .Include(l => l.LoanCollaterals)
                        .ThenInclude(lc => lc.Collateral)
                    .Where(l => l.DebtorID == id)
                    .ToListAsync();

                // Get balance sheets and P&L statements
                var balanceSheets = await _dbContext.DebtorBalanceSheets
                    .Where(bs => bs.DebtorID == id)
                    .OrderBy(bs => bs.BookYear)
                    .ToListAsync();

                var profitLossStatements = await _dbContext.DebtorPLs
                    .Where(pl => pl.DebtorID == id)
                    .OrderBy(pl => pl.BookYear)
                    .ToListAsync();

                // Determine the year range to calculate
                var allYears = new HashSet<int>();
                allYears.UnionWith(balanceSheets.Select(bs => bs.BookYear));
                allYears.UnionWith(profitLossStatements.Select(pl => pl.BookYear));
                
                // Add years for active loans
                foreach (var loan in loans)
                {
                    int loanStartYear = loan.StartDate.Year;
                    int loanEndYear = loan.StartDate.AddMonths(loan.TenorMonths).Year;
                    for (int year = loanStartYear; year <= loanEndYear; year++)
                    {
                        allYears.Add(year);
                    }
                }

                // Build loan summaries with yearly details
                var loanSummaries = new List<LoanSummaryDto>();
                foreach (var loan in loans)
                {
                    var collateralValue = _loanFinancialCalculator.GetTotalCollateralValue(loan);
                    
                    // Get primary collateral type (most common or first)
                    string? primaryCollateralType = null;
                    if (loan.LoanCollaterals != null && loan.LoanCollaterals.Any())
                    {
                        primaryCollateralType = loan.LoanCollaterals
                            .Select(lc => lc.Collateral?.PropertyType)
                            .Where(pt => !string.IsNullOrEmpty(pt))
                            .FirstOrDefault();
                    }
                    
                    var yearlyDetails = new List<LoanYearlyDetailsDto>();
                    foreach (var year in allYears.OrderBy(y => y))
                    {
                        var outstanding = _loanFinancialCalculator.CalculateOutstandingBalanceAtYear(loan, year);
                        var interest = _loanFinancialCalculator.CalculateInterestForYear(loan, year);
                        var redemption = _loanFinancialCalculator.CalculateRedemptionForYear(loan, year);

                        if (outstanding > 0 || interest > 0 || redemption > 0)
                        {
                            yearlyDetails.Add(new LoanYearlyDetailsDto
                            {
                                Year = year,
                                OutstandingBalance = outstanding,
                                InterestExpense = interest,
                                RedemptionAmount = redemption
                            });
                        }
                    }

                    loanSummaries.Add(new LoanSummaryDto
                    {
                        LoanId = loan.LoanID,
                        LoanAmount = loan.LoanAmount,
                        AnnualInterestRate = loan.AnnualInterestRate,
                        TenorMonths = loan.TenorMonths,
                        InterestOnlyMonths = loan.InterestOnlyMonths,
                        RedemptionSchedule = loan.RedemptionSchedule,
                        StartDate = loan.StartDate,
                        Status = loan.Status ?? "Unknown",
                        TotalCollateralValue = collateralValue,
                        PrimaryCollateralType = primaryCollateralType,
                        YearlyDetails = yearlyDetails
                    });
                }

                // Enhance balance sheets with loan data
                var enhancedBalanceSheets = new List<EnhancedBalanceSheetDto>();
                foreach (var bs in balanceSheets)
                {
                    var activeLoans = loans.Where(l => 
                        l.StartDate.Year <= bs.BookYear && 
                        l.StartDate.AddMonths(l.TenorMonths).Year >= bs.BookYear
                    ).ToList();

                    var totalDebt = activeLoans.Sum(l => 
                        _loanFinancialCalculator.CalculateOutstandingBalanceAtYear(l, bs.BookYear)
                    );

                    // Calculate total collateral value from UNIQUE collaterals, considering total allocation across all loans
                    // and subtracting any subordination (first mortgage amounts)
                    var collateralAllocations = activeLoans
                        .SelectMany(l => l.LoanCollaterals)
                        .Where(lc => lc.Collateral != null && lc.Collateral.AppraisalValue.HasValue)
                        .GroupBy(lc => lc.Collateral!.CollateralId)
                        .Select(g => new
                        {
                            Collateral = g.First().Collateral!,
                            TotalAllocation = Math.Min(100, g.Sum(lc => lc.AllocationPercentage)) // Cap at 100%
                        });

                    // Calculate gross collateral value
                    var grossCollateralValue = collateralAllocations.Sum(ca => 
                        ca.Collateral.AppraisalValue!.Value * (ca.TotalAllocation / 100m)
                    );

                    // Calculate total subordination amount from UNIQUE collaterals only (avoid double counting)
                    var totalSubordination = activeLoans
                        .SelectMany(l => l.LoanCollaterals)
                        .Where(lc => lc.Collateral != null && lc.Collateral.FirstMortgageAmount.HasValue)
                        .Select(lc => lc.Collateral!)
                        .Distinct()
                        .Sum(c => c.FirstMortgageAmount!.Value);

                    // Net collateral available = Gross collateral value - Subordination
                    var totalCollateral = grossCollateralValue - totalSubordination;

                    enhancedBalanceSheets.Add(new EnhancedBalanceSheetDto
                    {
                        Id = bs.Id,
                        DebtorID = bs.DebtorID,
                        BookYear = bs.BookYear,
                        CurrentAssets = bs.CurrentAssets,
                        LongTermAssets = bs.LongTermAssets,
                        CurrentLiabilities = bs.CurrentLiabilities,
                        LongTermLiabilities = bs.LongTermLiabilities,
                        OwnersEquity = bs.OwnersEquity,
                        TotalLoanDebt = totalDebt,
                        TotalCollateralValue = totalCollateral,
                        TotalSubordinationAmount = totalSubordination,
                        ActiveLoanIds = activeLoans.Select(l => l.LoanID).ToList(),
                        FirstLienLoanAmount = bs.FirstLienLoanAmount,
                        FirstLienInterestRate = bs.FirstLienInterestRate,
                        FirstLienTenorMonths = bs.FirstLienTenorMonths,
                        FirstLienRedemptionSchedule = bs.FirstLienRedemptionSchedule
                    });
                }

                // Enhance P&L statements with loan interest
                var enhancedProfitLoss = new List<EnhancedProfitLossDto>();
                foreach (var pl in profitLossStatements)
                {
                    var activeLoans = loans.Where(l => 
                        l.StartDate.Year <= pl.BookYear && 
                        l.StartDate.AddMonths(l.TenorMonths).Year >= pl.BookYear
                    ).ToList();

                    var calculatedInterest = activeLoans.Sum(l => 
                        _loanFinancialCalculator.CalculateInterestForYear(l, pl.BookYear)
                    );

                    enhancedProfitLoss.Add(new EnhancedProfitLossDto
                    {
                        Id = pl.Id,
                        DebtorID = pl.DebtorID,
                        BookYear = pl.BookYear,
                        Revenue = pl.Revenue,
                        OperatingExpenses = pl.OperatingExpenses,
                        CostOfGoodsSold = pl.CostOfGoodsSold,
                        EBITDA = pl.EBITDA ?? 0,
                        InterestExpense = pl.InterestExpense,
                        TaxExpense = pl.TaxExpense,
                        NetIncome = pl.NetIncome,
                        RevenueSectorBreakdown = pl.RevenueSectorBreakdown,
                        CalculatedLoanInterest = calculatedInterest,
                        OtherInterestExpense = pl.InterestExpense - calculatedInterest,
                        ActiveLoanIds = activeLoans.Select(l => l.LoanID).ToList()
                    });
                }

                var result = new DebtorFinancialWithLoansDto
                {
                    DebtorId = debtor.DebtorID,
                    DebtorName = debtor.DebtorName,
                    Loans = loanSummaries,
                    BalanceSheets = enhancedBalanceSheets,
                    ProfitLossStatements = enhancedProfitLoss
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving debtor financials with loans.", error = ex.Message });
            }
        }
        }
}