using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class TieredPricingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AddOnPermissions",
                columns: table => new
                {
                    AddOnPermissionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AddOnId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissionKey = table.Column<string>(type: "TEXT", nullable: false),
                    PermissionName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnPermissions", x => x.AddOnPermissionId);
                    table.ForeignKey(
                        name: "FK_AddOnPermissions_PlanAddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "PlanAddOns",
                        principalColumn: "AddOnId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AddOnPricingTiers",
                columns: table => new
                {
                    TierId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AddOnId = table.Column<int>(type: "INTEGER", nullable: false),
                    MinQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                    MonthlyPricePerAssignment = table.Column<decimal>(type: "TEXT", nullable: false),
                    AnnualPricePerAssignment = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnPricingTiers", x => x.TierId);
                    table.ForeignKey(
                        name: "FK_AddOnPricingTiers_PlanAddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "PlanAddOns",
                        principalColumn: "AddOnId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantPricingSummaries",
                columns: table => new
                {
                    SummaryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveUserCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyUserCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    AnnualUserCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    MonthlyAddOnCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    AnnualAddOnCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalMonthly = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalAnnual = table.Column<decimal>(type: "TEXT", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantAgreed = table.Column<bool>(type: "INTEGER", nullable: false),
                    AgreedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AgreedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    AgreedById = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPricingSummaries", x => x.SummaryId);
                    table.ForeignKey(
                        name: "FK_TenantPricingSummaries_AspNetUsers_AgreedById",
                        column: x => x.AgreedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantPricingSummaries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAddOns",
                columns: table => new
                {
                    UserAddOnId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    AddOnId = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedById = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAddOns", x => x.UserAddOnId);
                    table.ForeignKey(
                        name: "FK_UserAddOns_AspNetUsers_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserAddOns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAddOns_PlanAddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "PlanAddOns",
                        principalColumn: "AddOnId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAddOns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPricingTiers",
                columns: table => new
                {
                    TierId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MinUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxUsers = table.Column<int>(type: "INTEGER", nullable: true),
                    BaseMonthlyPricePerUser = table.Column<decimal>(type: "TEXT", nullable: false),
                    BaseAnnualPricePerUser = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPricingTiers", x => x.TierId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AddOnPermissions_AddOnId",
                table: "AddOnPermissions",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnPricingTiers_AddOnId",
                table: "AddOnPricingTiers",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPricingSummaries_AgreedById",
                table: "TenantPricingSummaries",
                column: "AgreedById");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPricingSummaries_TenantId_CalculatedAt",
                table: "TenantPricingSummaries",
                columns: new[] { "TenantId", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAddOns_AddOnId",
                table: "UserAddOns",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddOns_AssignedById",
                table: "UserAddOns",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddOns_TenantId",
                table: "UserAddOns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddOns_UserId_AddOnId_TenantId",
                table: "UserAddOns",
                columns: new[] { "UserId", "AddOnId", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddOnPermissions");

            migrationBuilder.DropTable(
                name: "AddOnPricingTiers");

            migrationBuilder.DropTable(
                name: "TenantPricingSummaries");

            migrationBuilder.DropTable(
                name: "UserAddOns");

            migrationBuilder.DropTable(
                name: "UserPricingTiers");
        }
    }
}
