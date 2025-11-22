using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models.Loan; // For Loan model

namespace LoanAnnuityCalculatorAPI.Services
{
    public class LoanService
    {
        private readonly LoanDbContext _dbContext;

        public LoanService(LoanDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Loan? GetLoanById(int loanId)
        {
            // Handle the possibility of null
            return _dbContext.Loans.Find(loanId);
        }

        public IEnumerable<Loan> GetAllLoans()
        {
            return _dbContext.Loans.ToList();
        }

        public void AddLoan(Loan loan)
        {
            _dbContext.Loans.Add(loan);
            _dbContext.SaveChanges();
        }
    }
}