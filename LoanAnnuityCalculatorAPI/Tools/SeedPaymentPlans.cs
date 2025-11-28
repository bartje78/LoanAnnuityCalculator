using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LoanAnnuityCalculatorAPI.Tools
{
    /// <summary>
    /// Simple tool to seed payment plans without authentication
    /// Run: dotnet run --project Tools/SeedPaymentPlans.cs
    /// </summary>
    public class SeedPaymentPlans
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Payment Plans Seeder ===");
            
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Setup DbContext
            var optionsBuilder = new DbContextOptionsBuilder<LoanDbContext>();
            optionsBuilder.UseSqlite(configuration.GetConnectionString("DefaultConnection"));

            using var dbContext = new LoanDbContext(optionsBuilder.Options);

            // Check if plans already exist
            var existingCount = await dbContext.PaymentPlans.CountAsync();
            if (existingCount > 0)
            {
                Console.WriteLine($"Payment plans already exist ({existingCount} plans found)");
                Console.WriteLine("Delete existing plans? (y/n)");
                var response = Console.ReadLine();
                if (response?.ToLower() != "y")
                {
                    Console.WriteLine("Seeding cancelled");
                    return;
                }
                
                dbContext.PaymentPlans.RemoveRange(dbContext.PaymentPlans);
                await dbContext.SaveChangesAsync();
                Console.WriteLine("Existing plans deleted");
            }

            // Create default plans
            var plans = new[]
            {
                new PaymentPlan
                {
                    Name = "Free",
                    Description = "Perfect for trying out the platform",
                    MonthlyPrice = 0,
                    AnnualPrice = 0,
                    MaxUsers = 2,
                    MaxFunds = 1,
                    MaxDebtors = 25,
                    MaxLoans = 100,
                    StorageLimitMB = 500,
                    AllowMonteCarloSimulation = false,
                    AllowPortfolioAnalysis = true,
                    AllowReporting = true,
                    AllowExport = false,
                    AllowImport = true,
                    AllowApiAccess = false,
                    AllowCustomBranding = false,
                    AllowAdvancedAnalytics = false,
                    AllowMultipleFunds = false,
                    SupportLevel = "Email",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 1
                },
                new PaymentPlan
                {
                    Name = "Starter",
                    Description = "Great for small teams managing a single fund",
                    MonthlyPrice = 99,
                    AnnualPrice = 990,
                    MaxUsers = 5,
                    MaxFunds = 3,
                    MaxDebtors = 100,
                    MaxLoans = 500,
                    StorageLimitMB = 2000,
                    AllowMonteCarloSimulation = true,
                    AllowPortfolioAnalysis = true,
                    AllowReporting = true,
                    AllowExport = true,
                    AllowImport = true,
                    AllowApiAccess = false,
                    AllowCustomBranding = false,
                    AllowAdvancedAnalytics = false,
                    AllowMultipleFunds = true,
                    SupportLevel = "Email",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 2
                },
                new PaymentPlan
                {
                    Name = "Professional",
                    Description = "For growing asset managers with multiple funds",
                    MonthlyPrice = 299,
                    AnnualPrice = 2990,
                    MaxUsers = 15,
                    MaxFunds = 10,
                    MaxDebtors = 500,
                    MaxLoans = 2500,
                    StorageLimitMB = 10000,
                    AllowMonteCarloSimulation = true,
                    AllowPortfolioAnalysis = true,
                    AllowReporting = true,
                    AllowExport = true,
                    AllowImport = true,
                    AllowApiAccess = true,
                    AllowCustomBranding = false,
                    AllowAdvancedAnalytics = true,
                    AllowMultipleFunds = true,
                    SupportLevel = "Priority",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 3
                },
                new PaymentPlan
                {
                    Name = "Enterprise",
                    Description = "Unlimited scale for large organizations",
                    MonthlyPrice = 999,
                    AnnualPrice = 9990,
                    MaxUsers = 100,
                    MaxFunds = 50,
                    MaxDebtors = 10000,
                    MaxLoans = 50000,
                    StorageLimitMB = 100000,
                    AllowMonteCarloSimulation = true,
                    AllowPortfolioAnalysis = true,
                    AllowReporting = true,
                    AllowExport = true,
                    AllowImport = true,
                    AllowApiAccess = true,
                    AllowCustomBranding = true,
                    AllowAdvancedAnalytics = true,
                    AllowMultipleFunds = true,
                    SupportLevel = "24/7",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = 4
                }
            };

            dbContext.PaymentPlans.AddRange(plans);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"✓ Successfully seeded {plans.Length} payment plans:");
            foreach (var plan in plans)
            {
                Console.WriteLine($"  - {plan.Name} (€{plan.MonthlyPrice}/month, {plan.MaxUsers} users, {plan.MaxFunds} funds)");
            }
            Console.WriteLine("\nSeeding complete!");
        }
    }
}
