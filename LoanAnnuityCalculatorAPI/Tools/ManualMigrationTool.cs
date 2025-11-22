using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Services;
using Microsoft.Extensions.Logging;

namespace LoanAnnuityCalculatorAPI.Tools
{
    /// <summary>
    /// Manual migration tool - run this to populate balance sheet line items
    /// Usage: dotnet run --project LoanAnnuityCalculatorAPI migrate-debtor <debtorId>
    /// </summary>
    public class ManualMigrationTool
    {
        public static async Task Main(string[] args)
        {
            if (args.Length == 0 || args[0] != "migrate-debtor")
            {
                Console.WriteLine("Usage: dotnet run migrate-debtor <debtorId>");
                Console.WriteLine("   or: dotnet run migrate-all");
                return;
            }

            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "loans.db");
            var connectionString = $"Data Source={dbPath}";
            
            var optionsBuilder = new DbContextOptionsBuilder<LoanDbContext>();
            optionsBuilder.UseSqlite(connectionString);
            
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var migrationLogger = loggerFactory.CreateLogger<BalanceSheetMigrationService>();
            
            using var context = new LoanDbContext(optionsBuilder.Options);
            var loanCalculator = new LoanFinancialCalculatorService();
            var migrationService = new BalanceSheetMigrationService(context, migrationLogger, loanCalculator);

            if (args[0] == "migrate-all")
            {
                Console.WriteLine("Migrating ALL balance sheets...");
                var count = await migrationService.MigrateAllBalanceSheets();
                Console.WriteLine($"✅ Migrated {count} balance sheets");
            }
            else if (args.Length >= 2 && int.TryParse(args[1], out int debtorId))
            {
                Console.WriteLine($"Migrating balance sheets for debtor {debtorId}...");
                var count = await migrationService.MigrateDebtorBalanceSheets(debtorId);
                Console.WriteLine($"✅ Migrated {count} balance sheets for debtor {debtorId}");
            }
            else
            {
                Console.WriteLine("Invalid debtor ID");
            }
        }
    }
}
