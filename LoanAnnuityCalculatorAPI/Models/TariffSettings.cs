using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models
{
    public class TariffSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<LtvSpreadTier> LtvTiers { get; set; } = new List<LtvSpreadTier>();
        public ICollection<CreditRatingSpread> CreditRatings { get; set; } = new List<CreditRatingSpread>();
        public ICollection<ImpactDiscount> ImpactDiscounts { get; set; } = new List<ImpactDiscount>();
    }

    public class LtvSpreadTier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TariffSettingsId { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal MaxLtv { get; set; }

        [Required]
        [Range(0, 10000)]
        public decimal Spread { get; set; } // in basis points

        public int SortOrder { get; set; }

        // Navigation property
        [ForeignKey("TariffSettingsId")]
        public TariffSettings TariffSettings { get; set; } = null!;
    }

    public class CreditRatingSpread
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TariffSettingsId { get; set; }

        [Required]
        [StringLength(10)]
        public string Rating { get; set; } = string.Empty;

        [Required]
        [Range(0, 10000)]
        public decimal Spread { get; set; } // in basis points

        public int SortOrder { get; set; }

        // Navigation property
        [ForeignKey("TariffSettingsId")]
        public TariffSettings TariffSettings { get; set; } = null!;
    }

    public class ImpactDiscount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TariffSettingsId { get; set; }

        [Required]
        [StringLength(20)]
        public string Level { get; set; } = string.Empty; // very-high, high, medium, low, very-low

        [Required]
        [Range(0, 100)]
        public decimal Discount { get; set; } // Percentage discount (e.g., 50 = 50% off base rate)

        public int SortOrder { get; set; }

        // Navigation property
        [ForeignKey("TariffSettingsId")]
        public TariffSettings TariffSettings { get; set; } = null!;
    }
}
