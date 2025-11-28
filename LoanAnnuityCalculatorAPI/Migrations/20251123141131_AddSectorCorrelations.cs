using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorCorrelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityAgriculture",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityConstruction",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityFinancialServices",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityHealthcare",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityHospitality",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityManufacturing",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityOther",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityProfessionalServices",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityRealEstate",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityRetail",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityTechnology",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SectorVolatilityTransportation",
                table: "ModelSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SectorCorrelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModelSettingsId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sector1 = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Sector2 = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CorrelationCoefficient = table.Column<decimal>(type: "TEXT", nullable: false)
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
                name: "StandardRevenueCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Sector = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardRevenueCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SectorCorrelations_ModelSettingsId",
                table: "SectorCorrelations",
                column: "ModelSettingsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SectorCorrelations");

            migrationBuilder.DropTable(
                name: "StandardRevenueCategories");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityAgriculture",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityConstruction",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityFinancialServices",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityHealthcare",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityHospitality",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityManufacturing",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityOther",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityProfessionalServices",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityRealEstate",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityRetail",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityTechnology",
                table: "ModelSettings");

            migrationBuilder.DropColumn(
                name: "SectorVolatilityTransportation",
                table: "ModelSettings");
        }
    }
}
