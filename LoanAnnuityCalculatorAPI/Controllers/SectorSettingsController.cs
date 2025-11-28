using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models.Settings;
using LoanAnnuityCalculatorAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SectorSettingsController : ControllerBase
    {
        private readonly LoanDbContext _context;
        private readonly SectorCorrelationSeedService _seedService;
        
        public SectorSettingsController(LoanDbContext context, SectorCorrelationSeedService seedService)
        {
            _context = context;
            _seedService = seedService;
        }
        
        /// <summary>
        /// Initialize/seed default sectors and correlations for a model settings
        /// </summary>
        [HttpPost("{modelSettingsId}/initialize")]
        public async Task<IActionResult> InitializeSectors(int modelSettingsId)
        {
            try
            {
                await _seedService.SeedDefaultCorrelationsAsync(modelSettingsId);
                return Ok(new { message = "Sectoren en correlaties succesvol geïnitialiseerd" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        
        /// <summary>
        /// Get all sector definitions for a model settings
        /// </summary>
        [HttpGet("{modelSettingsId}/sectors")]
        public async Task<ActionResult<IEnumerable<SectorDefinition>>> GetSectors(int modelSettingsId)
        {
            var sectors = await _context.SectorDefinitions
                .Where(s => s.ModelSettingsId == modelSettingsId)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();
            
            return Ok(sectors);
        }
        
        /// <summary>
        /// Get active sectors only (for dropdowns)
        /// </summary>
        [HttpGet("{modelSettingsId}/sectors/active")]
        public async Task<ActionResult<IEnumerable<object>>> GetActiveSectors(int modelSettingsId)
        {
            var sectors = await _context.SectorDefinitions
                .Where(s => s.ModelSettingsId == modelSettingsId && s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new
                {
                    s.Id,
                    s.SectorCode,
                    s.DisplayName,
                    s.Description,
                    s.ColorCode,
                    s.DefaultVolatility
                })
                .ToListAsync();
            
            return Ok(sectors);
        }
        
        /// <summary>
        /// Update a sector definition
        /// </summary>
        [HttpPut("{modelSettingsId}/sectors/{sectorId}")]
        public async Task<IActionResult> UpdateSector(int modelSettingsId, int sectorId, [FromBody] SectorDefinitionUpdateDto dto)
        {
            var sector = await _context.SectorDefinitions
                .FirstOrDefaultAsync(s => s.Id == sectorId && s.ModelSettingsId == modelSettingsId);
            
            if (sector == null)
                return NotFound();
            
            sector.DisplayName = dto.DisplayName;
            sector.Description = dto.Description ?? string.Empty;
            sector.DefaultVolatility = dto.DefaultVolatility;
            sector.ExpectedGrowth = dto.ExpectedGrowth;
            sector.ColorCode = dto.ColorCode ?? sector.ColorCode;
            sector.IsActive = dto.IsActive;
            sector.DisplayOrder = dto.DisplayOrder;
            sector.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return Ok(sector);
        }
        
        /// <summary>
        /// Get sector-sector correlation matrix
        /// </summary>
        [HttpGet("{modelSettingsId}/correlations/sector-sector")]
        public async Task<ActionResult<object>> GetSectorCorrelations(int modelSettingsId)
        {
            var correlations = await _context.SectorCorrelations
                .Where(c => c.ModelSettingsId == modelSettingsId)
                .ToListAsync();
            
            var sectors = await _context.SectorDefinitions
                .Where(s => s.ModelSettingsId == modelSettingsId && s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();
            
            // Build correlation matrix
            var matrix = new Dictionary<string, Dictionary<string, decimal>>();
            foreach (var s1 in sectors)
            {
                matrix[s1.SectorCode] = new Dictionary<string, decimal>();
                foreach (var s2 in sectors)
                {
                    if (s1.SectorCode == s2.SectorCode)
                    {
                        matrix[s1.SectorCode][s2.SectorCode] = 1.0m;
                    }
                    else
                    {
                        var key1 = string.CompareOrdinal(s1.SectorCode, s2.SectorCode) < 0
                            ? (s1.SectorCode, s2.SectorCode)
                            : (s2.SectorCode, s1.SectorCode);
                        
                        var corr = correlations.FirstOrDefault(c =>
                            c.Sector1 == key1.Item1 && c.Sector2 == key1.Item2);
                        
                        matrix[s1.SectorCode][s2.SectorCode] = corr?.CorrelationCoefficient ?? 0.35m;
                    }
                }
            }
            
            return Ok(new { sectors, matrix });
        }
        
        /// <summary>
        /// Update sector-sector correlation
        /// </summary>
        [HttpPut("{modelSettingsId}/correlations/sector-sector")]
        public async Task<IActionResult> UpdateSectorCorrelation(int modelSettingsId, [FromBody] SectorCorrelationUpdateDto dto)
        {
            // Ensure alphabetical order
            var (sector1, sector2) = string.CompareOrdinal(dto.Sector1, dto.Sector2) < 0
                ? (dto.Sector1, dto.Sector2)
                : (dto.Sector2, dto.Sector1);
            
            var correlation = await _context.SectorCorrelations
                .FirstOrDefaultAsync(c => c.ModelSettingsId == modelSettingsId
                    && c.Sector1 == sector1
                    && c.Sector2 == sector2);
            
            if (correlation == null)
            {
                // Create new correlation
                correlation = new SectorCorrelation
                {
                    ModelSettingsId = modelSettingsId,
                    Sector1 = sector1,
                    Sector2 = sector2,
                    CorrelationCoefficient = dto.CorrelationCoefficient
                };
                _context.SectorCorrelations.Add(correlation);
            }
            else
            {
                correlation.CorrelationCoefficient = dto.CorrelationCoefficient;
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(correlation);
        }
        
        /// <summary>
        /// Get sector-collateral correlations
        /// </summary>
        [HttpGet("{modelSettingsId}/correlations/sector-collateral")]
        public async Task<ActionResult<IEnumerable<SectorCollateralCorrelation>>> GetSectorCollateralCorrelations(int modelSettingsId)
        {
            var correlations = await _context.SectorCollateralCorrelations
                .Where(c => c.ModelSettingsId == modelSettingsId)
                .ToListAsync();
            
            return Ok(correlations);
        }
        
        /// <summary>
        /// Update sector-collateral correlation
        /// </summary>
        [HttpPut("{modelSettingsId}/correlations/sector-collateral")]
        public async Task<IActionResult> UpdateSectorCollateralCorrelation(int modelSettingsId, [FromBody] SectorCollateralCorrelationUpdateDto dto)
        {
            var correlation = await _context.SectorCollateralCorrelations
                .FirstOrDefaultAsync(c => c.ModelSettingsId == modelSettingsId
                    && c.Sector == dto.Sector
                    && c.PropertyType == dto.PropertyType);
            
            if (correlation == null)
            {
                correlation = new SectorCollateralCorrelation
                {
                    ModelSettingsId = modelSettingsId,
                    Sector = dto.Sector,
                    PropertyType = dto.PropertyType,
                    CorrelationCoefficient = dto.CorrelationCoefficient
                };
                _context.SectorCollateralCorrelations.Add(correlation);
            }
            else
            {
                correlation.CorrelationCoefficient = dto.CorrelationCoefficient;
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(correlation);
        }
    }
    
    // DTOs
    public class SectorDefinitionUpdateDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DefaultVolatility { get; set; }
        public decimal ExpectedGrowth { get; set; }
        public string? ColorCode { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }
    
    public class SectorCorrelationUpdateDto
    {
        public string Sector1 { get; set; } = string.Empty;
        public string Sector2 { get; set; } = string.Empty;
        public decimal CorrelationCoefficient { get; set; }
    }
    
    public class SectorCollateralCorrelationUpdateDto
    {
        public string Sector { get; set; } = string.Empty;
        public string PropertyType { get; set; } = string.Empty;
        public decimal CorrelationCoefficient { get; set; }
    }
}
