using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddModelSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SettingName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DefaultRevenueGrowthRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    DefaultOperatingCostGrowthRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    DefaultRevenueVolatility = table.Column<decimal>(type: "TEXT", nullable: false),
                    DefaultEbitdaMarginVolatility = table.Column<decimal>(type: "TEXT", nullable: false),
                    DefaultOperatingCostVolatility = table.Column<decimal>(type: "TEXT", nullable: false),
                    DefaultCorporateTaxRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    DefaultCollateralExpectedReturn = table.Column<decimal>(type: "TEXT", nullable: false),
                    DefaultCollateralVolatility = table.Column<decimal>(type: "TEXT", nullable: false),
                    DefaultCollateralCorrelation = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyTypeParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModelSettingsId = table.Column<int>(type: "INTEGER", nullable: false),
                    PropertyType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExpectedReturn = table.Column<decimal>(type: "TEXT", nullable: false),
                    Volatility = table.Column<decimal>(type: "TEXT", nullable: false),
                    CorrelationWithRevenue = table.Column<decimal>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTypeParameters_ModelSettingsId",
                table: "PropertyTypeParameters",
                column: "ModelSettingsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyTypeParameters");

            migrationBuilder.DropTable(
                name: "ModelSettings");
        }
    }
}
