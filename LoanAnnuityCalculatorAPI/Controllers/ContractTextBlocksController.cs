using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RiskManagerOrAdmin")] // Require RiskManager or Admin for contract settings
    public class ContractTextBlocksController : ControllerBase
    {
        private readonly LoanDbContext _context;
        private readonly ILogger<ContractTextBlocksController> _logger;

        public ContractTextBlocksController(LoanDbContext context, ILogger<ContractTextBlocksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ContractTextBlocks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContractTextBlock>>> GetContractTextBlocks(
            [FromQuery] string? section = null,
            [FromQuery] bool activeOnly = false)
        {
            try
            {
                var query = _context.ContractTextBlocks.AsQueryable();

                if (!string.IsNullOrEmpty(section))
                {
                    query = query.Where(b => b.Section == section);
                }

                if (activeOnly)
                {
                    query = query.Where(b => b.IsActive);
                }

                var blocks = await query
                    .OrderBy(b => b.Section)
                    .ThenBy(b => b.SortOrder)
                    .ThenBy(b => b.Name)
                    .ToListAsync();

                return Ok(blocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contract text blocks");
                return StatusCode(500, "Error fetching contract text blocks");
            }
        }

        // GET: api/ContractTextBlocks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ContractTextBlock>> GetContractTextBlock(int id)
        {
            try
            {
                var block = await _context.ContractTextBlocks.FindAsync(id);

                if (block == null)
                {
                    return NotFound();
                }

                return Ok(block);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contract text block {Id}", id);
                return StatusCode(500, "Error fetching contract text block");
            }
        }

        // POST: api/ContractTextBlocks
        [HttpPost]
        public async Task<ActionResult<ContractTextBlock>> CreateContractTextBlock(ContractTextBlock block)
        {
            try
            {
                block.CreatedAt = DateTime.UtcNow;
                _context.ContractTextBlocks.Add(block);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetContractTextBlock), new { id = block.Id }, block);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract text block");
                return StatusCode(500, "Error creating contract text block");
            }
        }

        // PUT: api/ContractTextBlocks/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContractTextBlock(int id, ContractTextBlock block)
        {
            if (id != block.Id)
            {
                return BadRequest();
            }

            try
            {
                var existing = await _context.ContractTextBlocks.FindAsync(id);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.Name = block.Name;
                existing.Section = block.Section;
                existing.Content = block.Content;
                existing.SortOrder = block.SortOrder;
                existing.IsActive = block.IsActive;
                // Persist the two insertion flags so checkbox state is saved
                existing.InsertPaymentChart = block.InsertPaymentChart;
                existing.InsertBseTable = block.InsertBseTable;
                existing.ShowSectionHeader = block.ShowSectionHeader;
                existing.RedemptionScheduleType = block.RedemptionScheduleType;
                existing.SecurityType = block.SecurityType;
                existing.Description = block.Description;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contract text block {Id}", id);
                return StatusCode(500, "Error updating contract text block");
            }
        }

        // DELETE: api/ContractTextBlocks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContractTextBlock(int id)
        {
            try
            {
                var block = await _context.ContractTextBlocks.FindAsync(id);
                if (block == null)
                {
                    return NotFound();
                }

                _context.ContractTextBlocks.Remove(block);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract text block {Id}", id);
                return StatusCode(500, "Error deleting contract text block");
            }
        }

        // GET: api/ContractTextBlocks/sections
        [HttpGet("sections")]
        public async Task<ActionResult<IEnumerable<string>>> GetSections()
        {
            try
            {
                var sections = await _context.ContractTextBlocks
                    .Where(b => b.IsActive)
                    .Select(b => b.Section)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sections");
                return StatusCode(500, "Error fetching sections");
            }
        }
    }
}
