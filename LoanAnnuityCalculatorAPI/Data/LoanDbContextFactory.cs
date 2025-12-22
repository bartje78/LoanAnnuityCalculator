using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace LoanAnnuityCalculatorAPI.Data
{
    public class LoanDbContextFactory : IDesignTimeDbContextFactory<LoanDbContext>
    {
        public LoanDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LoanDbContext>();
            
            // Build configuration to read connection string from appsettings.json or environment
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Production.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrEmpty(connectionString))
            {
                // Use SQL Server if connection string is provided
                optionsBuilder.UseSqlServer(connectionString);
            }
            else
            {
                // Fallback to SQLite for local development
                var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "loans.db");
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }

            return new LoanDbContext(optionsBuilder.Options);
        }
    }
}