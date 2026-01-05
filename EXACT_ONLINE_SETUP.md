# Exact Online API Configuration Guide

## Overview
This guide explains how to securely configure Exact Online API credentials for both local development and Azure production environments.

## 1. Local Development Setup

### Step 1: Add Your Credentials
Edit `LoanAnnuityCalculatorAPI/appsettings.local.json` (this file is excluded from Git):

```json
{
  "ExactOnline": {
    "ClientId": "YOUR_CLIENT_ID_HERE",
    "ClientSecret": "YOUR_CLIENT_SECRET_HERE"
  }
}
```

**Replace `YOUR_CLIENT_ID_HERE` and `YOUR_CLIENT_SECRET_HERE` with your actual credentials.**

### Step 2: Verify .gitignore
The `.gitignore` file already includes:
- `appsettings.local.json` - Your local development secrets
- `appsettings.Production.json` - Production configuration
- `appsettings-azure.json` - Azure-specific settings

**These files will NEVER be committed to Git.**

### Step 3: Test Locally
Start your backend:
```bash
cd LoanAnnuityCalculatorAPI
dotnet run
```

The application will load settings in this order:
1. `appsettings.json` (base configuration)
2. `appsettings.Development.json` (development overrides)
3. `appsettings.local.json` (your local secrets - NOT in Git)
4. Environment variables (highest priority)

## 2. Azure Production Setup

Since you deploy via Git/GitHub Actions, you need to configure secrets in Azure App Service.

### Step 1: Set Application Settings in Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your API App Service: `loancalculator-api-loanportal`
3. In the left menu, select **Configuration** → **Application settings**
4. Click **+ New application setting** and add:

   **Setting 1:**
   - Name: `ExactOnline__ClientId`
   - Value: `YOUR_ACTUAL_CLIENT_ID`
   
   **Setting 2:**
   - Name: `ExactOnline__ClientSecret`
   - Value: `YOUR_ACTUAL_CLIENT_SECRET`

   **Note:** Use double underscores `__` to represent nested JSON configuration

5. Click **Save** at the top
6. Click **Continue** to restart the app

### Step 2: Verify Configuration

Your Azure app will now use:
1. `appsettings.json` (from Git)
2. `appsettings.Production.json` (if you create one)
3. Azure Application Settings (highest priority - overrides everything)

### Alternative: Using Azure CLI

```bash
# Set Client ID
az webapp config appsettings set \
  --name loancalculator-api-loanportal \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings ExactOnline__ClientId="YOUR_ACTUAL_CLIENT_ID"

# Set Client Secret
az webapp config appsettings set \
  --name loancalculator-api-loanportal \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings ExactOnline__ClientSecret="YOUR_ACTUAL_CLIENT_SECRET"
```

## 3. GitHub Actions (Optional)

If you want to automate configuration during deployment, add GitHub Secrets:

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add secrets:
   - `EXACT_CLIENT_ID`
   - `EXACT_CLIENT_SECRET`

Then update your workflow to deploy these as app settings.

## 4. Exact Online Registration Details

**Redirect URI (already configured):**
```
https://loancalculator-api-loanportal.azurewebsites.net/api/exact/callback
```

**Register your app at:**
https://apps.exactonline.com/

## 5. Configuration Hierarchy

The application loads configuration in this order (later sources override earlier ones):

1. `appsettings.json` - Base config (committed to Git)
2. `appsettings.{Environment}.json` - Environment-specific (Development/Production)
3. `appsettings.local.json` - Local secrets (NOT in Git)
4. Azure Application Settings - Production secrets (NOT in Git)
5. Environment variables - Can override everything

## 6. Security Checklist

✅ `appsettings.local.json` is in `.gitignore`
✅ Never commit actual credentials to Git
✅ Azure Application Settings are used for production
✅ Redirect URI matches production backend URL
✅ Client Secret is kept secure and not shared

## 7. Testing the Integration

After configuration, test the Exact Online integration:

1. Start your local backend and frontend
2. Navigate to Settings → Integrations (when UI is ready)
3. Click "Connect to Exact Online"
4. You should be redirected to Exact's login page
5. After authorization, you'll be redirected back with a success message

## Need Help?

If you encounter issues:
1. Check Azure App Service logs
2. Verify Application Settings in Azure Portal
3. Ensure redirect URI matches exactly
4. Check that credentials are active in Exact Online portal
