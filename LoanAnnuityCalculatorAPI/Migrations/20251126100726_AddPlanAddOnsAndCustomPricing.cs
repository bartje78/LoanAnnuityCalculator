using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanAddOnsAndCustomPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanAddOns",
                columns: table => new
                {
                    AddOnId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    FeatureKey = table.Column<string>(type: "TEXT", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanAddOns", x => x.AddOnId);
                });

            migrationBuilder.CreateTable(
                name: "TenantCustomPricings",
                columns: table => new
                {
                    CustomPricingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomMaxUsers = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomMaxFunds = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomMaxDebtors = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomMaxLoans = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomStorageLimitMB = table.Column<int>(type: "INTEGER", nullable: true),
                    PricePerUser = table.Column<decimal>(type: "TEXT", nullable: true),
                    BaseMonthlyPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    BaseAnnualPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    MultiYearDiscount = table.Column<decimal>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                name: "TenantAddOns",
                columns: table => new
                {
                    TenantAddOnId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AddOnId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CustomMonthlyPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    CustomAnnualPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    EnabledAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantAddOns");

            migrationBuilder.DropTable(
                name: "TenantCustomPricings");

            migrationBuilder.DropTable(
                name: "PlanAddOns");
        }
    }
}
