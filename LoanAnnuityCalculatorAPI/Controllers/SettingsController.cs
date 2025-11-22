using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using LoanAnnuityCalculatorAPI.Models.DTOs;
using System.Text.Json;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly LoanDbContext _context;

        public SettingsController(LoanDbContext context)
        {
            _context = context;
        }

        // GET: api/settings/company
        [HttpGet("company")]
        public async Task<ActionResult<CompanySettingsDto>> GetCompanySettings()
        {
            var settings = await _context.CompanySettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                // Return default settings if none exist
                return Ok(new CompanySettingsDto
                {
                    CompanyName = "Uw Bedrijfsnaam",
                    FundName = "Hoofdfonds",
                    ContactEmail = "info@uwbedrijf.nl",
                    PhoneNumber = "+31 20 123 4567",
                    Address = "Voorbeeldstraat 123",
                    City = "Amsterdam",
                    PostalCode = "1234 AB",
                    Country = "Nederland"
                });
            }

            return Ok(new CompanySettingsDto
            {
                CompanyName = settings.CompanyName,
                FundName = settings.FundName,
                ContactEmail = settings.ContactEmail,
                PhoneNumber = settings.PhoneNumber,
                Address = settings.Address,
                City = settings.City,
                PostalCode = settings.PostalCode,
                Country = settings.Country
            });
        }

        // PUT: api/settings/company
        [HttpPut("company")]
        public async Task<IActionResult> UpdateCompanySettings(CompanySettingsDto dto)
        {
            var settings = await _context.CompanySettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                // Create new settings record
                settings = new CompanySettings();
                _context.CompanySettings.Add(settings);
            }

            // Update fields
            settings.CompanyName = dto.CompanyName;
            settings.FundName = dto.FundName;
            settings.ContactEmail = dto.ContactEmail;
            settings.PhoneNumber = dto.PhoneNumber;
            settings.Address = dto.Address;
            settings.City = dto.City;
            settings.PostalCode = dto.PostalCode;
            settings.Country = dto.Country;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Company settings updated successfully" });
        }

        // GET: api/settings/invoice
        [HttpGet("invoice")]
        public async Task<ActionResult<InvoiceSettingsDto>> GetInvoiceSettings()
        {
            var settings = await _context.InvoiceSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                // Return default settings if none exist
                return Ok(new InvoiceSettingsDto
                {
                    DefaultPaymentTerms = 30,
                    InvoicePrefix = "INV",
                    InvoiceNumberStart = 1000,
                    DueDateCalculation = "automatic",
                    ReminderDays = new List<int> { 7, 14, 30 },
                    DefaultCurrency = "EUR",
                    TaxRate = 21.0m,
                    IncludeCompanyLogo = false,
                    FooterText = "Bedankt voor uw vertrouwen in onze dienstverlening.",
                    InvoiceDay = 1,
                    BankDetails = new BankDetailsDto
                    {
                        BankName = "ING Bank",
                        AccountNumber = "NL91 INGB 0002 4425 95",
                        BIC = "INGBNL2A",
                        AccountHolder = "Uw Bedrijfsnaam B.V."
                    }
                });
            }

            // Parse reminder days from JSON string
            var reminderDays = new List<int> { 7, 14, 30 };
            try
            {
                reminderDays = JsonSerializer.Deserialize<List<int>>(settings.ReminderDays) ?? reminderDays;
            }
            catch
            {
                // Use default if parsing fails
            }

            return Ok(new InvoiceSettingsDto
            {
                DefaultPaymentTerms = settings.DefaultPaymentTerms,
                InvoicePrefix = settings.InvoicePrefix,
                InvoiceNumberStart = settings.InvoiceNumberStart,
                DueDateCalculation = settings.DueDateCalculation,
                ReminderDays = reminderDays,
                DefaultCurrency = settings.DefaultCurrency,
                TaxRate = settings.TaxRate,
                IncludeCompanyLogo = settings.IncludeCompanyLogo,
                FooterText = settings.FooterText,
                InvoiceDay = settings.InvoiceDay,
                BankDetails = new BankDetailsDto
                {
                    BankName = settings.BankName,
                    AccountNumber = settings.AccountNumber,
                    BIC = settings.BIC,
                    AccountHolder = settings.AccountHolder
                }
            });
        }

        // PUT: api/settings/invoice
        [HttpPut("invoice")]
        public async Task<IActionResult> UpdateInvoiceSettings(InvoiceSettingsDto dto)
        {
            var settings = await _context.InvoiceSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                // Create new settings record
                settings = new InvoiceSettings();
                _context.InvoiceSettings.Add(settings);
            }

            // Update fields
            settings.DefaultPaymentTerms = dto.DefaultPaymentTerms;
            settings.InvoicePrefix = dto.InvoicePrefix;
            settings.InvoiceNumberStart = dto.InvoiceNumberStart;
            settings.DueDateCalculation = dto.DueDateCalculation;
            settings.ReminderDays = JsonSerializer.Serialize(dto.ReminderDays);
            settings.DefaultCurrency = dto.DefaultCurrency;
            settings.TaxRate = dto.TaxRate;
            settings.IncludeCompanyLogo = dto.IncludeCompanyLogo;
            settings.FooterText = dto.FooterText;
            settings.InvoiceDay = dto.InvoiceDay;
            settings.BankName = dto.BankDetails.BankName;
            settings.AccountNumber = dto.BankDetails.AccountNumber;
            settings.BIC = dto.BankDetails.BIC;
            settings.AccountHolder = dto.BankDetails.AccountHolder;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Invoice settings updated successfully" });
        }
    }
}