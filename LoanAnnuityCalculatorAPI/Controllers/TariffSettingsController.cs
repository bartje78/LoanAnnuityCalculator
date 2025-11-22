using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")] // Only admins can modify tariff settings
    public class TariffSettingsController : ControllerBase
    {
        private readonly LoanDbContext _context;
        private readonly ILogger<TariffSettingsController> _logger;

        public TariffSettingsController(LoanDbContext context, ILogger<TariffSettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/TariffSettings
        [HttpGet]
        public async Task<ActionResult<TariffSettingsDto>> GetTariffSettings()
        {
            try
            {
                // Get the active tariff settings
                var settings = await _context.TariffSettings
                    .Include(s => s.LtvTiers.OrderBy(t => t.SortOrder))
                    .Include(s => s.CreditRatings.OrderBy(r => r.SortOrder))
                    .FirstOrDefaultAsync(s => s.IsActive);

                if (settings == null)
                {
                    // Return default settings if none exist
                    return Ok(GetDefaultSettings());
                }

                var dto = new TariffSettingsDto
                {
                    Id = settings.Id,
                    LtvTiers = settings.LtvTiers.Select(t => new LtvSpreadTierDto
                    {
                        MaxLtv = t.MaxLtv,
                        Spread = t.Spread
                    }).ToList(),
                    CreditRatings = settings.CreditRatings.Select(r => new CreditRatingSpreadDto
                    {
                        Rating = r.Rating,
                        Spread = r.Spread
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tariff settings");
                return StatusCode(500, "Error retrieving tariff settings");
            }
        }

        // POST: api/TariffSettings
        [HttpPost]
        public async Task<ActionResult<TariffSettingsDto>> SaveTariffSettings([FromBody] TariffSettingsDto dto)
        {
            try
            {
                // Deactivate all existing settings
                var existingSettings = await _context.TariffSettings.ToListAsync();
                foreach (var setting in existingSettings)
                {
                    setting.IsActive = false;
                }

                // Create new settings
                var newSettings = new TariffSettings
                {
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Add LTV tiers
                for (int i = 0; i < dto.LtvTiers.Count; i++)
                {
                    newSettings.LtvTiers.Add(new LtvSpreadTier
                    {
                        MaxLtv = dto.LtvTiers[i].MaxLtv,
                        Spread = dto.LtvTiers[i].Spread,
                        SortOrder = i
                    });
                }

                // Add credit ratings
                for (int i = 0; i < dto.CreditRatings.Count; i++)
                {
                    newSettings.CreditRatings.Add(new CreditRatingSpread
                    {
                        Rating = dto.CreditRatings[i].Rating,
                        Spread = dto.CreditRatings[i].Spread,
                        SortOrder = i
                    });
                }

                _context.TariffSettings.Add(newSettings);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tariff settings saved successfully");

                dto.Id = newSettings.Id;
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving tariff settings");
                return StatusCode(500, "Error saving tariff settings");
            }
        }

        // GET: api/TariffSettings/default
        [HttpGet("default")]
        public ActionResult<TariffSettingsDto> GetDefaultTariffSettings()
        {
            return Ok(GetDefaultSettings());
        }

        private TariffSettingsDto GetDefaultSettings()
        {
            return new TariffSettingsDto
            {
                LtvTiers = new List<LtvSpreadTierDto>
                {
                    new LtvSpreadTierDto { MaxLtv = 50, Spread = 10 },
                    new LtvSpreadTierDto { MaxLtv = 60, Spread = 25 },
                    new LtvSpreadTierDto { MaxLtv = 70, Spread = 50 },
                    new LtvSpreadTierDto { MaxLtv = 80, Spread = 75 },
                    new LtvSpreadTierDto { MaxLtv = 90, Spread = 100 },
                    new LtvSpreadTierDto { MaxLtv = 100, Spread = 150 }
                },
                CreditRatings = new List<CreditRatingSpreadDto>
                {
                    new CreditRatingSpreadDto { Rating = "AAA", Spread = 10 },
                    new CreditRatingSpreadDto { Rating = "AA", Spread = 25 },
                    new CreditRatingSpreadDto { Rating = "A", Spread = 50 },
                    new CreditRatingSpreadDto { Rating = "BBB", Spread = 100 },
                    new CreditRatingSpreadDto { Rating = "BB", Spread = 200 },
                    new CreditRatingSpreadDto { Rating = "B", Spread = 350 },
                    new CreditRatingSpreadDto { Rating = "CCC", Spread = 500 }
                }
            };
        }
    }

    // DTOs
    public class TariffSettingsDto
    {
        public int Id { get; set; }
        public List<LtvSpreadTierDto> LtvTiers { get; set; } = new();
        public List<CreditRatingSpreadDto> CreditRatings { get; set; } = new();
    }

    public class LtvSpreadTierDto
    {
        public decimal MaxLtv { get; set; }
        public decimal Spread { get; set; }
    }

    public class CreditRatingSpreadDto
    {
        public string Rating { get; set; } = string.Empty;
        public decimal Spread { get; set; }
    }
}
