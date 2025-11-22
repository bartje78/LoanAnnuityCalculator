using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace LoanAnnuityCalculatorAPI.Models
{
    public class Collateral
    {
        [Key]
        public int CollateralId { get; set; }

        [Required]
        [StringLength(100)]
        public string CollateralType { get; set; } = string.Empty; // e.g., "1st Mortgage", "2nd Mortgage", "Guarantee", "Unsecured"

        [StringLength(500)]
        public string? Description { get; set; } // e.g., address of mortgaged property

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AppraisalValue { get; set; } // Latest appraisal value

        public DateTime? AppraisalDate { get; set; } // Latest appraisal date

        [StringLength(100)]
        public string? PropertyType { get; set; } // e.g., "Residential", "Commercial", "Industrial", "Land"

        // === UNIQUE ASSET IDENTIFICATION FIELDS ===
        [StringLength(200)]
        public string? PropertyAddress { get; set; } // Full address for display purposes

        [StringLength(50)]
        public string? LandRegistryCode { get; set; } // Official land registry/cadastral number (kadastrale aanduiding) - for plots of land

        [StringLength(10)]
        public string? PostalCode { get; set; } // Dutch postal code (e.g., "1234AB") - for properties

        [StringLength(20)]
        public string? HouseNumber { get; set; } // House number including additions (e.g., "123", "123A", "123-1") - for properties

        [StringLength(100)]
        public string? AssetUniqueId { get; set; } // Generic unique identifier (VIN, serial number, etc.) - for non-real estate assets

        // === SECURITY TYPE AND SUBORDINATION FIELDS ===
        [StringLength(50)]
        public string? SecurityType { get; set; } // e.g., "1st Mortgage", "2nd Mortgage", "General Security", "Guarantee"

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FirstMortgageAmount { get; set; } // Amount of first mortgage held by third party (for 2nd mortgages)

        // === LIQUIDITY HAIRCUT ===
        [Column(TypeName = "decimal(5,2)")]
        public decimal LiquidityHaircut { get; set; } = 0.00M; // Percentage discount (0-100) on indexed value based on collateral liquidity

        // === AUDIT FIELDS ===
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The most recent appraisal value update date - used for validation
        /// </summary>
        public DateTime? LastAppraisalUpdate { get; set; }

        // Navigation properties - Many-to-Many relationship with Loans
        public virtual ICollection<LoanCollateral> LoanCollaterals { get; set; } = new List<LoanCollateral>();
    }
}