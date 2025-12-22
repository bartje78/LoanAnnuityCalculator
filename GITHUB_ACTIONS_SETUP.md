# GitHub Actions Deployment Setup

This guide will help you set up automated deployment using GitHub Actions.

---

## Step 1: Get Azure Publish Profiles

### Backend Publish Profile
```bash
az webapp deployment list-publishing-profiles \
  --name loancalculator-api-loanportal \
  --resource-group LoanCalculatorRG \
  --xml > backend-publish-profile.xml
```

### Frontend Publish Profile
```bash
az webapp deployment list-publishing-profiles \
  --name loancalculator-frontend-loanportal \
  --resource-group LoanCalculatorRG \
  --xml > frontend-publish-profile.xml
```

---

## Step 2: Create GitHub Repository

1. Go to GitHub and create a new repository (e.g., `LoanAnnuityCalculator`)
2. Initialize git in your local project:

```bash
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculator"
git init
git add .
git commit -m "Initial commit with Azure deployment workflows"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/LoanAnnuityCalculator.git
git push -u origin main
```

---

## Step 3: Configure GitHub Secrets

Go to your GitHub repository:
**Settings → Secrets and variables → Actions → New repository secret**

Add these secrets:

### 1. AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND
- Copy the entire contents of `backend-publish-profile.xml`
- Paste as the secret value

### 2. AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND
- Copy the entire contents of `frontend-publish-profile.xml`
- Paste as the secret value

### 3. AZURE_SQL_CONNECTION_STRING
```
Server=tcp:loancalculator-sql-loanportal.database.windows.net,1433;Initial Catalog=LoanCalculatorDB;Persist Security Info=False;User ID=sqladmin;Password=Nappy_1978;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### 4. JWT_SECRET_KEY
```
pNzW28JL1MvuY0WpFzfbquIbHDaZZeZGYRfMuO/o0IJX6KEE6NXEBMThlVNVV5S65OdNORGZYF3JCGh1MjGE7A==
```

### 5. EXACT_ONLINE_CLIENT_ID (optional - add when ready)
```
YOUR_EXACT_ONLINE_CLIENT_ID
```

### 6. EXACT_ONLINE_CLIENT_SECRET (optional - add when ready)
```
YOUR_EXACT_ONLINE_CLIENT_SECRET
```

---

## Step 4: Trigger Deployment

### Automatic Deployment
- Push any changes to `main` or `master` branch
- Changes to backend code trigger backend deployment
- Changes to frontend code trigger frontend deployment

### Manual Deployment
1. Go to: **Actions** tab in your GitHub repository
2. Select the workflow (Deploy Backend or Deploy Frontend)
3. Click **Run workflow**

---

## Step 5: Monitor Deployment

1. Go to **Actions** tab in GitHub
2. Click on the running workflow
3. Watch the deployment progress
4. Check for any errors in the logs

---

## Workflow Files Created

### Backend: `.github/workflows/deploy-backend.yml`
- Builds .NET application
- Publishes to Azure App Service
- Configures all app settings
- Restarts the app

### Frontend: `.github/workflows/deploy-frontend.yml`
- Builds Angular application
- Deploys to Azure App Service
- Optimized for production

---

## Testing After Deployment

### Backend Health Check
```bash
curl https://loancalculator-api-loanportal.azurewebsites.net
```

### Frontend Check
```bash
curl https://loancalculator-frontend-loanportal.azurewebsites.net
```

---

## Adding OWASP ZAP Security Testing (Future)

We'll add this as an additional workflow step later:

```yaml
- name: OWASP ZAP Scan
  uses: zaproxy/action-baseline@v0.7.0
  with:
    target: 'https://loancalculator-api-loanportal.azurewebsites.net'
    rules_file_name: '.zap/rules.tsv'
    cmd_options: '-a'
```

---

## Troubleshooting

### Deployment Fails
1. Check GitHub Actions logs for specific errors
2. Verify all secrets are correctly set
3. Ensure publish profiles are valid
4. Check Azure App Service logs

### App Won't Start
1. Check App Settings in Azure Portal
2. Verify connection string is correct
3. Check application logs via Azure Portal or CLI

### IP Restrictions
Remember: Your app is restricted to:
- Your IP: 195.241.23.107
- Exact Online IPs: 185.23.126.0/24, 185.23.127.0/24

GitHub Actions deployment uses Azure IPs which are allowed for deployment.

---

## Quick Commands

### Get Publish Profiles
```bash
# Backend
az webapp deployment list-publishing-profiles --name loancalculator-api-loanportal --resource-group LoanCalculatorRG --xml

# Frontend  
az webapp deployment list-publishing-profiles --name loancalculator-frontend-loanportal --resource-group LoanCalculatorRG --xml
```

### View Recent Deployments
```bash
# Via Azure CLI
az webapp deployment list --name loancalculator-api-loanportal --resource-group LoanCalculatorRG

# Via GitHub
# Go to Actions tab → Select workflow → View run
```

### Manual Trigger
```bash
# Via GitHub CLI (if installed)
gh workflow run deploy-backend.yml
gh workflow run deploy-frontend.yml
```

---

## Cost Optimization

GitHub Actions provides:
- 2,000 minutes/month free for private repos
- Unlimited minutes for public repos
- These deployments typically use < 5 minutes each

---

## Next Steps

1. ✅ Get publish profiles
2. ✅ Create GitHub repository
3. ✅ Configure GitHub secrets
4. ✅ Push code to trigger deployment
5. ⏳ Add OWASP ZAP testing
6. ⏳ Add automated tests
7. ⏳ Set up staging environment
