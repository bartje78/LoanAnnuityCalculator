using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddConditionalDisplayToContractTextBlock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RedemptionScheduleType",
                table: "ContractTextBlocks",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityType",
                table: "ContractTextBlocks",
                type: "TEXT",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedemptionScheduleType",
                table: "ContractTextBlocks");

            migrationBuilder.DropColumn(
                name: "SecurityType",
                table: "ContractTextBlocks");
        }
    }
}
