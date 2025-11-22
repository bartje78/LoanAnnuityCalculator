using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace LoanAnnuityCalculatorAPI.Data
{
    public class LoanDbContextFactory : IDesignTimeDbContextFactory<LoanDbContext>
    {
        public LoanDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LoanDbContext>();
            // Use an absolute path so design-time tools (ef migrations) target the same file as runtime
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "loans.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}"); // Ensure this matches the connection string in Program.cs

            return new LoanDbContext(optionsBuilder.Options);
        }
    }
}