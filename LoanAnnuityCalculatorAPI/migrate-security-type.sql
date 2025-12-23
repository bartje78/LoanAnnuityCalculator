-- Copy SecurityType from Collateral to Loan
-- For loans with multiple collaterals, use the first one (ordered by Priority if available)

UPDATE Loans
SET SecurityType = (
    SELECT TOP 1 c.SecurityType
    FROM Collaterals c
    INNER JOIN LoanCollaterals lc ON c.CollateralId = lc.CollateralId
    WHERE lc.LoanId = Loans.Id
    AND c.SecurityType IS NOT NULL
    AND c.SecurityType != ''
    ORDER BY lc.Priority ASC, lc.AssignedDate ASC
)
WHERE EXISTS (
    SELECT 1 
    FROM Collaterals c
    INNER JOIN LoanCollaterals lc ON c.CollateralId = lc.CollateralId
    WHERE lc.LoanId = Loans.Id
    AND c.SecurityType IS NOT NULL
    AND c.SecurityType != ''
);
