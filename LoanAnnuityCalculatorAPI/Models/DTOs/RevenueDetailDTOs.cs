using System.ComponentModel.DataAnnotations;

namespace LoanAnnuityCalculatorAPI.Models.DTOs
{
    public class CreateRevenueDetailRequest
    {
        [Required]
        public string RevenueCategory { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be greater than or equal to 0")]
        public decimal Amount { get; set; }
        
        public bool IsRecurring { get; set; } = true;
        
        public string? Notes { get; set; }
    }

    public class UpdateRevenueDetailRequest
    {
        [Required]
        public string RevenueCategory { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be greater than or equal to 0")]
        public decimal Amount { get; set; }
        
        public bool IsRecurring { get; set; } = true;
        
        public string? Notes { get; set; }
    }
}