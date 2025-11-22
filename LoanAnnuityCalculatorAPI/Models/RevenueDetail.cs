using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanAnnuityCalculatorAPI.Models.Debtor
{
    public class RevenueDetail
    {
        [Key]
        public int RevenueDetailId { get; set; }

        [ForeignKey("DebtorPL")]
        public int PLId { get; set; }

        public string RevenueCategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsRecurring { get; set; } = true;
        public string? Notes { get; set; }

        public required DebtorPL DebtorPL { get; set; }
    }
}