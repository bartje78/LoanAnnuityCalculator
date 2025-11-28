-- Seed default payment plans
-- Run this SQL to populate the PaymentPlans table

-- Delete existing plans (optional - comment out if you want to keep existing data)
-- DELETE FROM PaymentPlans;

-- Insert Free Plan
INSERT INTO PaymentPlans (Name, Description, MonthlyPrice, AnnualPrice, MaxUsers, MaxFunds, MaxDebtors, MaxLoans, StorageLimitMB, 
    AllowMonteCarloSimulation, AllowPortfolioAnalysis, AllowReporting, AllowExport, AllowImport, AllowApiAccess, AllowCustomBranding, 
    AllowAdvancedAnalytics, AllowMultipleFunds, SupportLevel, IsActive, IsPublic, DisplayOrder, CreatedAt)
VALUES ('Free', 'Perfect for trying out the platform', 0, 0, 2, 1, 25, 100, 500, 
    0, 1, 1, 0, 1, 0, 0, 0, 0, 'Email', 1, 1, 1, datetime('now'));

-- Insert Starter Plan
INSERT INTO PaymentPlans (Name, Description, MonthlyPrice, AnnualPrice, MaxUsers, MaxFunds, MaxDebtors, MaxLoans, StorageLimitMB, 
    AllowMonteCarloSimulation, AllowPortfolioAnalysis, AllowReporting, AllowExport, AllowImport, AllowApiAccess, AllowCustomBranding, 
    AllowAdvancedAnalytics, AllowMultipleFunds, SupportLevel, IsActive, IsPublic, DisplayOrder, CreatedAt)
VALUES ('Starter', 'Great for small teams managing a single fund', 99, 990, 5, 3, 100, 500, 2000, 
    1, 1, 1, 1, 1, 0, 0, 0, 1, 'Email', 1, 1, 2, datetime('now'));

-- Insert Professional Plan
INSERT INTO PaymentPlans (Name, Description, MonthlyPrice, AnnualPrice, MaxUsers, MaxFunds, MaxDebtors, MaxLoans, StorageLimitMB, 
    AllowMonteCarloSimulation, AllowPortfolioAnalysis, AllowReporting, AllowExport, AllowImport, AllowApiAccess, AllowCustomBranding, 
    AllowAdvancedAnalytics, AllowMultipleFunds, SupportLevel, IsActive, IsPublic, DisplayOrder, CreatedAt)
VALUES ('Professional', 'For growing asset managers with multiple funds', 299, 2990, 15, 10, 500, 2500, 10000, 
    1, 1, 1, 1, 1, 1, 0, 1, 1, 'Priority', 1, 1, 3, datetime('now'));

-- Insert Enterprise Plan
INSERT INTO PaymentPlans (Name, Description, MonthlyPrice, AnnualPrice, MaxUsers, MaxFunds, MaxDebtors, MaxLoans, StorageLimitMB, 
    AllowMonteCarloSimulation, AllowPortfolioAnalysis, AllowReporting, AllowExport, AllowImport, AllowApiAccess, AllowCustomBranding, 
    AllowAdvancedAnalytics, AllowMultipleFunds, SupportLevel, IsActive, IsPublic, DisplayOrder, CreatedAt)
VALUES ('Enterprise', 'Unlimited scale for large organizations', 999, 9990, 100, 50, 10000, 50000, 100000, 
    1, 1, 1, 1, 1, 1, 1, 1, 1, '24/7', 1, 1, 4, datetime('now'));

-- Verify the data
SELECT * FROM PaymentPlans ORDER BY DisplayOrder;
