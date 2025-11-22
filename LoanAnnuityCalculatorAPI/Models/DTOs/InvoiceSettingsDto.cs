namespace LoanAnnuityCalculatorAPI.Models.DTOs
{
    public class InvoiceSettingsDto
    {
        public int DefaultPaymentTerms { get; set; } = 30;
        public string InvoicePrefix { get; set; } = "INV";
        public int InvoiceNumberStart { get; set; } = 1000;
        public string DueDateCalculation { get; set; } = "automatic";
        public List<int> ReminderDays { get; set; } = new List<int> { 7, 14, 30 };
        public string DefaultCurrency { get; set; } = "EUR";
        public decimal TaxRate { get; set; } = 21.0m;
        public bool IncludeCompanyLogo { get; set; } = false;
        public string FooterText { get; set; } = "Bedankt voor uw vertrouwen in onze dienstverlening.";
        public int InvoiceDay { get; set; } = 1; // Day of the month when invoices are sent (1-31)
        public BankDetailsDto BankDetails { get; set; } = new BankDetailsDto();
    }

    public class BankDetailsDto
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BIC { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
    }
}