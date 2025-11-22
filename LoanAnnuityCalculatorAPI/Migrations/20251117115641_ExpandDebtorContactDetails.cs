using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class ExpandDebtorContactDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactCallingName",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactFirstNames",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactLastName",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HouseNumber",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Signatory1Function",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Signatory1Name",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Signatory2Function",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Signatory2Name",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Signatory3Function",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Signatory3Name",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "DebtorDetails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "ContactCallingName",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "ContactFirstNames",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "ContactLastName",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "Signatory1Function",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "Signatory1Name",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "Signatory2Function",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "Signatory2Name",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "Signatory3Function",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "Signatory3Name",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "DebtorDetails");
        }
    }
}
