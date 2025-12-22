using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Middleware;
using LoanAnnuityCalculatorAPI.Models.Loan;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingDepotController : ControllerBase
    {
        private readonly LoanDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BuildingDepotController(
            LoanDbContext context, 
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Get all withdrawals
        /// </summary>
        [HttpGet("withdrawals")]
        public async Task<ActionResult<IEnumerable<BuildingDepotWithdrawal>>> GetAllWithdrawals()
        {
            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            var withdrawals = await _context.BuildingDepotWithdrawals
                .Where(w => !tenantId.HasValue || w.TenantId == tenantId)
                .Include(w => w.Loan!)
                    .ThenInclude(l => l.DebtorDetails!)
                .Include(w => w.LineItems)
                .OrderByDescending(w => w.CreatedDate)
                .ToListAsync();

            return Ok(withdrawals);
        }

        /// <summary>
        /// Get withdrawals for a specific loan
        /// </summary>
        [HttpGet("withdrawals/loan/{loanId}")]
        public async Task<ActionResult<IEnumerable<BuildingDepotWithdrawal>>> GetWithdrawalsByLoan(int loanId)
        {
            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            var withdrawals = await _context.BuildingDepotWithdrawals
                .Where(w => w.LoanId == loanId && (!tenantId.HasValue || w.TenantId == tenantId))
                .Include(w => w.Loan!)
                    .ThenInclude(l => l.DebtorDetails!)
                .Include(w => w.LineItems)
                .OrderByDescending(w => w.CreatedDate)
                .ToListAsync();

            return Ok(withdrawals);
        }

        /// <summary>
        /// Get a specific withdrawal by ID
        /// </summary>
        [HttpGet("withdrawals/{withdrawalId}")]
        public async Task<ActionResult<BuildingDepotWithdrawal>> GetWithdrawal(int withdrawalId)
        {
            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            var withdrawal = await _context.BuildingDepotWithdrawals
                .Where(w => w.WithdrawalId == withdrawalId && (!tenantId.HasValue || w.TenantId == tenantId))
                .Include(w => w.Loan!)
                    .ThenInclude(l => l.DebtorDetails!)
                .Include(w => w.LineItems)
                .FirstOrDefaultAsync();

            if (withdrawal == null)
            {
                return NotFound(new { message = "Withdrawal not found." });
            }

            return Ok(withdrawal);
        }

        /// <summary>
        /// Create a new withdrawal
        /// </summary>
        [HttpPost("withdrawals")]
        public async Task<ActionResult<BuildingDepotWithdrawal>> CreateWithdrawal([FromBody] BuildingDepotWithdrawal withdrawal)
        {
            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            // Validate loan exists and is a building depot
            var loan = await _context.Loans
                .FirstOrDefaultAsync(l => l.LoanID == withdrawal.LoanId && 
                                         (!tenantId.HasValue || l.TenantId == tenantId));

            if (loan == null)
            {
                return NotFound(new { message = "Loan not found." });
            }

            if (loan.RedemptionSchedule != "BuildingDepot")
            {
                return BadRequest(new { message = "Loan is not a building depot." });
            }

            // Check if sufficient credit available
            var availableCredit = (loan.CreditLimit ?? 0) - (loan.AmountDrawn ?? 0);
            if (withdrawal.TotalAmount > availableCredit)
            {
                return BadRequest(new { message = $"Insufficient credit. Available: {availableCredit:C2}" });
            }

            // Set tenant and dates
            withdrawal.TenantId = tenantId;
            withdrawal.CreatedDate = DateTime.UtcNow;
            if (withdrawal.Status == "Submitted")
            {
                withdrawal.SubmittedDate = DateTime.UtcNow;
            }

            // Add withdrawal
            _context.BuildingDepotWithdrawals.Add(withdrawal);

            // Update loan AmountDrawn
            loan.AmountDrawn = (loan.AmountDrawn ?? 0) + withdrawal.TotalAmount;

            await _context.SaveChangesAsync();

            // Return simplified response with LineItemIds for file uploads
            return Ok(new 
            {
                WithdrawalId = withdrawal.WithdrawalId,
                LoanId = withdrawal.LoanId,
                Status = "Submitted",
                Message = "Withdrawal created successfully",
                LineItems = withdrawal.LineItems.Select(li => new 
                {
                    LineItemId = li.LineItemId,
                    Description = li.Description,
                    Amount = li.Amount
                }).ToList()
            });
        }

        /// <summary>
        /// Update an existing withdrawal
        /// </summary>
        [HttpPut("withdrawals/{withdrawalId}")]
        public async Task<ActionResult<BuildingDepotWithdrawal>> UpdateWithdrawal(int withdrawalId, [FromBody] BuildingDepotWithdrawal withdrawal)
        {
            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            var existingWithdrawal = await _context.BuildingDepotWithdrawals
                .Include(w => w.LineItems)
                .FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId && 
                                         (!tenantId.HasValue || w.TenantId == tenantId));

            if (existingWithdrawal == null)
            {
                return NotFound(new { message = "Withdrawal not found." });
            }

            // Can only update draft withdrawals
            if (existingWithdrawal.Status != "Draft")
            {
                return BadRequest(new { message = "Can only update draft withdrawals." });
            }

            // Calculate difference in total amount
            var amountDifference = withdrawal.TotalAmount - existingWithdrawal.TotalAmount;

            // Update loan AmountDrawn
            if (amountDifference != 0)
            {
                var loan = await _context.Loans.FindAsync(existingWithdrawal.LoanId);
                if (loan != null)
                {
                    var availableCredit = (loan.CreditLimit ?? 0) - (loan.AmountDrawn ?? 0);
                    if (amountDifference > availableCredit)
                    {
                        return BadRequest(new { message = $"Insufficient credit. Available: {availableCredit:C2}" });
                    }

                    loan.AmountDrawn = (loan.AmountDrawn ?? 0) + amountDifference;
                }
            }

            // Update withdrawal properties
            existingWithdrawal.WithdrawalType = withdrawal.WithdrawalType;
            existingWithdrawal.WithdrawalDate = withdrawal.WithdrawalDate;
            existingWithdrawal.TotalAmount = withdrawal.TotalAmount;
            existingWithdrawal.Status = withdrawal.Status;
            if (withdrawal.Status == "Submitted" && !existingWithdrawal.SubmittedDate.HasValue)
            {
                existingWithdrawal.SubmittedDate = DateTime.UtcNow;
            }

            // Update line items (simplified - remove all and re-add)
            _context.BuildingDepotWithdrawalLineItems.RemoveRange(existingWithdrawal.LineItems);
            existingWithdrawal.LineItems = withdrawal.LineItems;

            await _context.SaveChangesAsync();

            // Reload with includes
            var updatedWithdrawal = await _context.BuildingDepotWithdrawals
                .Include(w => w.Loan!)
                    .ThenInclude(l => l.DebtorDetails!)
                .Include(w => w.LineItems)
                .FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId);

            return Ok(updatedWithdrawal);
        }

        /// <summary>
        /// Submit a withdrawal for processing
        /// </summary>
        [HttpPost("withdrawals/{withdrawalId}/submit")]
        public async Task<ActionResult> SubmitWithdrawal(int withdrawalId)
        {
            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            var withdrawal = await _context.BuildingDepotWithdrawals
                .FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId && 
                                         (!tenantId.HasValue || w.TenantId == tenantId));

            if (withdrawal == null)
            {
                return NotFound(new { message = "Withdrawal not found." });
            }

            if (withdrawal.Status != "Draft")
            {
                return BadRequest(new { message = "Withdrawal is already submitted." });
            }

            withdrawal.Status = "Submitted";
            withdrawal.SubmittedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Withdrawal submitted successfully." });
        }

        /// <summary>
        /// Delete a withdrawal (only drafts)
        /// </summary>
        [HttpDelete("withdrawals/{withdrawalId}")]
        public async Task<ActionResult> DeleteWithdrawal(int withdrawalId)
        {
            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            var withdrawal = await _context.BuildingDepotWithdrawals
                .Include(w => w.LineItems)
                .FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId && 
                                         (!tenantId.HasValue || w.TenantId == tenantId));

            if (withdrawal == null)
            {
                return NotFound(new { message = "Withdrawal not found." });
            }

            if (withdrawal.Status != "Draft")
            {
                return BadRequest(new { message = "Can only delete draft withdrawals." });
            }

            // Restore loan AmountDrawn
            var loan = await _context.Loans.FindAsync(withdrawal.LoanId);
            if (loan != null)
            {
                loan.AmountDrawn = (loan.AmountDrawn ?? 0) - withdrawal.TotalAmount;
            }

            _context.BuildingDepotWithdrawals.Remove(withdrawal);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Withdrawal deleted successfully." });
        }

        /// <summary>
        /// Upload receipt for a line item
        /// </summary>
        [HttpPost("withdrawals/{withdrawalId}/line-items/{lineItemId}/receipt")]
        public async Task<ActionResult> UploadReceipt(int withdrawalId, int lineItemId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided." });
            }

            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            var lineItem = await _context.BuildingDepotWithdrawalLineItems
                .Include(li => li.Withdrawal)
                .FirstOrDefaultAsync(li => li.WithdrawalId == withdrawalId && 
                                          li.LineItemId == lineItemId &&
                                          (!tenantId.HasValue || li.Withdrawal!.TenantId == tenantId));

            if (lineItem == null)
            {
                return NotFound(new { message = "Line item not found." });
            }

            // Save file
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads", "BuildingDepot", "Receipts");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{withdrawalId}_{lineItemId}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            lineItem.ReceiptFileName = file.FileName;
            lineItem.ReceiptFilePath = filePath;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Receipt uploaded successfully.", fileName = file.FileName });
        }

        /// <summary>
        /// Upload declaration form for a withdrawal
        /// </summary>
        [HttpPost("withdrawals/{withdrawalId}/declaration")]
        public async Task<ActionResult> UploadDeclaration(int withdrawalId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file provided." });
            }

            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            var withdrawal = await _context.BuildingDepotWithdrawals
                .FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId && 
                                         (!tenantId.HasValue || w.TenantId == tenantId));

            if (withdrawal == null)
            {
                return NotFound(new { message = "Withdrawal not found." });
            }

            // Save file
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads", "BuildingDepot", "Declarations");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{withdrawalId}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            withdrawal.DeclarationFileName = file.FileName;
            withdrawal.DeclarationFilePath = filePath;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Declaration uploaded successfully.", fileName = file.FileName });
        }

        /// <summary>
        /// Download receipt for a line item
        /// </summary>
        [HttpGet("withdrawals/{withdrawalId}/line-items/{lineItemId}/receipt")]
        public async Task<ActionResult> DownloadReceipt(int withdrawalId, int lineItemId)
        {
            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            var lineItem = await _context.BuildingDepotWithdrawalLineItems
                .Include(li => li.Withdrawal)
                .FirstOrDefaultAsync(li => li.WithdrawalId == withdrawalId && 
                                          li.LineItemId == lineItemId &&
                                          (!tenantId.HasValue || li.Withdrawal!.TenantId == tenantId));

            if (lineItem == null || string.IsNullOrEmpty(lineItem.ReceiptFilePath))
            {
                return NotFound(new { message = "Receipt not found." });
            }

            if (!System.IO.File.Exists(lineItem.ReceiptFilePath))
            {
                return NotFound(new { message = "File not found on server." });
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(lineItem.ReceiptFilePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var contentType = "application/octet-stream";
            return File(memory, contentType, lineItem.ReceiptFileName);
        }

        /// <summary>
        /// Download declaration for a withdrawal
        /// </summary>
        [HttpGet("withdrawals/{withdrawalId}/declaration")]
        public async Task<ActionResult> DownloadDeclaration(int withdrawalId)
        {
            var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as int?;

            var withdrawal = await _context.BuildingDepotWithdrawals
                .FirstOrDefaultAsync(w => w.WithdrawalId == withdrawalId && 
                                         (!tenantId.HasValue || w.TenantId == tenantId));

            if (withdrawal == null || string.IsNullOrEmpty(withdrawal.DeclarationFilePath))
            {
                return NotFound(new { message = "Declaration not found." });
            }

            if (!System.IO.File.Exists(withdrawal.DeclarationFilePath))
            {
                return NotFound(new { message = "File not found on server." });
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(withdrawal.DeclarationFilePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var contentType = "application/pdf";
            return File(memory, contentType, withdrawal.DeclarationFileName);
        }
    }
}
