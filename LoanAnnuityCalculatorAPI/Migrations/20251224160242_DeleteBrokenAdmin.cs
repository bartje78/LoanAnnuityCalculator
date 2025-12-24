using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class DeleteBrokenAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete the admin user with incorrect password hash
            migrationBuilder.Sql(@"
                DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE UserName = 'admin');
                DELETE FROM AspNetUsers WHERE UserName = 'admin';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback
        }
    }
}
