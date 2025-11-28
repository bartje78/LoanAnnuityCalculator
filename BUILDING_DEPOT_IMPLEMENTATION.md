# Building Depot Loan Type Implementation

## Overview
Building Depot is a new loan type designed for construction financing. It provides a credit facility where the debtor can draw funds incrementally as construction progresses, with an interest-only period followed by repayment of the drawn amount.

## Key Characteristics

### 1. Credit Facility Structure
- **Credit Limit**: The maximum amount the debtor can draw from the facility
- **Amount Drawn**: The actual amount withdrawn from the facility at any point
- **Interest-Only Period**: Initial period where only interest is paid on the drawn amount
- **Repayment Period**: After interest-only, the drawn amount is repaid using annuity schedule

### 2. Calculation Logic

#### During Interest-Only Period
```
Outstanding Amount = Amount Drawn
Monthly Payment = (Amount Drawn × Annual Rate / 12)
```

#### During Repayment Period
The system calculates repayment using the **annuity method** on the drawn amount:
```
Capital Repayment Months = Total Tenor - Interest-Only Months
Months Into Repayment = Elapsed Months - Interest-Only Months

Annuity Payment = (Drawn Amount × Monthly Rate × (1 + Monthly Rate)^Capital Months) /
                  ((1 + Monthly Rate)^Capital Months - 1)

For each month:
  Interest = Remaining × Monthly Rate
  Capital = Annuity Payment - Interest
  Remaining = Remaining - Capital
```

## Database Changes

### New Fields in Loans Table
```sql
ALTER TABLE Loans ADD COLUMN CreditLimit DECIMAL(18,2) NULL;
ALTER TABLE Loans ADD COLUMN AmountDrawn DECIMAL(18,2) NULL;
```

Migration: `20251127160503_AddBuildingDepotFields`

## Backend Implementation

### Model Changes (`Loan.cs`)
```csharp
public decimal? CreditLimit { get; set; }
public decimal? AmountDrawn { get; set; }
```

### Calculation Logic
Added new case in `CalculateOutstandingAmount()` method:
```csharp
case "BuildingDepot":
    decimal drawnAmount = AmountDrawn ?? 0;
    
    if (monthsElapsed < InterestOnlyMonths)
    {
        return drawnAmount; // Interest-only phase
    }
    else
    {
        // Repayment phase using annuity method
        // [calculation logic...]
    }
    break;
```

### API Updates
- **POST /api/loan**: Accepts `CreditLimit` and `AmountDrawn` fields
- **PUT /api/loan/{id}**: Allows updating `CreditLimit` and `AmountDrawn`
- Both endpoints return these fields in response

## Frontend Implementation

### Loan Table Display
Added two new columns:
- **Kredietlimiet**: Shows credit limit for Building Depot loans only
- **Opgenomen Bedrag**: Shows drawn amount for Building Depot loans only

Columns are conditionally displayed:
- Show value if `RedemptionSchedule === 'BuildingDepot'` and value exists
- Show "—" for non-Building Depot loans

### Form Inputs
Edit and new loan forms include:
```html
<input *ngIf="loan.RedemptionSchedule === 'BuildingDepot'"
       type="number" 
       [(ngModel)]="loan.CreditLimit" 
       placeholder="Kredietlimiet"
       title="Maximaal op te nemen bedrag">

<input *ngIf="loan.RedemptionSchedule === 'BuildingDepot'"
       type="number" 
       [(ngModel)]="loan.AmountDrawn" 
       placeholder="Opgenomen bedrag"
       title="Opgenomen bedrag uit kredietfaciliteit">
```

### Redemption Schedule Options
Updated all dropdowns to include:
```html
<option value="BuildingDepot">Bouwdepot</option>
```

With display label: "Bouwdepot"

### Components Updated
1. **loan-table-per-debtor**: Main loan management table
2. **tariff-calculator**: Tariff calculation with building depot option
3. **invoice-table**: Invoice management display
4. **current-annuity**: Annuity details display
5. **financials**: Balance sheet external loans

## Usage Example

### Creating a Building Depot Loan

**Scenario**: €500,000 credit limit for construction project

1. **Initial Setup**
   - Credit Limit: €500,000
   - Amount Drawn: €0
   - Interest Rate: 5% per year
   - Tenor: 120 months (10 years)
   - Interest-Only: 24 months (2 years)
   - Redemption Schedule: BuildingDepot

2. **Month 6** (during construction)
   - Amount Drawn updated to: €150,000
   - Outstanding Amount: €150,000
   - Monthly Interest: €150,000 × 5% / 12 = €625

3. **Month 12** (more drawn)
   - Amount Drawn updated to: €300,000
   - Outstanding Amount: €300,000
   - Monthly Interest: €300,000 × 5% / 12 = €1,250

4. **Month 24** (construction complete)
   - Amount Drawn: €450,000 (final drawn amount)
   - Outstanding Amount: €450,000
   - Still interest-only

5. **Month 25** (repayment starts)
   - Repayment Period: 96 months (120 - 24)
   - Monthly Annuity Payment calculated on €450,000
   - Annuity Payment ≈ €5,686
   - Outstanding reduces monthly

6. **Month 120** (end)
   - Outstanding Amount: €0
   - Loan fully repaid

## Key Differences from Other Loan Types

| Feature | Annuity | Linear | Bullet | Building Depot |
|---------|---------|--------|--------|----------------|
| Initial Amount | Fixed | Fixed | Fixed | Variable (drawn) |
| Credit Limit | N/A | N/A | N/A | Yes |
| Draw Phase | N/A | N/A | N/A | Yes |
| Interest-Only | Optional | Optional | Full term | Initial period |
| Repayment Method | Annuity | Linear | Lump sum | Annuity on drawn |
| Outstanding Calc | From LoanAmount | From LoanAmount | Fixed until end | From AmountDrawn |

## Testing Recommendations

1. **Create Building Depot Loan**
   - Set credit limit (e.g., €500,000)
   - Set initial drawn amount (e.g., €0 or €100,000)
   - Set interest-only period (e.g., 24 months)
   - Verify loan saves correctly

2. **Update Drawn Amount**
   - Edit loan during construction phase
   - Increase Amount Drawn
   - Verify Outstanding Amount updates correctly

3. **Check Calculations**
   - During interest-only: Outstanding = Amount Drawn
   - After interest-only: Outstanding decreases monthly
   - Verify interest calculations use correct base amount

4. **Edge Cases**
   - Amount Drawn > Credit Limit (should allow but flag)
   - Amount Drawn = 0 (valid case)
   - Interest-only = 0 (immediate repayment)
   - Verify past end date shows €0 outstanding

## Future Enhancements

1. **Validation**: Add business rule to prevent Amount Drawn > Credit Limit
2. **Draw History**: Track multiple draws with dates and amounts
3. **Utilization %**: Display Amount Drawn / Credit Limit percentage
4. **Alerts**: Notify when approaching credit limit
5. **Reporting**: Separate analytics for building depot loans
6. **CSV Import**: Support building depot in loan imports

## Technical Notes

- Building Depot loans use the same annuity repayment calculation as regular annuity loans
- The key difference is the base amount: AmountDrawn instead of LoanAmount
- LoanAmount field can be set equal to CreditLimit or left as original requested amount
- OutstandingAmount calculation automatically handles the two-phase structure
- Frontend conditionally shows/hides fields based on RedemptionSchedule
- All existing loan functionality (collateral, payments, invoicing) works with building depot

## Migration Path

Existing loans are not affected:
- CreditLimit and AmountDrawn are nullable
- Only Building Depot loans populate these fields
- All other loan types ignore these fields
- No data migration required for existing loans
