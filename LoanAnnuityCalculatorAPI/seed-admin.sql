-- Check if admin user already exists
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE UserName = 'admin')
BEGIN
    DECLARE @AdminUserId NVARCHAR(450) = NEWID();
    DECLARE @AdminRoleId NVARCHAR(450);
    
    -- Insert admin user
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
    VALUES (@AdminUserId, 'admin', 'ADMIN', 'admin@example.com', 'ADMIN@EXAMPLE.COM', 1, 'AQAAAAIAAYagAAAAEFkJ8qHPPJFJIK3PXVZ3tLrCJfYpP9VHtP7L3qLCjP0pCFnFtJXqDKvPJ7fLYNqwHQ==', NEWID(), NEWID(), 0, 0, 1, 0);
    
    -- Get Admin role ID
    SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';
    
    -- Assign Admin role to user
    IF @AdminRoleId IS NOT NULL
    BEGIN
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        VALUES (@AdminUserId, @AdminRoleId);
    END
    
    PRINT 'Admin user created successfully';
END
ELSE
BEGIN
    PRINT 'Admin user already exists';
END
