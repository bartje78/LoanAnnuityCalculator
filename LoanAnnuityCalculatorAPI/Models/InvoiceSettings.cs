using System.ComponentModel.DataAnnotations;

namespace LoanAnnuityCalculatorAPI.Models
{
    public class InvoiceSettings
    {
        [Key]
        public int Id { get; set; }
        
        public int DefaultPaymentTerms { get; set; } = 30;
        
        [MaxLength(10)]
        public string InvoicePrefix { get; set; } = "INV";
        
        public int InvoiceNumberStart { get; set; } = 1000;
        
        [MaxLength(20)]
        public string DueDateCalculation { get; set; } = "automatic"; // "automatic" or "manual"
        
        public string ReminderDays { get; set; } = "[7,14,30]"; // JSON array as string
        
        [MaxLength(5)]
        public string DefaultCurrency { get; set; } = "EUR";
        
        public decimal TaxRate { get; set; } = 21.0m;
        
        public bool IncludeCompanyLogo { get; set; } = false;
        
        [MaxLength(500)]
        public string FooterText { get; set; } = "Bedankt voor uw vertrouwen in onze dienstverlening.";
        
        public int InvoiceDay { get; set; } = 1; // Day of the month when invoices are sent (1-31)
        
        // Bank Details
        [MaxLength(100)]
        public string BankName { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string BIC { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string AccountHolder { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}