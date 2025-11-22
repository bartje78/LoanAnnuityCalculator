# Loss Given Default (LGD) Implementation Summary

## Overview
Comprehensive LGD modeling has been implemented in the Monte Carlo simulation to calculate potential losses in the event of default, accounting for collateral value evolution, liquidation costs, and senior liens.

## Implementation Date
November 12, 2025

## Key Features

### 1. Collateral Value Modeling
- **Stochastic Evolution**: Collateral values evolve using geometric Brownian motion
- **Drift Parameter**: Expected annual return on collateral (default: 2%)
- **Volatility Parameter**: Standard deviation of collateral value changes (default: 10%)
- **Correlation**: Collateral value changes are correlated with revenue shocks (default: 0.30)

### 2. Recovery Calculation
For each loan at the moment of default:
```
Recovery = (Collateral Value × (1 - Liquidity Haircut%)) - Subordination
```

Where:
- **Collateral Value**: Evolved value at time of default
- **Liquidity Haircut**: Discount factor for forced liquidation (extracted from loan collaterals)
- **Subordination**: Senior liens that must be paid first (first mortgage amounts)

### 3. Loss Given Default Calculation
```
LGD = max(0, Outstanding Debt - Recovery)
LGD% = (LGD / Outstanding Debt) × 100
```

### 4. Expected Loss Metric
Basel formula implementation:
```
Expected Loss = PD × LGD% × Average EAD
```

Where:
- **PD**: Probability of Default (from simulation results)
- **LGD%**: Average loss percentage given default
- **EAD**: Exposure At Default (average outstanding debt at default)

## Technical Implementation

### Backend Changes

#### 1. DTOs (MonteCarloSimulationDtos.cs)

**MonteCarloRequest**:
- Added `CollateralExpectedReturn` (decimal, default: 0.02)
- Added `CollateralVolatility` (decimal, default: 0.10)
- Added `CollateralCorrelation` (decimal, default: 0.30)

**SimulatedLoanInfo**:
- Added `LiquidityHaircut` (decimal)
- Added `Subordination` (decimal)

**SimulationPath**:
- Added `CollateralValueAtDefault` (decimal?)
- Added `RecoveryAmount` (decimal?)
- Added `OutstandingDebtAtDefault` (decimal?)
- Added `LossGivenDefault` (decimal?)
- Added `LGDPercentage` (decimal?)

**SimulationStatistics**:
- Added `AverageLGD` (decimal)
- Added `MedianLGD` (decimal)
- Added `ExpectedLoss` (decimal)

#### 2. Controller (MonteCarloController.cs)

**Collateral Data Extraction** (lines 82-104):
- Iterate through loan collaterals
- Calculate weighted average liquidity haircut
- Sum total subordination (first mortgage amounts)
- Handle multiple collateral items per loan

#### 3. Service (MonteCarloSimulationService.cs)

**Collateral Evolution** (lines 153-192):
- Initialize collateral values dictionary for each loan
- Generate correlated shocks using: `ρ×Z₁ + √(1-ρ²)×Z₂`
- Update collateral values annually: `V(t+1) = V(t) × (1 + drift + shock)`
- Ensure values stay non-negative

**LGD Calculation at Default** (lines 299-348 for liquidity, 351-384 for insolvency):
- Sum all loan collateral values
- Calculate recovery per loan: `(collateral × (1 - haircut)) - subordination`
- Sum total recovery across all loans
- Calculate loss: `outstanding - recovery`
- Calculate LGD percentage
- Store all values in SimulationPath

**Aggregate Statistics** (lines 120-131):
- Filter paths where default occurred and LGD was calculated
- Calculate average LGD across defaulted paths
- Calculate median LGD using percentile function
- Compute Expected Loss using Basel formula

### Frontend Changes

#### 1. TypeScript (monte-carlo-simulation.ts)

**Component Properties** (lines 227-230):
```typescript
collateralExpectedReturn = 0.02;  // 2% annual appreciation
collateralVolatility = 0.10;       // 10% volatility
collateralCorrelation = 0.30;      // Correlation with revenue
```

**Interface Updates**:
- Added collateral parameters to `MonteCarloRequest`
- Added LGD fields to `SimulationPath`
- Added LGD statistics to `SimulationStatistics`

**Helper Method** (lines 468-481):
```typescript
calculateAverageLGDPercentage(): string {
  const defaultedPaths = this.results.SamplePaths.filter(
    p => p.DefaultOccurred && p.LGDPercentage
  );
  if (defaultedPaths.length === 0) return '0.0';
  
  const avgLGDPercentage = defaultedPaths.reduce(
    (sum, p) => sum + (p.LGDPercentage || 0), 0
  ) / defaultedPaths.length;
  return avgLGDPercentage.toFixed(1);
}
```

#### 2. HTML Template (monte-carlo-simulation.html)

**Input Controls**:
- Added "Onderpand Waarderingsparameters" section
- Three range sliders for:
  * Expected Return (-5% to 10%, step 0.5%)
  * Volatility (0% to 30%, step 1%)
  * Correlation (-1.0 to 1.0, step 0.05)

**LGD Statistics Display** (lines 387-417):
- Section shown only when `AverageLGD > 0`
- Displays Average LGD, Median LGD, LGD %, and Expected Loss
- Highlighted styling for Expected Loss metric

**Sample Paths Enhancement** (in each scenario card):
- Added LGD info section shown when `LossGivenDefault` exists
- Displays:
  * Collateral value at default
  * Recoverable amount (after haircut and subordination)
  * Loss Given Default (in red)
  * LGD percentage (in red)

#### 3. CSS (monte-carlo-simulation.css)

**LGD Metrics Section**:
- Orange gradient background (`#fff8e1` to `#ffecb3`)
- Orange border (`#ffa726`)
- Highlight class for Expected Loss (red gradient)

**LGD Info in Sample Paths**:
- Light orange background with border
- Loss values in red (`#d32f2f`)
- Bold font weight for emphasis

## Mathematical Details

### Correlated Random Shocks
To generate collateral shocks correlated with revenue:
```
ε_collateral = ρ × ε_revenue + √(1 - ρ²) × ε_independent
```

Where:
- `ρ` = correlation coefficient (collateralCorrelation parameter)
- `ε_revenue` = revenue shock (standard normal)
- `ε_independent` = independent shock (standard normal)

This approach uses Cholesky decomposition for bivariate normal distribution.

### Collateral Value Evolution
```
V(t+1) = V(t) × (1 + μ + σ × ε_collateral)
```

Where:
- `μ` = expected return (drift)
- `σ` = volatility
- `ε_collateral` = correlated shock

### Recovery Formula
```
For each loan i:
  Recovery_i = max(0, (CollateralValue_i × (1 - Haircut_i/100)) - Subordination_i)

Total Recovery = Σ Recovery_i
```

### LGD Metrics
```
Total Outstanding = Σ OutstandingBalance_i
Total Recovery = Σ Recovery_i
Loss Amount = max(0, Total Outstanding - Total Recovery)
LGD Percentage = (Loss Amount / Total Outstanding) × 100
```

### Expected Loss
```
EL = PD × (Average LGD% / 100) × Average EAD
```

## Default Triggers

LGD is calculated for both types of default:

1. **Liquidity Default**: When liquid assets drop below zero
2. **Insolvency Default**: When equity becomes negative

## User Interface Flow

1. **Configure Collateral Parameters**:
   - Adjust expected return slider (real estate: 2-4%, machinery: 0-2%)
   - Set volatility based on asset type (real estate: 5-15%, equipment: 10-30%)
   - Configure correlation with business performance (-1 to +1)

2. **Run Simulation**:
   - Backend evolves collateral values alongside financial metrics
   - Default detection triggers LGD calculation
   - Recovery and loss amounts computed per path

3. **Review Results**:
   - **Summary Statistics**: Average LGD, Median LGD, Expected Loss
   - **Sample Paths**: See detailed breakdown for worst/median/best scenarios
   - **Risk Assessment**: Compare Expected Loss to loan amounts

## Data Sources

### Collateral Information
Extracted from database tables:
- **Collateral**: AppraisalValue, LiquidityHaircut, FirstMortgageAmount
- **LoanCollateral**: Links loans to collaterals

### Haircut Calculation
When multiple collateral items exist:
```
Weighted Haircut = (Σ CollateralValue_i × Haircut_i) / Σ CollateralValue_i
```

### Subordination
Sum of all first mortgage amounts across collateral items.

## Testing Recommendations

### Unit Tests
- Test collateral evolution with various parameters
- Verify correlation implementation
- Test recovery calculation with edge cases (zero collateral, full subordination)
- Validate LGD calculation for various outstanding amounts

### Integration Tests
- Run simulation with actual loan data
- Verify haircut and subordination extraction from database
- Test multiple loans with different collateral types
- Validate Expected Loss formula

### Scenario Tests
1. **High Correlation (0.8)**: Collateral falls when business struggles
2. **Negative Correlation (-0.5)**: Counter-cyclical assets
3. **Zero Collateral**: LGD should equal 100%
4. **Full Subordination**: Recovery should be zero
5. **High Haircut (80%)**: Minimal recovery despite high collateral value

## Business Interpretation

### LGD Percentages
- **0-20%**: Excellent collateral coverage and liquidity
- **20-40%**: Good coverage, typical for well-secured loans
- **40-60%**: Moderate risk, review collateral quality
- **60-80%**: High risk, insufficient coverage
- **80-100%**: Severe risk, near-total loss expected

### Expected Loss
Represents the **average loss** across all scenarios (defaults and non-defaults):
```
Example:
- PD = 10% (10% chance of default)
- LGD = 40% (40% loss on defaulted amount)
- EAD = €500,000 (average exposure)

EL = 0.10 × 0.40 × €500,000 = €20,000
```

This €20,000 represents the **expected credit loss** to be provisioned.

### Risk-Based Pricing
Use Expected Loss to adjust interest rates:
```
Required Risk Premium = (Expected Loss / Loan Amount) / Tenor Years
```

## Future Enhancements

### Short Term
1. Add LGD distribution chart (histogram of losses)
2. Save collateral parameters to model settings
3. Export LGD analysis to Excel/PDF
4. Add sensitivity analysis on haircut and subordination

### Medium Term
1. Industry-specific collateral parameters (real estate, equipment, inventory)
2. Time-varying collateral values (seasonal effects)
3. Correlation matrix for multiple collateral types
4. Monte Carlo for collateral liquidation timing

### Long Term
1. Machine learning for collateral valuation
2. Market-based haircut estimation
3. Recovery rate modeling from historical data
4. Portfolio-level LGD optimization

## References

### Financial Theory
- Basel II/III framework for credit risk
- Expected Loss = PD × LGD × EAD
- Recovery rate modeling

### Implementation
- Geometric Brownian Motion for asset prices
- Cholesky decomposition for correlation
- Percentile calculations for statistics

## Version History

### v1.0 (November 12, 2025)
- Initial LGD implementation
- Collateral evolution with drift and volatility
- Correlated shocks with revenue
- Recovery calculation with haircut and subordination
- Expected Loss calculation
- Full UI integration

## Notes

### Performance
- Collateral dictionary maintains O(1) lookup per loan
- LGD calculation only triggered on default (efficient)
- Statistics computed once after all simulations

### Accuracy
- Collateral values constrained to non-negative
- Recovery never exceeds collateral value
- LGD percentage capped at 100%
- Weighted haircuts account for portfolio composition

### Validation
- Backend: ✅ Builds successfully (0 errors)
- Frontend: ✅ Builds successfully (bundle warnings only)
- Ready for end-to-end testing with actual loan data

## Contact & Support
For questions or issues with LGD implementation, review:
1. This documentation
2. Inline code comments in MonteCarloSimulationService.cs
3. TypeScript interfaces in monte-carlo-simulation.ts
