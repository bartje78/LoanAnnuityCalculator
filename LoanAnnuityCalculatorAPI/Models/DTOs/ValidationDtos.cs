using System.ComponentModel.DataAnnotations;

namespace LoanAnnuityCalculatorAPI.Models.DTOs
{
    /// <summary>
    /// Request DTO for creating/updating loans with validation
    /// </summary>
    public class LoanRequestDto
    {
        [Required(ErrorMessage = "Loan amount is required")]
        [Range(100, 100000000, ErrorMessage = "Loan amount must be between 100 and 100,000,000")]
        public decimal LoanAmount { get; set; }

        [Required(ErrorMessage = "Interest rate is required")]
        [Range(0.01, 50, ErrorMessage = "Interest rate must be between 0.01% and 50%")]
        public decimal AnnualInterestRate { get; set; }

        [Required(ErrorMessage = "Tenor is required")]
        [Range(1, 600, ErrorMessage = "Tenor must be between 1 and 600 months")]
        public int TenorMonths { get; set; }

        [Range(0, 600, ErrorMessage = "Interest-only months cannot exceed tenor")]
        public int InterestOnlyMonths { get; set; } = 0;

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Debtor ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid debtor ID")]
        public int DebtorID { get; set; }

        public int? FundId { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request DTO for creating/updating collateral with validation
    /// </summary>
    public class CollateralRequestDto
    {
        [Required(ErrorMessage = "Collateral type is required")]
        [MaxLength(100)]
        public string CollateralType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Appraised value is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Appraised value must be positive")]
        public decimal AppraisedValue { get; set; }

        [Required(ErrorMessage = "Appraisal date is required")]
        public DateTime AppraisalDate { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(20)]
        [RegularExpression(@"^\d{4}\s?[A-Z]{2}$", ErrorMessage = "Invalid Dutch postal code format")]
        public string? PostalCode { get; set; }

        [MaxLength(50)]
        public string? LandRegistryCode { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Market value must be positive")]
        public decimal? MarketValue { get; set; }

        public int? FundId { get; set; }
    }

    /// <summary>
    /// Request DTO for creating debtors with validation
    /// </summary>
    public class DebtorRequestDto
    {
        [Required(ErrorMessage = "Debtor name is required")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string DebtorName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ContactPerson { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(200)]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(50)]
        public string? TaxId { get; set; }

        public int? FundId { get; set; }
    }
}
