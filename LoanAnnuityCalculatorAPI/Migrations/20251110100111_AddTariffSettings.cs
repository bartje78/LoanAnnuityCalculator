using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTariffSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TariffSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TariffSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditRatingSpreads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TariffSettingsId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Spread = table.Column<decimal>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "LtvSpreadTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TariffSettingsId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLtv = table.Column<decimal>(type: "TEXT", nullable: false),
                    Spread = table.Column<decimal>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_CreditRatingSpreads_TariffSettingsId",
                table: "CreditRatingSpreads",
                column: "TariffSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_LtvSpreadTiers_TariffSettingsId",
                table: "LtvSpreadTiers",
                column: "TariffSettingsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditRatingSpreads");

            migrationBuilder.DropTable(
                name: "LtvSpreadTiers");

            migrationBuilder.DropTable(
                name: "TariffSettings");
        }
    }
}
