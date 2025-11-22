using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCollateralUniqueIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetUniqueId",
                table: "Collaterals",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandRegistryCode",
                table: "Collaterals",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropertyAddress",
                table: "Collaterals",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SharedAssetGroup",
                table: "Collaterals",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetUniqueId",
                table: "Collaterals");

            migrationBuilder.DropColumn(
                name: "LandRegistryCode",
                table: "Collaterals");

            migrationBuilder.DropColumn(
                name: "PropertyAddress",
                table: "Collaterals");

            migrationBuilder.DropColumn(
                name: "SharedAssetGroup",
                table: "Collaterals");
        }
    }
}
