#!/bin/bash

echo "🔐 Exact Online Credentials Setup"
echo "================================="
echo ""

# Check if appsettings.local.json exists
if [ ! -f "LoanAnnuityCalculatorAPI/appsettings.local.json" ]; then
    echo "❌ appsettings.local.json not found!"
    exit 1
fi

echo "✅ appsettings.local.json exists"

# Check if it contains placeholder values
if grep -q "YOUR_CLIENT_ID_HERE" "LoanAnnuityCalculatorAPI/appsettings.local.json"; then
    echo "⚠️  Please update appsettings.local.json with your actual Exact Online credentials"
    echo ""
    echo "Edit the file: LoanAnnuityCalculatorAPI/appsettings.local.json"
    echo ""
else
    echo "✅ Credentials appear to be configured"
fi

# Check .gitignore
if grep -q "appsettings.local.json" ".gitignore"; then
    echo "✅ appsettings.local.json is in .gitignore"
else
    echo "❌ WARNING: appsettings.local.json is NOT in .gitignore!"
fi

echo ""
echo "📋 Next Steps:"
echo "1. Edit LoanAnnuityCalculatorAPI/appsettings.local.json with your credentials"
echo "2. Configure Azure App Service Application Settings:"
echo "   - ExactOnline__ClientId"
echo "   - ExactOnline__ClientSecret"
echo "3. Register redirect URI in Exact Online:"
echo "   https://loancalculator-api-loanportal.azurewebsites.net/api/exact/callback"
echo ""
echo "For detailed instructions, see EXACT_ONLINE_SETUP.md"

