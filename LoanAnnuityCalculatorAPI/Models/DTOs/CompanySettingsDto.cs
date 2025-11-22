namespace LoanAnnuityCalculatorAPI.Models.DTOs
{
    public class CompanySettingsDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string FundName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = "Nederland";
    }
}