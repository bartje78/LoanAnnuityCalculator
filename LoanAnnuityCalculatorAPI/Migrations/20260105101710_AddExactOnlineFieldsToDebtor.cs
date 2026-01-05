using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddExactOnlineFieldsToDebtor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExactArticleCodeInterest",
                table: "DebtorDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExactArticleCodeRedemption",
                table: "DebtorDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExactDebtorId",
                table: "DebtorDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExactGLInterest",
                table: "DebtorDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExactGLRedemption",
                table: "DebtorDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExactArticleCodeInterest",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "ExactArticleCodeRedemption",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "ExactDebtorId",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "ExactGLInterest",
                table: "DebtorDetails");

            migrationBuilder.DropColumn(
                name: "ExactGLRedemption",
                table: "DebtorDetails");
        }
    }
}
