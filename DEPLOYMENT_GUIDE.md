# Azure Deployment Guide

## Your Configuration
- **Your IP**: 195.241.23.107
- **Exact Online IPs**: 185.23.126.0/24, 185.23.127.0/24 (Netherlands)
- **Resource Group**: LoanCalculatorRG
- **Location**: West Europe

## Option 1: Automated Deployment (Run the script)

```bash
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculator"
./deploy-to-azure.sh
```

The script will prompt you for:
1. SQL Server admin password
2. JWT Secret (generate with: `openssl rand -base64 64`)

## Option 2: Manual Step-by-Step Deployment

### Step 1: Create Resource Group
```bash
az group create --name LoanCalculatorRG --location westeurope
```

### Step 2: Create App Service Plan
```bash
az appservice plan create \
  --name LoanCalculatorPlan \
  --resource-group LoanCalculatorRG \
  --location westeurope \
  --sku B1 \
  --is-linux
```

### Step 3: Create Backend Web App
```bash
az webapp create \
  --name loancalculator-api-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --plan LoanCalculatorPlan \
  --runtime "DOTNETCORE:8.0"
```

### Step 4: Configure IP Restrictions (Backend)
```bash
# Your IP
az webapp config access-restriction add \
  --name loancalculator-api-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --rule-name "AllowMyIP" \
  --action Allow \
  --ip-address "195.241.23.107/32" \
  --priority 100

# Exact Online Netherlands
az webapp config access-restriction add \
  --name loancalculator-api-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --rule-name "AllowExactOnline1" \
  --action Allow \
  --ip-address "185.23.126.0/24" \
  --priority 200

az webapp config access-restriction add \
  --name loancalculator-api-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --rule-name "AllowExactOnline2" \
  --action Allow \
  --ip-address "185.23.127.0/24" \
  --priority 201
```

### Step 5: Create Azure SQL Database
```bash
# Create SQL Server
az sql server create \
  --name loancalculator-sql-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --location westeurope \
  --admin-user sqladmin \
  --admin-password "YOUR-STRONG-PASSWORD"

# Allow Azure services
az sql server firewall-rule create \
  --resource-group LoanCalculatorRG \
  --server loancalculator-sql-YOUR-SUFFIX \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Allow your IP
az sql server firewall-rule create \
  --resource-group LoanCalculatorRG \
  --server loancalculator-sql-YOUR-SUFFIX \
  --name AllowMyIP \
  --start-ip-address 195.241.23.107 \
  --end-ip-address 195.241.23.107

# Create database
az sql db create \
  --resource-group LoanCalculatorRG \
  --server loancalculator-sql-YOUR-SUFFIX \
  --name LoanCalculatorDB \
  --service-objective S0
```

### Step 6: Configure App Settings
```bash
JWT_SECRET=$(openssl rand -base64 64)

az webapp config appsettings set \
  --name loancalculator-api-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    "ConnectionStrings__DefaultConnection=Server=tcp:loancalculator-sql-YOUR-SUFFIX.database.windows.net,1433;Initial Catalog=LoanCalculatorDB;User ID=sqladmin;Password=YOUR-STRONG-PASSWORD;Encrypt=True;" \
    "JwtSettings__SecretKey=$JWT_SECRET" \
    "JwtSettings__ExpirationMinutes=30" \
    "RateLimiting__PermitLimit=100" \
    "RateLimiting__Window=60" \
    "RateLimiting__AuthPermitLimit=5"
```

### Step 7: Deploy Backend
```bash
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculator/LoanAnnuityCalculatorAPI"
dotnet publish -c Release -o ./publish
cd publish
zip -r ../deploy.zip .
cd ..

az webapp deployment source config-zip \
  --name loancalculator-api-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --src deploy.zip
```

### Step 8: Run Migrations
```bash
dotnet ef database update --connection "Server=tcp:loancalculator-sql-YOUR-SUFFIX.database.windows.net,1433;Initial Catalog=LoanCalculatorDB;User ID=sqladmin;Password=YOUR-STRONG-PASSWORD;Encrypt=True;"
```

### Step 9: Test Backend
```bash
curl https://loancalculator-api-YOUR-SUFFIX.azurewebsites.net/api/health
```

### Step 10: Create Frontend Web App
```bash
az webapp create \
  --name loancalculator-frontend-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --plan LoanCalculatorPlan \
  --runtime "NODE:20-lts"
```

### Step 11: Configure IP Restrictions (Frontend)
```bash
az webapp config access-restriction add \
  --name loancalculator-frontend-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --rule-name "AllowMyIP" \
  --action Allow \
  --ip-address "195.241.23.107/32" \
  --priority 100
```

### Step 12: Update Frontend Config & Deploy
```bash
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculatorApp"

# Update proxy config to point to Azure
cat > proxy.conf.json << 'EOF'
{
  "/api": {
    "target": "https://loancalculator-api-YOUR-SUFFIX.azurewebsites.net",
    "secure": true,
    "changeOrigin": true
  }
}
EOF

# Build
npm install
npm run build -- --configuration production

# Deploy
cd dist/loan-annuity-calculator-app/browser
zip -r ../../../frontend-deploy.zip .
cd ../../..

az webapp deployment source config-zip \
  --name loancalculator-frontend-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --src frontend-deploy.zip
```

### Step 13: Update Exact Online Settings
```bash
az webapp config appsettings set \
  --name loancalculator-api-YOUR-SUFFIX \
  --resource-group LoanCalculatorRG \
  --settings \
    "ExactOnline__ClientId=YOUR-EXACT-CLIENT-ID" \
    "ExactOnline__ClientSecret=YOUR-EXACT-CLIENT-SECRET" \
    "ExactOnline__RedirectUri=https://loancalculator-api-YOUR-SUFFIX.azurewebsites.net/api/exact/callback" \
    "AllowedOrigins__0=https://loancalculator-frontend-YOUR-SUFFIX.azurewebsites.net"
```

## Important Notes

1. **Replace YOUR-SUFFIX** with something unique (e.g., your initials + random numbers)
2. **Exact Online IP Ranges**: The ranges 185.23.126.0/24 and 185.23.127.0/24 are for Netherlands. If you use a different region, you'll need to check Exact's documentation.
3. **Update Exact Online App**: After deployment, update your Exact Online app settings with the new redirect URI
4. **SQL Security**: Store SQL password securely
5. **JWT Secret**: Generate with `openssl rand -base64 64`

## Quick Deployment (Recommended)

If you want me to run the automated script, I can do that. Just confirm:
- Do you want to use the automated script?
- What suffix do you want for your app names? (e.g., "bk2024" would create loancalculator-api-bk2024)
- What SQL admin password do you want to use?

## After Deployment Checklist

- [ ] Backend URL: https://loancalculator-api-YOUR-SUFFIX.azurewebsites.net
- [ ] Frontend URL: https://loancalculator-frontend-YOUR-SUFFIX.azurewebsites.net
- [ ] Update Exact Online redirect URI
- [ ] Test login from your IP
- [ ] Test Exact Online OAuth flow
- [ ] Verify IP restrictions are working (try from different network - should be blocked)
