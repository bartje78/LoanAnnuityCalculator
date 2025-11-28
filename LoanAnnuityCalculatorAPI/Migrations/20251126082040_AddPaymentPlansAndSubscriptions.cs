using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPlansAndSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentPlans",
                columns: table => new
                {
                    PaymentPlanId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxFunds = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxDebtors = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLoans = table.Column<int>(type: "INTEGER", nullable: false),
                    StorageLimitMB = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowMonteCarloSimulation = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowPortfolioAnalysis = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowReporting = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowExport = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowImport = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowApiAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowCustomBranding = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowAdvancedAnalytics = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowMultipleFunds = table.Column<bool>(type: "INTEGER", nullable: false),
                    SupportLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlans", x => x.PaymentPlanId);
                });

            migrationBuilder.CreateTable(
                name: "UsageTrackings",
                columns: table => new
                {
                    UsageId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActiveUserCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalUserCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FundCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DebtorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LoanCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CollateralCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StorageUsedMB = table.Column<decimal>(type: "TEXT", nullable: false),
                    MonteCarloSimulationsRun = table.Column<int>(type: "INTEGER", nullable: false),
                    ReportsGenerated = table.Column<int>(type: "INTEGER", nullable: false),
                    ApiCallsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ExportsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "TenantSubscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BillingPeriod = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TrialEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextBillingDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CustomMaxUsers = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomMaxFunds = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomMaxDebtors = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomMaxLoans = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomStorageLimitMB = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomAllowMonteCarloSimulation = table.Column<bool>(type: "INTEGER", nullable: true),
                    CustomAllowPortfolioAnalysis = table.Column<bool>(type: "INTEGER", nullable: true),
                    CustomAllowReporting = table.Column<bool>(type: "INTEGER", nullable: true),
                    CustomAllowApiAccess = table.Column<bool>(type: "INTEGER", nullable: true),
                    CustomAllowAdvancedAnalytics = table.Column<bool>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantSubscriptions");

            migrationBuilder.DropTable(
                name: "UsageTrackings");

            migrationBuilder.DropTable(
                name: "PaymentPlans");
        }
    }
}
