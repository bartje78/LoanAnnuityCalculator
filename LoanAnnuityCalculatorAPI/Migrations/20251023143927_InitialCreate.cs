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
                name: "CreditRatingThresholds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DebtorID = table.Column<int>(type: "INTEGER", nullable: false),
                    RatioName = table.Column<string>(type: "TEXT", nullable: false),
                    CreditRating = table.Column<string>(type: "TEXT", nullable: false),
                    MinValue = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxValue = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditRatingThresholds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DebtorDetails",
                columns: table => new
                {
                    DebtorID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DebtorName = table.Column<string>(type: "TEXT", nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtorDetails", x => x.DebtorID);
                });

            migrationBuilder.CreateTable(
                name: "DebtorBalanceSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DebtorID = table.Column<int>(type: "INTEGER", nullable: false),
                    BookYear = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentAssets = table.Column<decimal>(type: "TEXT", nullable: false),
                    LongTermAssets = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrentLiabilities = table.Column<decimal>(type: "TEXT", nullable: false),
                    LongTermLiabilities = table.Column<decimal>(type: "TEXT", nullable: false),
                    OwnersEquity = table.Column<decimal>(type: "TEXT", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DebtorID = table.Column<int>(type: "INTEGER", nullable: false),
                    BookYear = table.Column<int>(type: "INTEGER", nullable: false),
                    GrossRevenue = table.Column<decimal>(type: "TEXT", nullable: false),
                    EBITDA = table.Column<decimal>(type: "TEXT", nullable: false),
                    InterestCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    NetProfit = table.Column<decimal>(type: "TEXT", nullable: false)
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
                name: "Loans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DebtorID = table.Column<int>(type: "INTEGER", nullable: false),
                    LoanAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    AnnualInterestRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    TenorMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    InterestOnlyMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_DebtorBalanceSheets_DebtorID",
                table: "DebtorBalanceSheets",
                column: "DebtorID");

            migrationBuilder.CreateIndex(
                name: "IX_DebtorPLs_DebtorID",
                table: "DebtorPLs",
                column: "DebtorID");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_DebtorID",
                table: "Loans",
                column: "DebtorID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditRatingThresholds");

            migrationBuilder.DropTable(
                name: "DebtorBalanceSheets");

            migrationBuilder.DropTable(
                name: "DebtorPLs");

            migrationBuilder.DropTable(
                name: "Loans");

            migrationBuilder.DropTable(
                name: "DebtorDetails");
        }
    }
}
