-- Multi-Tenancy Data Migration Script
-- Run this AFTER applying the AddMultiTenancy migration
-- This script sets up initial tenant, fund, and updates existing data

-- ==================================================================
-- STEP 1: CREATE DEFAULT TENANT
-- ==================================================================
INSERT INTO Tenants (Name, Description, DatabaseKey, IsActive, CreatedAt)
VALUES (
    'Default Tenant', 
    'Initial tenant created during migration', 
    lower(hex(randomblob(16))), -- SQLite way to generate unique key
    1, 
    datetime('now')
);

-- Get the tenant ID (SQLite uses last_insert_rowid())
-- Store in a variable for later use

-- ==================================================================
-- STEP 2: CREATE DEFAULT FUND
-- ==================================================================
INSERT INTO Funds (TenantId, Name, Description, FundCode, IsActive, CreatedAt)
SELECT 
    TenantId, 
    'Default Fund',
    'Initial fund created during migration',
    'DEFAULT',
    1,
    datetime('now')
FROM Tenants 
WHERE Name = 'Default Tenant';

-- ==================================================================
-- STEP 3: UPDATE EXISTING USERS
-- ==================================================================
-- Assign all existing users to the default tenant
UPDATE AspNetUsers
SET TenantId = (SELECT TenantId FROM Tenants WHERE Name = 'Default Tenant')
WHERE TenantId IS NULL;

-- ==================================================================
-- STEP 4: UPDATE EXISTING DEBTORS
-- ==================================================================
-- Assign all existing debtors to default tenant and fund
UPDATE DebtorDetails
SET 
    TenantId = (SELECT TenantId FROM Tenants WHERE Name = 'Default Tenant'),
    FundId = (SELECT FundId FROM Funds WHERE FundCode = 'DEFAULT')
WHERE TenantId = 0 OR TenantId IS NULL;

-- ==================================================================
-- STEP 5: UPDATE EXISTING LOANS
-- ==================================================================
-- Assign all existing loans to default tenant and fund
UPDATE Loans
SET 
    TenantId = (SELECT TenantId FROM Tenants WHERE Name = 'Default Tenant'),
    FundId = (SELECT FundId FROM Funds WHERE FundCode = 'DEFAULT')
WHERE TenantId = 0 OR TenantId IS NULL;

-- ==================================================================
-- STEP 6: UPDATE EXISTING COLLATERAL
-- ==================================================================
-- Assign all existing collateral to default tenant and fund
UPDATE Collaterals
SET 
    TenantId = (SELECT TenantId FROM Tenants WHERE Name = 'Default Tenant'),
    FundId = (SELECT FundId FROM Funds WHERE FundCode = 'DEFAULT')
WHERE TenantId = 0 OR TenantId IS NULL;

-- ==================================================================
-- STEP 7: GRANT ALL USERS ACCESS TO DEFAULT FUND
-- ==================================================================
-- Give all existing users Manager access to the default fund
INSERT INTO UserFundAccesses (UserId, FundId, Role, GrantedAt)
SELECT 
    u.Id,
    f.FundId,
    'Manager',
    datetime('now')
FROM AspNetUsers u
CROSS JOIN Funds f
WHERE f.FundCode = 'DEFAULT'
AND NOT EXISTS (
    SELECT 1 FROM UserFundAccesses ufa 
    WHERE ufa.UserId = u.Id AND ufa.FundId = f.FundId
);

-- ==================================================================
-- STEP 8: VERIFICATION QUERIES
-- ==================================================================
-- Run these to verify the migration was successful

-- Check tenant creation
SELECT 'Tenants Created' as Step, COUNT(*) as Count FROM Tenants;

-- Check fund creation
SELECT 'Funds Created' as Step, COUNT(*) as Count FROM Funds;

-- Check users assigned to tenant
SELECT 'Users with Tenant' as Step, COUNT(*) as Count 
FROM AspNetUsers WHERE TenantId IS NOT NULL;

-- Check debtors assigned
SELECT 'Debtors Migrated' as Step, COUNT(*) as Count 
FROM DebtorDetails WHERE TenantId IS NOT NULL AND FundId IS NOT NULL;

-- Check loans assigned
SELECT 'Loans Migrated' as Step, COUNT(*) as Count 
FROM Loans WHERE TenantId IS NOT NULL AND FundId IS NOT NULL;

-- Check collateral assigned
SELECT 'Collateral Migrated' as Step, COUNT(*) as Count 
FROM Collaterals WHERE TenantId IS NOT NULL AND FundId IS NOT NULL;

-- Check fund access granted
SELECT 'Fund Access Granted' as Step, COUNT(*) as Count 
FROM UserFundAccesses;

-- ==================================================================
-- OPTIONAL: CREATE ADDITIONAL TENANTS/FUNDS
-- ==================================================================
-- Example: Create a second tenant with two funds

/*
-- Create Asset Manager ABC
INSERT INTO Tenants (Name, Description, DatabaseKey, IsActive, CreatedAt)
VALUES (
    'Asset Manager ABC', 
    'Multi-fund real estate investor', 
    lower(hex(randomblob(16))),
    1, 
    datetime('now')
);

-- Create Fund 1 for ABC
INSERT INTO Funds (TenantId, Name, Description, FundCode, IsActive, CreatedAt)
SELECT 
    TenantId, 
    'ABC Real Estate Fund I',
    'First real estate fund',
    'ABC-RE-I',
    1,
    datetime('now')
FROM Tenants 
WHERE Name = 'Asset Manager ABC';

-- Create Fund 2 for ABC
INSERT INTO Funds (TenantId, Name, Description, FundCode, IsActive, CreatedAt)
SELECT 
    TenantId, 
    'ABC Commercial Fund',
    'Commercial property fund',
    'ABC-COM',
    1,
    datetime('now')
FROM Tenants 
WHERE Name = 'Asset Manager ABC';

-- Create a user for ABC (you'll need to create via API with password hashing)
-- Then grant fund access:
INSERT INTO UserFundAccesses (UserId, FundId, Role, GrantedAt)
VALUES (
    '[USER_ID_HERE]',
    (SELECT FundId FROM Funds WHERE FundCode = 'ABC-RE-I'),
    'Manager',
    datetime('now')
);
*/

-- ==================================================================
-- NOTES
-- ==================================================================
-- 1. This script assumes SQLite database
-- 2. For production, consider backing up database first
-- 3. The DatabaseKey is randomly generated for each tenant
-- 4. All existing data is assigned to "Default Tenant" / "Default Fund"
-- 5. All existing users get Manager access to Default Fund
-- 6. New tenants/funds should be created via API to ensure proper setup
-- 7. After migration, existing users will need to be assigned to proper tenants
--    and granted access to appropriate funds
