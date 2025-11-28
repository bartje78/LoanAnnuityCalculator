#!/bin/bash

# Seed default add-ons to the database

echo "Seeding default add-ons..."

# First, login to get a token
echo "Step 1: Getting authentication token..."
TOKEN=$(curl -s -X POST http://localhost:5206/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@loanannuity.local",
    "password": "Admin123!"
  }' | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "Error: Could not get authentication token"
  exit 1
fi

echo "Step 2: Seeding add-ons with token..."
RESPONSE=$(curl -s -X POST http://localhost:5206/api/planmanagement/addons/seed \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "Response: $RESPONSE"

if [[ $RESPONSE == *"error"* ]] || [[ $RESPONSE == *"Error"* ]]; then
  echo "Error seeding add-ons"
  exit 1
else
  echo "✓ Add-ons seeded successfully!"
  
  # List the seeded add-ons
  echo ""
  echo "Seeded add-ons:"
  curl -s http://localhost:5206/api/planmanagement/addons \
    -H "Authorization: Bearer $TOKEN" | python3 -m json.tool 2>/dev/null || echo "$RESPONSE"
fi
