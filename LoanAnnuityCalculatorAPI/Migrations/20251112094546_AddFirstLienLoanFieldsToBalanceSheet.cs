using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstLienLoanFieldsToBalanceSheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FirstLienInterestRate",
                table: "DebtorBalanceSheets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FirstLienLoanAmount",
                table: "DebtorBalanceSheets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstLienRedemptionSchedule",
                table: "DebtorBalanceSheets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirstLienTenorMonths",
                table: "DebtorBalanceSheets",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstLienInterestRate",
                table: "DebtorBalanceSheets");

            migrationBuilder.DropColumn(
                name: "FirstLienLoanAmount",
                table: "DebtorBalanceSheets");

            migrationBuilder.DropColumn(
                name: "FirstLienRedemptionSchedule",
                table: "DebtorBalanceSheets");

            migrationBuilder.DropColumn(
                name: "FirstLienTenorMonths",
                table: "DebtorBalanceSheets");
        }
    }
}
