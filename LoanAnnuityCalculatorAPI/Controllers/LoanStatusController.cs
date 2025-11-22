using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Models.DTOs;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoanStatusController : ControllerBase
    {
        private readonly LoanDbContext _context;

        public LoanStatusController(LoanDbContext context)
        {
            _context = context;
        }

        // GET: api/loanstatus
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoanStatusDto>>> GetLoanStatuses()
        {
            var statuses = await _context.LoanStatuses
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.StatusName)
                .ToListAsync();

            var statusDtos = statuses.Select(s => new LoanStatusDto
            {
                Id = s.Id,
                StatusName = s.StatusName,
                Description = s.Description,
                IsActive = s.IsActive,
                IsDefault = s.IsDefault,
                SortOrder = s.SortOrder,
                IsCalculated = s.IsCalculated,
                CalculationType = s.CalculationType
            });

            return Ok(statusDtos);
        }

        // GET: api/loanstatus/all (includes inactive)
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<LoanStatusDto>>> GetAllLoanStatuses()
        {
            var statuses = await _context.LoanStatuses
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.StatusName)
                .ToListAsync();

            var statusDtos = statuses.Select(s => new LoanStatusDto
            {
                Id = s.Id,
                StatusName = s.StatusName,
                Description = s.Description,
                IsActive = s.IsActive,
                IsDefault = s.IsDefault,
                SortOrder = s.SortOrder,
                IsCalculated = s.IsCalculated,
                CalculationType = s.CalculationType
            });

            return Ok(statusDtos);
        }

        // GET: api/loanstatus/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<LoanStatusDto>> GetLoanStatus(int id)
        {
            var status = await _context.LoanStatuses.FindAsync(id);

            if (status == null)
            {
                return NotFound();
            }

            return Ok(new LoanStatusDto
            {
                Id = status.Id,
                StatusName = status.StatusName,
                Description = status.Description,
                IsActive = status.IsActive,
                IsDefault = status.IsDefault,
                SortOrder = status.SortOrder,
                IsCalculated = status.IsCalculated,
                CalculationType = status.CalculationType
            });
        }

        // POST: api/loanstatus
        [HttpPost]
        public async Task<ActionResult<LoanStatusDto>> CreateLoanStatus(CreateLoanStatusDto createDto)
        {
            // Check if status name already exists
            var existingStatus = await _context.LoanStatuses
                .FirstOrDefaultAsync(s => s.StatusName.ToLower() == createDto.StatusName.ToLower());

            if (existingStatus != null)
            {
                return BadRequest(new { message = "Een status met deze naam bestaat al." });
            }

            // If this is set as default, remove default from other statuses
            if (createDto.IsDefault)
            {
                var currentDefault = await _context.LoanStatuses
                    .FirstOrDefaultAsync(s => s.IsDefault);
                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                }
            }

            var status = new LoanStatus
            {
                StatusName = createDto.StatusName,
                Description = createDto.Description,
                IsActive = createDto.IsActive,
                IsDefault = createDto.IsDefault,
                SortOrder = createDto.SortOrder,
                IsCalculated = createDto.IsCalculated,
                CalculationType = createDto.CalculationType,
                CreatedAt = DateTime.UtcNow
            };

            _context.LoanStatuses.Add(status);
            await _context.SaveChangesAsync();

            var responseDto = new LoanStatusDto
            {
                Id = status.Id,
                StatusName = status.StatusName,
                Description = status.Description,
                IsActive = status.IsActive,
                IsDefault = status.IsDefault,
                SortOrder = status.SortOrder,
                IsCalculated = status.IsCalculated,
                CalculationType = status.CalculationType
            };

            return CreatedAtAction(nameof(GetLoanStatus), new { id = status.Id }, responseDto);
        }

        // PUT: api/loanstatus/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLoanStatus(int id, UpdateLoanStatusDto updateDto)
        {
            var status = await _context.LoanStatuses.FindAsync(id);

            if (status == null)
            {
                return NotFound();
            }

            // Check if status name already exists (except for current status)
            var existingStatus = await _context.LoanStatuses
                .FirstOrDefaultAsync(s => s.StatusName.ToLower() == updateDto.StatusName.ToLower() && s.Id != id);

            if (existingStatus != null)
            {
                return BadRequest(new { message = "Een status met deze naam bestaat al." });
            }

            // If this is set as default, remove default from other statuses
            if (updateDto.IsDefault && !status.IsDefault)
            {
                var currentDefault = await _context.LoanStatuses
                    .FirstOrDefaultAsync(s => s.IsDefault && s.Id != id);
                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                }
            }

            status.StatusName = updateDto.StatusName;
            status.Description = updateDto.Description;
            status.IsActive = updateDto.IsActive;
            status.IsDefault = updateDto.IsDefault;
            status.SortOrder = updateDto.SortOrder;
            status.IsCalculated = updateDto.IsCalculated;
            status.CalculationType = updateDto.CalculationType;
            status.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LoanStatusExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/loanstatus/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLoanStatus(int id)
        {
            var status = await _context.LoanStatuses.FindAsync(id);
            if (status == null)
            {
                return NotFound();
            }

            // Check if this status is being used by any loans
            var loansUsingStatus = await _context.Loans
                .AnyAsync(l => l.Status == status.StatusName);

            if (loansUsingStatus)
            {
                return BadRequest(new { message = "Deze status kan niet worden verwijderd omdat deze gebruikt wordt door bestaande leningen." });
            }

            _context.LoanStatuses.Remove(status);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/loanstatus/seed
        [HttpPost("seed")]
        public async Task<IActionResult> SeedDefaultStatuses()
        {
            // Check if any statuses exist
            var existingCount = await _context.LoanStatuses.CountAsync();
            if (existingCount > 0)
            {
                return BadRequest(new { message = "Er bestaan al loan statussen in de database." });
            }

            var defaultStatuses = new List<LoanStatus>
            {
                new LoanStatus
                {
                    StatusName = "Active",
                    Description = "Actieve lening met lopende betalingen",
                    IsActive = true,
                    IsDefault = false,
                    SortOrder = 1,
                    IsCalculated = true,
                    CalculationType = "ActiveTenor",
                    CreatedAt = DateTime.UtcNow
                },
                new LoanStatus
                {
                    StatusName = "Aktief",
                    Description = "Actieve lening met lopende betalingen (Nederlands)",
                    IsActive = true,
                    IsDefault = true,
                    SortOrder = 2,
                    IsCalculated = true,
                    CalculationType = "ActiveTenor",
                    CreatedAt = DateTime.UtcNow
                },
                new LoanStatus
                {
                    StatusName = "Afgelost",
                    Description = "Volledig afgeloste lening",
                    IsActive = true,
                    IsDefault = false,
                    SortOrder = 3,
                    IsCalculated = true,
                    CalculationType = "Completed",
                    CreatedAt = DateTime.UtcNow
                },
                new LoanStatus
                {
                    StatusName = "In Gebreke",
                    Description = "Lening met betalingsachterstanden",
                    IsActive = true,
                    IsDefault = false,
                    SortOrder = 4,
                    IsCalculated = false,
                    CalculationType = "Manual",
                    CreatedAt = DateTime.UtcNow
                },
                new LoanStatus
                {
                    StatusName = "Opgezegd",
                    Description = "Lening is opgezegd",
                    IsActive = true,
                    IsDefault = false,
                    SortOrder = 5,
                    IsCalculated = false,
                    CalculationType = "Manual",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.LoanStatuses.AddRange(defaultStatuses);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Standaard loan statussen zijn succesvol aangemaakt.", count = defaultStatuses.Count });
        }

        private bool LoanStatusExists(int id)
        {
            return _context.LoanStatuses.Any(e => e.Id == id);
        }
    }
}