#!/bin/bash

# Azure Deployment Script for Loan Annuity Calculator
# This script deploys both backend API and frontend to Azure with IP restrictions

set -e  # Exit on error

# Configuration
RESOURCE_GROUP="LoanCalculatorRG"
LOCATION="westeurope"
APP_SERVICE_PLAN="LoanCalculatorPlan"
BACKEND_APP_NAME="loancalculator-api"
FRONTEND_APP_NAME="loancalculator-frontend"
SQL_SERVER_NAME="loancalculator-sql"
SQL_DB_NAME="LoanCalculatorDB"
SQL_ADMIN_USER="sqladmin"

# Get user's IP address
USER_IP=$(curl -s https://api.ipify.org)
echo "Your IP address: $USER_IP"

# Exact Online IP ranges (these are for Netherlands - adjust if needed)
# Note: Exact Online uses multiple IPs and ranges. These are common ones.
EXACT_IPS="185.23.126.0/24 185.23.127.0/24"

echo "========================================="
echo "Azure Deployment for Loan Calculator"
echo "========================================="
echo ""
echo "This script will:"
echo "1. Create a resource group"
echo "2. Create an App Service Plan (B1 tier)"
echo "3. Deploy the backend API"
echo "4. Create Azure SQL Database"
echo "5. Configure IP restrictions"
echo "6. Deploy the frontend"
echo ""

# Prompt for SQL password
read -sp "Enter SQL Server admin password (min 8 chars, uppercase, lowercase, number, special char): " SQL_ADMIN_PASSWORD
echo ""

# Prompt for JWT secret
read -sp "Enter JWT Secret (64+ characters recommended): " JWT_SECRET
echo ""

# Create resource group
echo ""
echo "Creating resource group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create App Service Plan
echo ""
echo "Creating App Service Plan (B1 tier)..."
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku B1 \
  --is-linux

# Create Azure SQL Server
echo ""
echo "Creating Azure SQL Server..."
az sql server create \
  --name $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user $SQL_ADMIN_USER \
  --admin-password "$SQL_ADMIN_PASSWORD"

# Allow Azure services to access SQL Server
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Allow user's IP to access SQL Server
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name AllowUserIP \
  --start-ip-address $USER_IP \
  --end-ip-address $USER_IP

# Create SQL Database
echo ""
echo "Creating Azure SQL Database..."
az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name $SQL_DB_NAME \
  --service-objective S0

# Get SQL connection string
SQL_CONNECTION_STRING="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Initial Catalog=${SQL_DB_NAME};Persist Security Info=False;User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Create Backend Web App
echo ""
echo "Creating Backend Web App..."
az webapp create \
  --name $BACKEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:8.0"

# Configure Backend App Settings
echo ""
echo "Configuring Backend App Settings..."
az webapp config appsettings set \
  --name $BACKEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    ConnectionStrings__DefaultConnection="$SQL_CONNECTION_STRING" \
    JwtSettings__SecretKey="$JWT_SECRET" \
    JwtSettings__Issuer="https://${BACKEND_APP_NAME}.azurewebsites.net" \
    JwtSettings__Audience="https://${FRONTEND_APP_NAME}.azurewebsites.net" \
    JwtSettings__ExpirationMinutes="30" \
    AllowedOrigins__0="https://${FRONTEND_APP_NAME}.azurewebsites.net" \
    RateLimiting__PermitLimit="100" \
    RateLimiting__Window="60" \
    RateLimiting__AuthPermitLimit="5"

# Enable HTTPS only
az webapp update \
  --name $BACKEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --https-only true

# Configure IP restrictions for Backend
echo ""
echo "Configuring IP restrictions for Backend..."

# Add user's IP
az webapp config access-restriction add \
  --name $BACKEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --rule-name "AllowUserIP" \
  --action Allow \
  --ip-address "${USER_IP}/32" \
  --priority 100

# Add Exact Online IP ranges
PRIORITY=200
for IP_RANGE in $EXACT_IPS; do
  az webapp config access-restriction add \
    --name $BACKEND_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --rule-name "AllowExactOnline${PRIORITY}" \
    --action Allow \
    --ip-address "$IP_RANGE" \
    --priority $PRIORITY
  PRIORITY=$((PRIORITY + 1))
done

# Allow frontend to backend communication
az webapp config access-restriction add \
  --name $BACKEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --rule-name "AllowFrontend" \
  --action Allow \
  --service-tag AzureCloud \
  --priority 300

# Deploy Backend
echo ""
echo "Building and deploying Backend..."
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculator/LoanAnnuityCalculatorAPI"
dotnet publish -c Release -o ./publish

# Create deployment zip
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --name $BACKEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src deploy.zip

# Run database migrations
echo ""
echo "Running database migrations..."
echo "Note: You'll need to run migrations manually with:"
echo "dotnet ef database update --connection \"$SQL_CONNECTION_STRING\""

# Create Frontend Web App
echo ""
echo "Creating Frontend Web App..."
az webapp create \
  --name $FRONTEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "NODE:20-lts"

# Configure IP restrictions for Frontend
echo ""
echo "Configuring IP restrictions for Frontend..."

# Add user's IP
az webapp config access-restriction add \
  --name $FRONTEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --rule-name "AllowUserIP" \
  --action Allow \
  --ip-address "${USER_IP}/32" \
  --priority 100

# Build and deploy Frontend
echo ""
echo "Building Frontend..."
cd "/Users/bartkoolhaas/Visual Studio Projecten/LoanAnnuityCalculatorApp"

# Update environment file with backend URL
cat > src/environments/environment.production.ts << EOF
export const environment = {
  production: true,
  apiUrl: 'https://${BACKEND_APP_NAME}.azurewebsites.net/api'
};
EOF

# Build Angular app
npm install
npm run build -- --configuration production

# Deploy frontend
cd dist/loan-annuity-calculator-app
zip -r ../../frontend-deploy.zip .
cd ../..

az webapp deployment source config-zip \
  --name $FRONTEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src frontend-deploy.zip

echo ""
echo "========================================="
echo "Deployment Complete!"
echo "========================================="
echo ""
echo "Backend URL: https://${BACKEND_APP_NAME}.azurewebsites.net"
echo "Frontend URL: https://${FRONTEND_APP_NAME}.azurewebsites.net"
echo ""
echo "SQL Server: ${SQL_SERVER_NAME}.database.windows.net"
echo "Database: ${SQL_DB_NAME}"
echo ""
echo "IP Restrictions configured for:"
echo "  - Your IP: $USER_IP"
echo "  - Exact Online IPs: $EXACT_IPS"
echo ""
echo "IMPORTANT NEXT STEPS:"
echo "1. Update Exact Online redirect URI to: https://${BACKEND_APP_NAME}.azurewebsites.net/api/exact/callback"
echo "2. Configure Exact Online credentials in Azure portal:"
echo "   az webapp config appsettings set --name $BACKEND_APP_NAME --resource-group $RESOURCE_GROUP --settings ExactOnline__ClientId=YOUR_CLIENT_ID ExactOnline__ClientSecret=YOUR_SECRET"
echo "3. Test the deployment"
echo ""
