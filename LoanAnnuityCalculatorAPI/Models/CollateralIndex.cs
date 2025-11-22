using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models
{
    public class CollateralIndex
    {
        [Key]
        public int CollateralIndexId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CollateralType { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Quarter { get; set; } = string.Empty; // Format: "2015Q3"

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal PriceIndex { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        // Computed property to parse quarter into a sortable date
        [NotMapped]
        public DateTime QuarterDate
        {
            get
            {
                if (string.IsNullOrEmpty(Quarter) || Quarter.Length < 6)
                    return DateTime.MinValue;

                try
                {
                    var yearPart = Quarter.Substring(0, 4);
                    var quarterPart = Quarter.Substring(5, 1);

                    if (int.TryParse(yearPart, out int year) && int.TryParse(quarterPart, out int quarter))
                    {
                        // Convert quarter to month (Q1=Jan, Q2=Apr, Q3=Jul, Q4=Oct)
                        int month = (quarter - 1) * 3 + 1;
                        return new DateTime(year, month, 1);
                    }
                }
                catch
                {
                    // Invalid format
                }

                return DateTime.MinValue;
            }
        }
    }
}