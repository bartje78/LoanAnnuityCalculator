namespace LoanAnnuityCalculatorAPI.Models.Ratios
{
    public class CreditRatingThreshold
    {
        public int Id { get; set; }
        public int DebtorID { get; set; } 
        public string RatioName { get; set; } = string.Empty; // e.g., "CurrentRatio"
        public string CreditRating { get; set; } = string.Empty; // e.g., "AAA"
        public decimal MinValue { get; set; } // Minimum value for the ratio
        public decimal MaxValue { get; set; } // Maximum value for the ratio
    }
}