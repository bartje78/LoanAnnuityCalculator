using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models.Debtor
{
    public class DebtorSignatory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DebtorID { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Function { get; set; } = string.Empty;

        [Required]
        public int SortOrder { get; set; }

        // Navigation property
        [ForeignKey("DebtorID")]
        public DebtorDetails? Debtor { get; set; }
    }
}
