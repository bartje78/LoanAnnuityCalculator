-- First, ensure roles exist
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

-- Check if admin user exists
IF NOT EXISTS (SELECT * FROM AspNetUsers WHERE UserName = 'admin')
BEGIN
    DECLARE @UserId UNIQUEIDENTIFIER = NEWID();
    DECLARE @AdminRoleId UNIQUEIDENTIFIER;
    
    -- Get Admin role ID
    SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';
    
    -- Create admin user
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
    
    -- Add user to Admin role
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@UserId, @AdminRoleId);
    
    PRINT 'Admin user created successfully';
END
ELSE
BEGIN
    PRINT 'Admin user already exists';
END
