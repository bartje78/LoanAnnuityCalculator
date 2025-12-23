# GitHub Secrets Setup Guide

The deployment is failing because the GitHub secrets need to be configured correctly. Here's how to set them up:

## Backend Repository (LoanAnnuityCalculator)

Go to: https://github.com/bartje78/LoanAnnuityCalculator/settings/secrets/actions

### Required Secrets:

1. **AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND**
   - Run this command to get the content:
   ```bash
   az webapp deployment list-publishing-profiles \
     --name loancalculator-api-loanportal \
     --resource-group LoanCalculatorRG \
     --xml
   ```
   - Copy the ENTIRE XML output (including `<publishData>` tags)
   - Paste it as the secret value

2. **AZURE_SQL_CONNECTION_STRING**
   ```
   Server=tcp:loancalculator-sql-loanportal.database.windows.net,1433;Initial Catalog=LoanCalculatorDB;Persist Security Info=False;User ID=sqladmin;Password=Nappy_1978;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```

3. **JWT_SECRET_KEY**
   ```
   pNzW28JL1MvuY0WpFzfbquIbHDaZZeZGYRfMuO/o0IJX6KEE6NXEBMThlVNVV5S65OdNORGZYF3JCGh1MjGE7A==
   ```

## Frontend Repository (LoanAnnuityCalculatorApp)

Go to: https://github.com/bartje78/LoanAnnuityCalculatorApp/settings/secrets/actions

### Required Secrets:

1. **AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND**
   - Run this command to get the content:
   ```bash
   az webapp deployment list-publishing-profiles \
     --name loancalculator-frontend-loanportal \
     --resource-group LoanCalculatorRG \
     --xml
   ```
   - Copy the ENTIRE XML output (including `<publishData>` tags)
   - Paste it as the secret value

## Important Notes:

- Make sure to copy the **ENTIRE** XML content from the terminal, not from the files (they're redacted)
- The XML should be all on one line or with its original formatting
- Don't add extra spaces or newlines when pasting
- After adding/updating secrets, re-run the workflows manually

## Testing the Secrets:

After adding the secrets, trigger the workflows manually:

### Backend:
```bash
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculator"
gh workflow run deploy-backend.yml
```

### Frontend:
```bash
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculatorApp"
gh workflow run deploy-frontend.yml
```

Check deployment status:
```bash
# For backend
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculator"
gh run list --workflow="deploy-backend.yml" --limit 1

# For frontend
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculatorApp"
gh run list --workflow="deploy-frontend.yml" --limit 1
```
