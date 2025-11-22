using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Models.Loan; // For Loan-related models
using LoanAnnuityCalculatorAPI.Models.Debtor; // For Debtor-related models
using LoanAnnuityCalculatorAPI.Models.Ratios; // For CreditRatingThreshold
using LoanAnnuityCalculatorAPI.Services;
using OfficeOpenXml;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT Settings from appsettings.json
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep PascalCase to match C# conventions
        options.JsonSerializerOptions.WriteIndented = false; // Optional: Disable pretty-printing
        // Remove ReferenceHandler.Preserve to avoid $id and $values metadata
    });

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<EcbApiService>(); // Register ECB API service with HttpClient
builder.Services.AddScoped<LoanAnnuityCalculatorAPI.Services.ExactOnlineService>();
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<AnnuityCalculator>(); // Register the AnnuityCalculator service
builder.Services.AddScoped<PaymentCalculatorService>(); // Register the new PaymentCalculatorService
builder.Services.AddSingleton<LoanDateHelper>();
builder.Services.AddScoped<RatioCalculationService>();
builder.Services.AddScoped<CollateralValidationService>(); // Register the new collateral validation service
builder.Services.AddScoped<PaymentScheduleService>(); // Register the payment schedule service
builder.Services.AddScoped<IStatusCalculationService, StatusCalculationService>(); // Register the status calculation service
builder.Services.AddScoped<FractionalPaymentCalculator>(); // Register the fractional payment calculator
builder.Services.AddScoped<TariffCalculatorService>(); // Register the tariff calculator service
builder.Services.AddScoped<MonteCarloSimulationService>(); // Register the Monte Carlo simulation service
builder.Services.AddScoped<LoanFinancialCalculatorService>(); // Register the loan financial calculator service
builder.Services.AddScoped<BalanceSheetMigrationService>(); // Register the balance sheet migration service
builder.Services.AddScoped<BalanceSheetCalculationService>(); // Register the balance sheet calculation service
builder.Services.AddScoped<IAuthService, AuthService>(); // Register the authentication service

// Register LoanDbContext with the DI container
// Build an absolute path to the database file so migrations and runtime use the same file
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "loans.db");
var connectionString = $"Data Source={dbPath}";
builder.Services.AddDbContext<LoanDbContext>(options =>
    options.UseSqlite(connectionString)); // Use SQLite or your database provider

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<LoanDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? ""))
    };
});

builder.Services.AddAuthorization(options =>
{
    // Define role-based policies
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RiskManagerOrAdmin", policy => policy.RequireRole("Admin", "RiskManager"));
    options.AddPolicy("ViewerOrAbove", policy => policy.RequireRole("Admin", "RiskManager", "Viewer"));
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:4201") // Allow requests from both frontend ports
              .AllowAnyHeader() // Allow all headers
              .AllowAnyMethod(); // Allow all HTTP methods (GET, POST, etc.)
    });
});

var app = builder.Build();

// Use CORS
app.UseCors("AllowSpecificOrigins");

// Use Authentication & Authorization (order matters!)
app.UseAuthentication();
app.UseAuthorization();

//app.UseHttpsRedirection();
app.MapControllers();

/// Seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LoanDbContext>();

    // Apply migrations to ensure the database schema is up-to-date
    Console.WriteLine("Applying migrations...");
    dbContext.Database.Migrate();

    // Seed DebtorDetails
    if (!dbContext.DebtorDetails.Any())
    {
        Console.WriteLine("Seeding DebtorDetails...");
        var debtor = new DebtorDetails
        {
            DebtorName = "John Doe",
            ContactPerson = "Jane Doe",
            Address = "123 Main St Springfield",
            Email = "johndoe@example.com"
        };
        var debtor1 = new DebtorDetails
        {
            DebtorName = "Harry Hakkel",
            ContactPerson = "Jannie Hakkel",
            Address = "321 Backstreet Ohio",
            Email = "harryhakkel@example.com"
        };

        var balanceSheet = new DebtorBalanceSheet
        {
            BookYear = 2025,
            CurrentAssets = 50000,
            LongTermAssets = 20000,
            CurrentLiabilities = 30000,
            LongTermLiabilities = 40000,
            OwnersEquity = 10000,
            DebtorDetails = debtor
        };
               var balanceSheet1 = new DebtorBalanceSheet
        {
            BookYear = 2024,
            CurrentAssets = 40000,
            LongTermAssets = 30000,
            CurrentLiabilities = 20000,
            LongTermLiabilities = 10000,
            OwnersEquity = 10000,
            DebtorDetails = debtor
        };

        var profitAndLoss = new DebtorPL
        {
            BookYear = 2025,
            GrossRevenue = 150000,
            EBITDA = 50000,
            InterestCost = 10000,
            NetProfit = 20000,
            DebtorDetails = debtor
        };

        debtor.BalanceSheets = new List<DebtorBalanceSheet> { balanceSheet, balanceSheet1 };
        debtor.ProfitAndLossStatements = new List<DebtorPL> { profitAndLoss };

        dbContext.DebtorDetails.AddRange(debtor, debtor1);

        // Seed Loans
        if (!dbContext.Loans.Any())
        {
            Console.WriteLine("Seeding Loans...");
            var loan1 = new Loan
            {
                LoanAmount = 10000,
                AnnualInterestRate = 5.0m,
                TenorMonths = 24,
                InterestOnlyMonths = 0,
                StartDate = new DateTime(2023, 1, 1),
                DebtorDetails = debtor
            };

            var loan2 = new Loan
            {
                LoanAmount = 20000,
                AnnualInterestRate = 4.5m,
                TenorMonths = 36,
                InterestOnlyMonths = 6,
                StartDate = new DateTime(2023, 6, 1),
                DebtorDetails = debtor
            };

            dbContext.Loans.AddRange(loan1, loan2);
            dbContext.SaveChanges();
            Console.WriteLine("Loans seeded successfully.");
        }
        else
        {
            Console.WriteLine("Loans already seeded.");
        }
    }

    // Seed CreditRatingThresholds
    if (!dbContext.CreditRatingThresholds.Any())
    {
        Console.WriteLine("Seeding CreditRatingThresholds...");
        dbContext.CreditRatingThresholds.AddRange(new List<CreditRatingThreshold>
        {
            new CreditRatingThreshold { RatioName = "CurrentRatio", CreditRating = "AAA", MinValue = 2.0m, MaxValue = 999.0m },
            new CreditRatingThreshold { RatioName = "CurrentRatio", CreditRating = "AA", MinValue = 1.5m, MaxValue = 2.0m },
            new CreditRatingThreshold { RatioName = "QuickRatio", CreditRating = "AAA", MinValue = 1.5m, MaxValue = 999.0m },
            new CreditRatingThreshold { RatioName = "QuickRatio", CreditRating = "AA", MinValue = 1.2m, MaxValue = 1.5m },
            new CreditRatingThreshold { RatioName = "DebtToEquityRatio", CreditRating = "AAA", MinValue = 0.0m, MaxValue = 0.5m },
            new CreditRatingThreshold { RatioName = "DebtToEquityRatio", CreditRating = "AA", MinValue = 0.5m, MaxValue = 1.0m }
        });

        dbContext.SaveChanges();
        Console.WriteLine("CreditRatingThresholds seeded.");
    }
}

app.Run();