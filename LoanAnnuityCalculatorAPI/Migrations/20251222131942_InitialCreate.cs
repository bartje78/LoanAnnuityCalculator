using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollateralIndexes",
                columns: table => new
                {
                    CollateralIndexId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollateralType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quarter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PriceIndex = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollateralIndexes", x => x.CollateralIndexId);
                });

            migrationBuilder.CreateTable(
                name: "Collaterals",
                columns: table => new
                {
                    CollateralId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    CollateralType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppraisalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AppraisalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PropertyType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PropertyAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LandRegistryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    HouseNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AssetUniqueId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SecurityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FirstMortgageAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LiquidityHaircut = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAppraisalUpdate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collaterals", x => x.CollateralId);
                });

            migrationBuilder.CreateTable(
                name: "CompanySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FundName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractTextBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InsertPaymentChart = table.Column<bool>(type: "bit", nullable: false),
                    InsertBseTable = table.Column<bool>(type: "bit", nullable: false),
                    ShowSectionHeader = table.Column<bool>(type: "bit", nullable: false),
                    RedemptionScheduleType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SecurityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTextBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditRatingThresholds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DebtorID = table.Column<int>(type: "int", nullable: false),
                    RatioName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreditRating = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditRatingThresholds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DebtorDetails",
                columns: table => new
                {
                    DebtorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    DebtorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactFirstNames = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactLastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactCallingName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HouseNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsProspect = table.Column<bool>(type: "bit", nullable: false),
                    CorporateTaxRate = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    Signatory1Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Signatory1Function = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Signatory2Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Signatory2Function = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Signatory3Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Signatory3Function = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtorDetails", x => x.DebtorID);
                });

            migrationBuilder.CreateTable(
                name: "ExactOnlineTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Division = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExactOnlineTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DefaultPaymentTerms = table.Column<int>(type: "int", nullable: false),
                    InvoicePrefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    InvoiceNumberStart = table.Column<int>(type: "int", nullable: false),
                    DueDateCalculation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReminderDays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultCurrency = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IncludeCompanyLogo = table.Column<bool>(type: "bit", nullable: false),
                    FooterText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    InvoiceDay = table.Column<int>(type: "int", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BIC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountHolder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoanStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCalculated = table.Column<bool>(type: "bit", nullable: false),
                    CalculationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultRevenueGrowthRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultOperatingCostGrowthRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultRevenueVolatility = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultEbitdaMarginVolatility = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultOperatingCostVolatility = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultCorporateTaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultCollateralExpectedReturn = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultCollateralVolatility = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultCollateralCorrelation = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityManufacturing = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityRetail = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityRealEstate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityHealthcare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityTechnology = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityProfessionalServices = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityHospitality = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityAgriculture = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityConstruction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityFinancialServices = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityTransportation = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SectorVolatilityOther = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlans",
                columns: table => new
                {
                    PaymentPlanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    MaxFunds = table.Column<int>(type: "int", nullable: false),
                    MaxDebtors = table.Column<int>(type: "int", nullable: false),
                    MaxLoans = table.Column<int>(type: "int", nullable: false),
                    StorageLimitMB = table.Column<int>(type: "int", nullable: false),
                    AllowMonteCarloSimulation = table.Column<bool>(type: "bit", nullable: false),
                    AllowPortfolioAnalysis = table.Column<bool>(type: "bit", nullable: false),
                    AllowReporting = table.Column<bool>(type: "bit", nullable: false),
                    AllowExport = table.Column<bool>(type: "bit", nullable: false),
                    AllowImport = table.Column<bool>(type: "bit", nullable: false),
                    AllowApiAccess = table.Column<bool>(type: "bit", nullable: false),
                    AllowCustomBranding = table.Column<bool>(type: "bit", nullable: false),
                    AllowAdvancedAnalytics = table.Column<bool>(type: "bit", nullable: false),
                    AllowMultipleFunds = table.Column<bool>(type: "bit", nullable: false),
                    SupportLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlans", x => x.PaymentPlanId);
                });

            migrationBuilder.CreateTable(
                name: "PlanAddOns",
                columns: table => new
                {
                    AddOnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanAddOns", x => x.AddOnId);
                });

            migrationBuilder.CreateTable(
                name: "StandardRevenueCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardRevenueCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TariffSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TariffSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DatabaseKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.TenantId);
                });

            migrationBuilder.CreateTable(
                name: "UserPricingTiers",
                columns: table => new
                {
                    TierId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinUsers = table.Column<int>(type: "int", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: true),
                    BaseMonthlyPricePerUser = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaseAnnualPricePerUser = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPricingTiers", x => x.TierId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DebtorBalanceSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DebtorID = table.Column<int>(type: "int", nullable: false),
                    BookYear = table.Column<int>(type: "int", nullable: false),
                    IsProForma = table.Column<bool>(type: "bit", nullable: false),
                    CurrentAssets = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LongTermAssets = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentLiabilities = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LongTermLiabilities = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OwnersEquity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FirstLienLoanAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FirstLienInterestRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FirstLienTenorMonths = table.Column<int>(type: "int", nullable: true),
                    FirstLienRedemptionSchedule = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtorBalanceSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebtorBalanceSheets_DebtorDetails_DebtorID",
                        column: x => x.DebtorID,
                        principalTable: "DebtorDetails",
                        principalColumn: "DebtorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DebtorPLs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DebtorID = table.Column<int>(type: "int", nullable: false),
                    BookYear = table.Column<int>(type: "int", nullable: false),
                    IsProForma = table.Column<bool>(type: "bit", nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OperatingExpenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostOfGoodsSold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterestExpense = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxExpense = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Depreciation = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EBIT = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EBT = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Cashflow = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CapitalRepayment = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FreeCashflow = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GrossRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EBITDA = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    InterestCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NetProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RevenueSectorBreakdown = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtorPLs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebtorPLs_DebtorDetails_DebtorID",
                        column: x => x.DebtorID,
                        principalTable: "DebtorDetails",
                        principalColumn: "DebtorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DebtorSignatories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DebtorID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Function = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtorSignatories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebtorSignatories_DebtorDetails_DebtorID",
                        column: x => x.DebtorID,
                        principalTable: "DebtorDetails",
                        principalColumn: "DebtorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DebtorID = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    LoanAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualInterestRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenorMonths = table.Column<int>(type: "int", nullable: false),
                    InterestOnlyMonths = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedemptionSchedule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecurityType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstMortgageAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmountDrawn = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ManualOutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loans_DebtorDetails_DebtorID",
                        column: x => x.DebtorID,
                        principalTable: "DebtorDetails",
                        principalColumn: "DebtorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyTypeParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelSettingsId = table.Column<int>(type: "int", nullable: false),
                    PropertyType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpectedReturn = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Volatility = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CorrelationWithRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTypeParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyTypeParameters_ModelSettings_ModelSettingsId",
                        column: x => x.ModelSettingsId,
                        principalTable: "ModelSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectorCollateralCorrelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelSettingsId = table.Column<int>(type: "int", nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PropertyType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CorrelationCoefficient = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorCollateralCorrelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectorCollateralCorrelations_ModelSettings_ModelSettingsId",
                        column: x => x.ModelSettingsId,
                        principalTable: "ModelSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectorCorrelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelSettingsId = table.Column<int>(type: "int", nullable: false),
                    Sector1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sector2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CorrelationCoefficient = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorCorrelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectorCorrelations_ModelSettings_ModelSettingsId",
                        column: x => x.ModelSettingsId,
                        principalTable: "ModelSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectorDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelSettingsId = table.Column<int>(type: "int", nullable: false),
                    SectorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    DefaultVolatility = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpectedGrowth = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectorDefinitions_ModelSettings_ModelSettingsId",
                        column: x => x.ModelSettingsId,
                        principalTable: "ModelSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AddOnPermissions",
                columns: table => new
                {
                    AddOnPermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddOnId = table.Column<int>(type: "int", nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PermissionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnPermissions", x => x.AddOnPermissionId);
                    table.ForeignKey(
                        name: "FK_AddOnPermissions_PlanAddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "PlanAddOns",
                        principalColumn: "AddOnId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AddOnPricingTiers",
                columns: table => new
                {
                    TierId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddOnId = table.Column<int>(type: "int", nullable: false),
                    MinQuantity = table.Column<int>(type: "int", nullable: false),
                    MaxQuantity = table.Column<int>(type: "int", nullable: true),
                    MonthlyPricePerAssignment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualPricePerAssignment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnPricingTiers", x => x.TierId);
                    table.ForeignKey(
                        name: "FK_AddOnPricingTiers_PlanAddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "PlanAddOns",
                        principalColumn: "AddOnId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditRatingSpreads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TariffSettingsId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Spread = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditRatingSpreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditRatingSpreads_TariffSettings_TariffSettingsId",
                        column: x => x.TariffSettingsId,
                        principalTable: "TariffSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImpactDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TariffSettingsId = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpactDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImpactDiscounts_TariffSettings_TariffSettingsId",
                        column: x => x.TariffSettingsId,
                        principalTable: "TariffSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LtvSpreadTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TariffSettingsId = table.Column<int>(type: "int", nullable: false),
                    MaxLtv = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Spread = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtvSpreadTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LtvSpreadTiers_TariffSettings_TariffSettingsId",
                        column: x => x.TariffSettingsId,
                        principalTable: "TariffSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    IsSystemAdmin = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Funds",
                columns: table => new
                {
                    FundId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FundCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funds", x => x.FundId);
                    table.ForeignKey(
                        name: "FK_Funds_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantAddOns",
                columns: table => new
                {
                    TenantAddOnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AddOnId = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CustomMonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CustomAnnualPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EnabledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAddOns", x => x.TenantAddOnId);
                    table.ForeignKey(
                        name: "FK_TenantAddOns_PlanAddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "PlanAddOns",
                        principalColumn: "AddOnId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantAddOns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantCustomPricings",
                columns: table => new
                {
                    CustomPricingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CustomMaxUsers = table.Column<int>(type: "int", nullable: true),
                    CustomMaxFunds = table.Column<int>(type: "int", nullable: true),
                    CustomMaxDebtors = table.Column<int>(type: "int", nullable: true),
                    CustomMaxLoans = table.Column<int>(type: "int", nullable: true),
                    CustomStorageLimitMB = table.Column<int>(type: "int", nullable: true),
                    PricePerUser = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BaseMonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BaseAnnualPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MultiYearDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantCustomPricings", x => x.CustomPricingId);
                    table.ForeignKey(
                        name: "FK_TenantCustomPricings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantSubscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PaymentPlanId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BillingPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrialEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextBillingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomMaxUsers = table.Column<int>(type: "int", nullable: true),
                    CustomMaxFunds = table.Column<int>(type: "int", nullable: true),
                    CustomMaxDebtors = table.Column<int>(type: "int", nullable: true),
                    CustomMaxLoans = table.Column<int>(type: "int", nullable: true),
                    CustomStorageLimitMB = table.Column<int>(type: "int", nullable: true),
                    CustomAllowMonteCarloSimulation = table.Column<bool>(type: "bit", nullable: true),
                    CustomAllowPortfolioAnalysis = table.Column<bool>(type: "bit", nullable: true),
                    CustomAllowReporting = table.Column<bool>(type: "bit", nullable: true),
                    CustomAllowApiAccess = table.Column<bool>(type: "bit", nullable: true),
                    CustomAllowAdvancedAnalytics = table.Column<bool>(type: "bit", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSubscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_PaymentPlans_PaymentPlanId",
                        column: x => x.PaymentPlanId,
                        principalTable: "PaymentPlans",
                        principalColumn: "PaymentPlanId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsageTrackings",
                columns: table => new
                {
                    UsageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActiveUserCount = table.Column<int>(type: "int", nullable: false),
                    TotalUserCount = table.Column<int>(type: "int", nullable: false),
                    FundCount = table.Column<int>(type: "int", nullable: false),
                    DebtorCount = table.Column<int>(type: "int", nullable: false),
                    LoanCount = table.Column<int>(type: "int", nullable: false),
                    CollateralCount = table.Column<int>(type: "int", nullable: false),
                    StorageUsedMB = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonteCarloSimulationsRun = table.Column<int>(type: "int", nullable: false),
                    ReportsGenerated = table.Column<int>(type: "int", nullable: false),
                    ApiCallsCount = table.Column<int>(type: "int", nullable: false),
                    ExportsCount = table.Column<int>(type: "int", nullable: false),
                    ImportsCount = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageTrackings", x => x.UsageId);
                    table.ForeignKey(
                        name: "FK_UsageTrackings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevenueDetails",
                columns: table => new
                {
                    RevenueDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PLId = table.Column<int>(type: "int", nullable: false),
                    RevenueCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevenueDetails", x => x.RevenueDetailId);
                    table.ForeignKey(
                        name: "FK_RevenueDetails_DebtorPLs_PLId",
                        column: x => x.PLId,
                        principalTable: "DebtorPLs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BalanceSheetLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BalanceSheetId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LoanId = table.Column<int>(type: "int", nullable: true),
                    CollateralId = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAutoGenerated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceSheetLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BalanceSheetLineItems_Collaterals_CollateralId",
                        column: x => x.CollateralId,
                        principalTable: "Collaterals",
                        principalColumn: "CollateralId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BalanceSheetLineItems_DebtorBalanceSheets_BalanceSheetId",
                        column: x => x.BalanceSheetId,
                        principalTable: "DebtorBalanceSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BalanceSheetLineItems_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BuildingDepotWithdrawals",
                columns: table => new
                {
                    WithdrawalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanId = table.Column<int>(type: "int", nullable: false),
                    WithdrawalType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WithdrawalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeclarationFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeclarationFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingDepotWithdrawals", x => x.WithdrawalId);
                    table.ForeignKey(
                        name: "FK_BuildingDepotWithdrawals_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanCollaterals",
                columns: table => new
                {
                    LoanCollateralId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanId = table.Column<int>(type: "int", nullable: false),
                    CollateralId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllocationPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanCollaterals", x => x.LoanCollateralId);
                    table.ForeignKey(
                        name: "FK_LoanCollaterals_Collaterals_CollateralId",
                        column: x => x.CollateralId,
                        principalTable: "Collaterals",
                        principalColumn: "CollateralId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanCollaterals_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanPayments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanId = table.Column<int>(type: "int", nullable: false),
                    PaymentMonth = table.Column<int>(type: "int", nullable: false),
                    InterestAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CapitalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaysLate = table.Column<int>(type: "int", nullable: false),
                    RemainingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanPayments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_LoanPayments_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantPricingSummaries",
                columns: table => new
                {
                    SummaryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ActiveUserCount = table.Column<int>(type: "int", nullable: false),
                    MonthlyUserCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualUserCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonthlyAddOnCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualAddOnCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalMonthly = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAnnual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantAgreed = table.Column<bool>(type: "bit", nullable: false),
                    AgreedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgreedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgreedById = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPricingSummaries", x => x.SummaryId);
                    table.ForeignKey(
                        name: "FK_TenantPricingSummaries_AspNetUsers_AgreedById",
                        column: x => x.AgreedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantPricingSummaries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAddOns",
                columns: table => new
                {
                    UserAddOnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AddOnId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedById = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAddOns", x => x.UserAddOnId);
                    table.ForeignKey(
                        name: "FK_UserAddOns_AspNetUsers_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserAddOns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAddOns_PlanAddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "PlanAddOns",
                        principalColumn: "AddOnId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAddOns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PreferenceKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreferenceValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFundAccesses",
                columns: table => new
                {
                    UserFundAccessId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFundAccesses", x => x.UserFundAccessId);
                    table.ForeignKey(
                        name: "FK_UserFundAccesses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFundAccesses_Funds_FundId",
                        column: x => x.FundId,
                        principalTable: "Funds",
                        principalColumn: "FundId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuildingDepotWithdrawalLineItems",
                columns: table => new
                {
                    LineItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WithdrawalId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupplierIBAN = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    ReceiptFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceiptFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingDepotWithdrawalLineItems", x => x.LineItemId);
                    table.ForeignKey(
                        name: "FK_BuildingDepotWithdrawalLineItems_BuildingDepotWithdrawals_WithdrawalId",
                        column: x => x.WithdrawalId,
                        principalTable: "BuildingDepotWithdrawals",
                        principalColumn: "WithdrawalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AddOnPermissions_AddOnId",
                table: "AddOnPermissions",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnPricingTiers_AddOnId",
                table: "AddOnPricingTiers",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceSheetLineItems_BalanceSheetId",
                table: "BalanceSheetLineItems",
                column: "BalanceSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceSheetLineItems_CollateralId",
                table: "BalanceSheetLineItems",
                column: "CollateralId");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceSheetLineItems_LoanId",
                table: "BalanceSheetLineItems",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingDepotWithdrawalLineItems_WithdrawalId",
                table: "BuildingDepotWithdrawalLineItems",
                column: "WithdrawalId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingDepotWithdrawals_LoanId",
                table: "BuildingDepotWithdrawals",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaterals_AssetUniqueId",
                table: "Collaterals",
                column: "AssetUniqueId",
                unique: true,
                filter: "[AssetUniqueId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Collaterals_FundId",
                table: "Collaterals",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaterals_LandRegistryCode",
                table: "Collaterals",
                column: "LandRegistryCode",
                unique: true,
                filter: "[LandRegistryCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Collaterals_PostalCode_HouseNumber",
                table: "Collaterals",
                columns: new[] { "PostalCode", "HouseNumber" },
                unique: true,
                filter: "[PostalCode] IS NOT NULL AND [HouseNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Collaterals_TenantId",
                table: "Collaterals",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditRatingSpreads_TariffSettingsId",
                table: "CreditRatingSpreads",
                column: "TariffSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtorBalanceSheets_DebtorID",
                table: "DebtorBalanceSheets",
                column: "DebtorID");

            migrationBuilder.CreateIndex(
                name: "IX_DebtorDetails_FundId",
                table: "DebtorDetails",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtorDetails_TenantId",
                table: "DebtorDetails",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtorPLs_DebtorID",
                table: "DebtorPLs",
                column: "DebtorID");

            migrationBuilder.CreateIndex(
                name: "IX_DebtorSignatories_DebtorID",
                table: "DebtorSignatories",
                column: "DebtorID");

            migrationBuilder.CreateIndex(
                name: "IX_Funds_TenantId",
                table: "Funds",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpactDiscounts_TariffSettingsId",
                table: "ImpactDiscounts",
                column: "TariffSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanCollaterals_CollateralId",
                table: "LoanCollaterals",
                column: "CollateralId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanCollaterals_LoanId",
                table: "LoanCollaterals",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPayments_LoanId",
                table: "LoanPayments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_DebtorID",
                table: "Loans",
                column: "DebtorID");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_FundId",
                table: "Loans",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_TenantId",
                table: "Loans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LtvSpreadTiers_TariffSettingsId",
                table: "LtvSpreadTiers",
                column: "TariffSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTypeParameters_ModelSettingsId",
                table: "PropertyTypeParameters",
                column: "ModelSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueDetails_PLId",
                table: "RevenueDetails",
                column: "PLId");

            migrationBuilder.CreateIndex(
                name: "IX_SectorCollateralCorrelations_ModelSettingsId",
                table: "SectorCollateralCorrelations",
                column: "ModelSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_SectorCorrelations_ModelSettingsId",
                table: "SectorCorrelations",
                column: "ModelSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_SectorDefinitions_ModelSettingsId",
                table: "SectorDefinitions",
                column: "ModelSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAddOns_AddOnId",
                table: "TenantAddOns",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAddOns_TenantId_AddOnId",
                table: "TenantAddOns",
                columns: new[] { "TenantId", "AddOnId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantCustomPricings_TenantId",
                table: "TenantCustomPricings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantPricingSummaries_AgreedById",
                table: "TenantPricingSummaries",
                column: "AgreedById");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPricingSummaries_TenantId_CalculatedAt",
                table: "TenantPricingSummaries",
                columns: new[] { "TenantId", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_PaymentPlanId",
                table: "TenantSubscriptions",
                column: "PaymentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_TenantId",
                table: "TenantSubscriptions",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsageTrackings_TenantId_Year_Month",
                table: "UsageTrackings",
                columns: new[] { "TenantId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAddOns_AddOnId",
                table: "UserAddOns",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddOns_AssignedById",
                table: "UserAddOns",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddOns_TenantId",
                table: "UserAddOns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddOns_UserId_AddOnId_TenantId",
                table: "UserAddOns",
                columns: new[] { "UserId", "AddOnId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFundAccesses_FundId",
                table: "UserFundAccesses",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFundAccesses_UserId",
                table: "UserFundAccesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId_PreferenceKey",
                table: "UserPreferences",
                columns: new[] { "UserId", "PreferenceKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddOnPermissions");

            migrationBuilder.DropTable(
                name: "AddOnPricingTiers");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BalanceSheetLineItems");

            migrationBuilder.DropTable(
                name: "BuildingDepotWithdrawalLineItems");

            migrationBuilder.DropTable(
                name: "CollateralIndexes");

            migrationBuilder.DropTable(
                name: "CompanySettings");

            migrationBuilder.DropTable(
                name: "ContractTextBlocks");

            migrationBuilder.DropTable(
                name: "CreditRatingSpreads");

            migrationBuilder.DropTable(
                name: "CreditRatingThresholds");

            migrationBuilder.DropTable(
                name: "DebtorSignatories");

            migrationBuilder.DropTable(
                name: "ExactOnlineTokens");

            migrationBuilder.DropTable(
                name: "ImpactDiscounts");

            migrationBuilder.DropTable(
                name: "InvoiceSettings");

            migrationBuilder.DropTable(
                name: "LoanCollaterals");

            migrationBuilder.DropTable(
                name: "LoanPayments");

            migrationBuilder.DropTable(
                name: "LoanStatuses");

            migrationBuilder.DropTable(
                name: "LtvSpreadTiers");

            migrationBuilder.DropTable(
                name: "PropertyTypeParameters");

            migrationBuilder.DropTable(
                name: "RevenueDetails");

            migrationBuilder.DropTable(
                name: "SectorCollateralCorrelations");

            migrationBuilder.DropTable(
                name: "SectorCorrelations");

            migrationBuilder.DropTable(
                name: "SectorDefinitions");

            migrationBuilder.DropTable(
                name: "StandardRevenueCategories");

            migrationBuilder.DropTable(
                name: "TenantAddOns");

            migrationBuilder.DropTable(
                name: "TenantCustomPricings");

            migrationBuilder.DropTable(
                name: "TenantPricingSummaries");

            migrationBuilder.DropTable(
                name: "TenantSubscriptions");

            migrationBuilder.DropTable(
                name: "UsageTrackings");

            migrationBuilder.DropTable(
                name: "UserAddOns");

            migrationBuilder.DropTable(
                name: "UserFundAccesses");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "UserPricingTiers");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "DebtorBalanceSheets");

            migrationBuilder.DropTable(
                name: "BuildingDepotWithdrawals");

            migrationBuilder.DropTable(
                name: "Collaterals");

            migrationBuilder.DropTable(
                name: "TariffSettings");

            migrationBuilder.DropTable(
                name: "DebtorPLs");

            migrationBuilder.DropTable(
                name: "ModelSettings");

            migrationBuilder.DropTable(
                name: "PaymentPlans");

            migrationBuilder.DropTable(
                name: "PlanAddOns");

            migrationBuilder.DropTable(
                name: "Funds");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "DebtorDetails");
        }
    }
}
