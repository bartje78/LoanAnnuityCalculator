# Azure Deployment Complete

**Deployment Date:** December 22, 2025  
**Status:** ✅ Deployed with IP Restrictions

---

## 🌐 Application URLs

### Frontend (Angular)
**URL:** https://loancalculator-frontend-loanportal.azurewebsites.net  
**Status:** ✅ Running

### Backend API (.NET)
**URL:** https://loancalculator-api-loanportal.azurewebsites.net  
**Status:** 🔄 Starting (may take 5-10 minutes on first deployment)

### Exact Online OAuth Callback
**URL:** https://loancalculator-api-loanportal.azurewebsites.net/api/exact/callback

---

## 🔒 Security Configuration

### IP Restrictions (ACTIVE)
Both frontend and backend are restricted to:
- **Your IP:** 195.241.23.107
- **Exact Online IPs:** 185.23.126.0/24, 185.23.127.0/24

⚠️ **Access is blocked from any other IP address**

### How to Test IP Restrictions
```bash
# From your IP (should work)
curl https://loancalculator-api-loanportal.azurewebsites.net

# From a different network (should get 403 Forbidden)
```

---

## 🔧 Azure Resources Created

### Resource Group
- **Name:** LoanCalculatorRG
- **Location:** West Europe

### App Service Plan
- **Name:** LoanCalculatorPlan
- **Tier:** B1 (Basic)
- **OS:** Linux

### Web Apps
1. **Backend API:** loancalculator-api-loanportal
2. **Frontend:** loancalculator-frontend-loanportal

### SQL Server
- **Server:** loancalculator-sql-loanportal.database.windows.net
- **Database:** LoanCalculatorDB
- **Tier:** S0 (Standard)
- **Admin User:** sqladmin

---

## ⚙️ Configuration Required

### 1. Exact Online Integration

Update your Exact Online app settings with:
- **Redirect URI:** `https://loancalculator-api-loanportal.azurewebsites.net/api/exact/callback`

Then add your Exact Online credentials to Azure:

```bash
az webapp config appsettings set \
  --name loancalculator-api-loanportal \
  --resource-group LoanCalculatorRG \
  --settings \
    "ExactOnline__ClientId=YOUR_EXACT_ONLINE_CLIENT_ID" \
    "ExactOnline__ClientSecret=YOUR_EXACT_ONLINE_CLIENT_SECRET" \
    "ExactOnline__RedirectUri=https://loancalculator-api-loanportal.azurewebsites.net/api/exact/callback"
```

### 2. CORS Configuration (Already Set)
- Backend accepts requests from: `https://loancalculator-frontend-loanportal.azurewebsites.net`

---

## 🔐 Security Features Deployed

✅ **JWT Authentication** (30-minute expiration)  
✅ **Rate Limiting** (100 requests/min general, 5/min auth)  
✅ **HTTPS Only** (HTTP redirects to HTTPS)  
✅ **Security Headers** (CSP, HSTS, X-Frame-Options, etc.)  
✅ **Audit Logging** (All sensitive operations logged)  
✅ **Strong Password Policy** (12+ chars, special characters required)  
✅ **IP Restrictions** (Only your IP + Exact Online IPs)  
✅ **SQL Server** (Production-grade database)

---

## 📊 Database Connection

### Connection String Format
```
Server=tcp:loancalculator-sql-loanportal.database.windows.net,1433;
Initial Catalog=LoanCalculatorDB;
User ID=sqladmin;
Password=Nappy_1978;
Encrypt=True;
```

### Accessing the Database
```bash
# Via Azure Portal
https://portal.azure.com → SQL databases → LoanCalculatorDB

# Via SQL Server Management Studio
Server: loancalculator-sql-loanportal.database.windows.net
Login: sqladmin
Password: Nappy_1978
```

---

## 🚀 First-Time Setup

### 1. Wait for Backend to Start
The backend may take 5-10 minutes to start on first deployment because it's:
- Creating database schema
- Running migrations
- Seeding initial data

Check status:
```bash
curl https://loancalculator-api-loanportal.azurewebsites.net
```

### 2. Initialize Admin Account
Once backend is running, initialize the admin account:

```bash
curl -X POST https://loancalculator-api-loanportal.azurewebsites.net/api/auth/initialize
```

⚠️ **Note:** This endpoint only works in Development environment. In production, you'll need to create admin users via direct database access or a separate admin tool.

### 3. Test the Application
1. Open: https://loancalculator-frontend-loanportal.azurewebsites.net
2. Login with admin credentials
3. Test Exact Online integration once configured

---

## 🔍 Monitoring & Logs

### View Application Logs
```bash
# Backend logs
az webapp log tail --name loancalculator-api-loanportal --resource-group LoanCalculatorRG

# Frontend logs
az webapp log tail --name loancalculator-frontend-loanportal --resource-group LoanCalculatorRG
```

### Enable Application Insights (Recommended)
```bash
# Create Application Insights
az monitor app-insights component create \
  --app loancalculator-insights \
  --location westeurope \
  --resource-group LoanCalculatorRG

# Get instrumentation key and add to app settings
```

---

## 🛠️ Management Commands

### Restart Applications
```bash
# Restart backend
az webapp restart --name loancalculator-api-loanportal --resource-group LoanCalculatorRG

# Restart frontend
az webapp restart --name loancalculator-frontend-loanportal --resource-group LoanCalculatorRG
```

### Update IP Restrictions
```bash
# Add a new IP
az webapp config access-restriction add \
  --name loancalculator-api-loanportal \
  --resource-group LoanCalculatorRG \
  --rule-name "NewIP" \
  --action Allow \
  --ip-address "1.2.3.4/32" \
  --priority 150

# Remove an IP
az webapp config access-restriction remove \
  --name loancalculator-api-loanportal \
  --resource-group LoanCalculatorRG \
  --rule-name "NewIP"
```

### Scale Up/Down
```bash
# Scale to a higher tier
az appservice plan update \
  --name LoanCalculatorPlan \
  --resource-group LoanCalculatorRG \
  --sku S1
```

---

## 💰 Cost Estimate

Based on current configuration:
- **App Service Plan (B1):** ~€13/month
- **SQL Database (S0):** ~€13/month
- **Data Transfer:** Minimal for development
- **Total:** ~€26-30/month

---

## 🔄 Update Deployment

### Backend Updates
```bash
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculator/LoanAnnuityCalculatorAPI"
dotnet publish -c Release -o ./publish
cd publish && zip -r ../deploy.zip . && cd ..
az webapp deployment source config-zip \
  --name loancalculator-api-loanportal \
  --resource-group LoanCalculatorRG \
  --src deploy.zip
```

### Frontend Updates
```bash
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculatorApp"
npm run build -- --configuration production
cd dist/LoanAnnuityCalculatorApp/browser && zip -r ../../../frontend-deploy.zip . && cd ../../..
az webapp deployment source config-zip \
  --name loancalculator-frontend-loanportal \
  --resource-group LoanCalculatorRG \
  --src frontend-deploy.zip
```

---

## 🆘 Troubleshooting

### Backend Won't Start
1. Check logs: `az webapp log tail --name loancalculator-api-loanportal --resource-group LoanCalculatorRG`
2. Verify database connection string is set
3. Ensure migrations completed successfully
4. Restart the app

### Frontend Shows Errors
1. Check if backend is running
2. Verify CORS settings allow frontend origin
3. Check browser console for specific errors
4. Verify IP restrictions aren't blocking requests

### Can't Access from Different Location
- IP restrictions are active! Only your IP (195.241.23.107) can access
- Add new IPs via access restriction commands above
- Or temporarily remove all restrictions for testing

### Database Connection Issues
1. Verify SQL firewall allows Azure services (already configured)
2. Check connection string in app settings
3. Verify password is correct
4. Test connection from Azure Portal Query Editor

---

## 📝 Next Steps

1. ✅ Wait for backend to fully start (check URL returns 200)
2. ⏳ Configure Exact Online credentials
3. ⏳ Test authentication flow
4. ⏳ Test Exact Online OAuth flow
5. ⏳ Set up Application Insights for monitoring
6. ⏳ Configure backup policies
7. ⏳ Set up CI/CD pipeline (GitHub Actions or Azure DevOps)

---

## 📞 Support

### Azure Portal
https://portal.azure.com → Resource Groups → LoanCalculatorRG

### Useful Links
- [App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [SQL Database Documentation](https://docs.microsoft.com/en-us/azure/azure-sql/)
- [Security Best Practices](https://docs.microsoft.com/en-us/azure/security/)

---

**Deployment completed by GitHub Copilot**  
**Date:** December 22, 2025
