using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanAnnuityCalculatorAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed roles
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Admin')
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());
                END

                IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'RiskManager')
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (NEWID(), 'RiskManager', 'RISKMANAGER', NEWID());
                END

                IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Viewer')
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (NEWID(), 'Viewer', 'VIEWER', NEWID());
                END
            ");

            // Seed admin user
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM AspNetUsers WHERE UserName = 'admin')
                BEGIN
                    DECLARE @UserId UNIQUEIDENTIFIER = NEWID();
                    DECLARE @AdminRoleId UNIQUEIDENTIFIER;
                    
                    SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';
                    
                    INSERT INTO AspNetUsers (
                        Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
                        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, 
                        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
                        FirstName, LastName, IsActive
                    )
                    VALUES (
                        @UserId, 'admin', 'ADMIN', 'admin@loanannuity.local', 'ADMIN@LOANANNUITY.LOCAL',
                        1, 'AQAAAAIAAYagAAAAEFkJ8qHPPJFJIK3PXVZ3tLrCJfYpP9VHtP7L3qLCjP0pCFnFtJXqDKvPJ7fLYNqwHQ==',
                        NEWID(), NEWID(), 0, 0, 1, 0,
                        'Admin', 'User', 1
                    );
                    
                    INSERT INTO AspNetUserRoles (UserId, RoleId)
                    VALUES (@UserId, @AdminRoleId);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM AspNetUsers WHERE UserName = 'admin'");
            migrationBuilder.Sql("DELETE FROM AspNetRoles WHERE Name IN ('Admin', 'RiskManager', 'Viewer')");
        }
    }
}
