-- System Admin Management Script
-- This script helps manage system administrator privileges

-- ========================================
-- GRANT SYSTEM ADMIN PRIVILEGES
-- ========================================

-- Make a user a system admin (replace the email)
-- UPDATE AspNetUsers SET IsSystemAdmin = 1 WHERE Email = 'user@example.com';

-- Example: Make current admin a system admin
UPDATE AspNetUsers SET IsSystemAdmin = 1 WHERE Email = 'admin@loanannuity.local';

-- ========================================
-- REVOKE SYSTEM ADMIN PRIVILEGES
-- ========================================

-- Remove system admin privileges from a user (replace the email)
-- UPDATE AspNetUsers SET IsSystemAdmin = 0 WHERE Email = 'user@example.com';

-- ========================================
-- VIEW SYSTEM ADMINS
-- ========================================

-- List all system administrators
SELECT 
    Id,
    Email,
    FirstName || ' ' || LastName as FullName,
    TenantId,
    IsSystemAdmin,
    IsActive
FROM AspNetUsers 
WHERE IsSystemAdmin = 1
ORDER BY Email;

-- ========================================
-- VIEW ALL USERS WITH ADMIN STATUS
-- ========================================

-- List all users showing their system admin status
SELECT 
    Id,
    Email,
    FirstName || ' ' || LastName as FullName,
    TenantId,
    IsSystemAdmin,
    IsActive
FROM AspNetUsers 
ORDER BY IsSystemAdmin DESC, Email;

-- ========================================
-- SECURITY NOTES
-- ========================================
-- 1. System admins have access to ALL tenant data
-- 2. System admins can manage payment plans and subscriptions
-- 3. System admins can view the admin dashboard
-- 4. Always verify system admin assignments in production
-- 5. Limit the number of system admins to minimum required
-- 6. When a user logs in, a new JWT token will be issued with the IsSystemAdmin claim
-- 7. Users need to log out and log back in after privilege changes
