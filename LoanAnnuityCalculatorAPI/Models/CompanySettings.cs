using System.ComponentModel.DataAnnotations;

namespace LoanAnnuityCalculatorAPI.Models
{
    public class CompanySettings
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string FundName { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string ContactEmail { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Address { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Country { get; set; } = "Nederland";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}