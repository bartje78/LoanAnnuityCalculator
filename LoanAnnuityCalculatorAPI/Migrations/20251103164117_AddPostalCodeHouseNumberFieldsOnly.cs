using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPostalCodeHouseNumberFieldsOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaterals_Loans_LoanId",
                table: "Collaterals");

            migrationBuilder.DropIndex(
                name: "IX_Collaterals_LoanId",
                table: "Collaterals");

            migrationBuilder.DropColumn(
                name: "LoanId",
                table: "Collaterals");

            migrationBuilder.RenameColumn(
                name: "SharedAssetGroup",
                table: "Collaterals",
                newName: "LastAppraisalUpdate");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Collaterals",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "HouseNumber",
                table: "Collaterals",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedDate",
                table: "Collaterals",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Collaterals",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoanCollaterals",
                columns: table => new
                {
                    LoanCollateralId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LoanId = table.Column<int>(type: "INTEGER", nullable: false),
                    CollateralId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AllocationPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanCollaterals", x => x.LoanCollateralId);
                    table.ForeignKey(
                        name: "FK_LoanCollaterals_Collaterals_CollateralId",
                        column: x => x.CollateralId,
                        principalTable: "Collaterals",
                        principalColumn: "CollateralId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanCollaterals_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Collaterals_AssetUniqueId",
                table: "Collaterals",
                column: "AssetUniqueId",
                unique: true,
                filter: "[AssetUniqueId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Collaterals_LandRegistryCode",
                table: "Collaterals",
                column: "LandRegistryCode",
                unique: true,
                filter: "[LandRegistryCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Collaterals_PostalCode_HouseNumber",
                table: "Collaterals",
                columns: new[] { "PostalCode", "HouseNumber" },
                unique: true,
                filter: "[PostalCode] IS NOT NULL AND [HouseNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoanCollaterals_CollateralId",
                table: "LoanCollaterals",
                column: "CollateralId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanCollaterals_LoanId",
                table: "LoanCollaterals",
                column: "LoanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanCollaterals");

            migrationBuilder.DropIndex(
                name: "IX_Collaterals_AssetUniqueId",
                table: "Collaterals");

            migrationBuilder.DropIndex(
                name: "IX_Collaterals_LandRegistryCode",
                table: "Collaterals");

            migrationBuilder.DropIndex(
                name: "IX_Collaterals_PostalCode_HouseNumber",
                table: "Collaterals");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Collaterals");

            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "Collaterals");

            migrationBuilder.DropColumn(
                name: "LastUpdatedDate",
                table: "Collaterals");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Collaterals");

            migrationBuilder.RenameColumn(
                name: "LastAppraisalUpdate",
                table: "Collaterals",
                newName: "SharedAssetGroup");

            migrationBuilder.AddColumn<int>(
                name: "LoanId",
                table: "Collaterals",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Collaterals_LoanId",
                table: "Collaterals",
                column: "LoanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collaterals_Loans_LoanId",
                table: "Collaterals",
                column: "LoanId",
                principalTable: "Loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
