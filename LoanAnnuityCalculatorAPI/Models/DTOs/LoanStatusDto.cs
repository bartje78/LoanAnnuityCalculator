namespace LoanAnnuityCalculatorAPI.Models.DTOs
{
    public class LoanStatusDto
    {
        public int Id { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public bool IsCalculated { get; set; } = false;
        public string? CalculationType { get; set; }
    }

    public class CreateLoanStatusDto
    {
        public string StatusName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public bool IsCalculated { get; set; } = false;
        public string? CalculationType { get; set; }
    }

    public class UpdateLoanStatusDto
    {
        public string StatusName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public bool IsCalculated { get; set; } = false;
        public string? CalculationType { get; set; }
    }
}