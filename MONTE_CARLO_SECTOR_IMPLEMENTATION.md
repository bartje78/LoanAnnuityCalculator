# Monte Carlo Sector-Based Implementation Summary

## Status: ✅ INDIVIDUAL DEBTOR MODE COMPLETE (November 2025)

### What's Implemented

#### 1. Database Schema
- **SectorDefinitions**: 12 editable sectors with volatilities, growth rates, Dutch names
- **SectorCorrelations**: 66 sector-to-sector correlations (12×12 symmetric matrix)
- **SectorCollateralCorrelations**: 108 sector-to-property-type correlations (12 sectors × 9 property types)
- **DebtorPL.RevenueSectorBreakdown**: JSON field storing revenue allocation per sector

#### 2. Monte Carlo Integration
- **Automatic enrichment**: `MonteCarloController.EnrichRequestWithSectorData()` loads all correlation data from database
- **Sector shock generation**: Uses Cholesky decomposition to create correlated sector shocks
- **Revenue calculation**: Weighted combination of sector shocks based on P&L breakdown
- **Collateral calculation**: Correlated with revenue sectors using sector-collateral correlations

### How Shocks Work

#### Frequency: ANNUAL
- Shocks are applied **once per year** in the simulation loop
- Each year generates new independent random shocks
- Models macroeconomic cycles (not daily price movements)

#### Types of Shocks (4 per year):

**1. Revenue Shock (Sector-Based)**
```csharp
// Step 1: Generate 12 correlated sector shocks
var correlatedNormals = GenerateCorrelatedNormals(correlationMatrix);  // Uses Cholesky
var sectorShocks = { Manufacturing: +0.8σ, Retail: -1.2σ, ... };

// Step 2: Calculate weighted shock based on revenue mix
revenueShock = Σ(weight[i] × volatility[i] × sectorShock[i])
             = (0.6 × 15% × 0.8) + (0.4 × 20% × -1.2)
             = +7.2% - 9.6% = -2.4%

// Step 3: Apply to revenue
Revenue_t = Revenue_(t-1) × (1 + growth + revenueShock)
```

**2. Operating Cost Shock (Independent)**
```csharp
operatingCostShock = GenerateNormalRandom(mean=0, stdDev=10%)
OperatingCosts_t = OperatingCosts_(t-1) × (1 + growth + costShock)
```
- **Independent from revenue** - captures operational unpredictability
- Can move opposite to revenue (worst case: costs spike during recession)

**3. Collateral Shock (Correlated with Revenue Sectors)**
```csharp
// Use SAME sector shocks as revenue (systematic risk!)
// For each sector in revenue mix, apply correlation with property type

For Residential property:
- Manufacturing (60% revenue) → Residential correlation = 0.35
- Retail (40% revenue) → Residential correlation = 0.45

correlatedPart = (0.6 × 0.35 × sectorShock[Mfg]) + 
                 (0.4 × 0.45 × sectorShock[Retail])
               = (0.6 × 0.35 × 0.8) + (0.4 × 0.45 × -1.2)
               = +0.168 - 0.216 = -0.048

// Add independent component to maintain total volatility
independentStdDev = sqrt(totalVariance - correlatedVariance)
collateralShock = correlatedPart × collateralVolatility + GenerateNormalRandom(0, independentStdDev)

Collateral_t = Collateral_(t-1) × (1 + expectedReturn + collateralShock)
```

**4. Box-Muller Transform (Random Number Generation)**
```csharp
double u1 = 1.0 - _random.NextDouble();  // Uniform [0,1]
double u2 = 1.0 - _random.NextDouble();
double randStdNormal = sqrt(-2 × ln(u1)) × sin(2π × u2)
return mean + stdDev × randStdNormal  // Transforms to N(mean, stdDev²)
```

### Key Properties

**Volatility = Annual Standard Deviation**
- 15% volatility ≈ 68% of years have shocks between -15% and +15%
- ≈ 95% of years have shocks between -30% and +30%
- ≈ 99.7% of years have shocks between -45% and +45%

**Correlation Structure**
- **Sectors correlated with each other**: Manufacturing ↔ Retail = 0.60
- **Sectors correlated with property types**: Manufacturing ↔ Industrial = 0.70
- **Operating costs independent**: No correlation with revenue shocks

**Multiple Revenue Streams + One Collateral**
When debtor has mixed revenue (e.g., 60% Manufacturing, 40% Retail) with one property type:

1. **Revenue shock** = weighted avg of sector shocks
2. **Collateral shock** = weighted avg of sector-property correlations × sector shocks
3. **Same shocks used** for both revenue and collateral (systematic risk!)

Example:
```
Revenue composition: 60% Manufacturing, 40% Retail
Collateral: Residential property

Year 1 shocks:
- Manufacturing sector: +0.8σ
- Retail sector: -1.2σ

Revenue impact:
0.6 × 15% × 0.8 + 0.4 × 20% × -1.2 = -2.4%
→ Revenue falls by 2.4%

Collateral impact (Residential):
0.6 × 0.35 × 0.8 + 0.4 × 0.45 × -1.2 = -0.048 (normalized)
-0.048 × 10% = -0.48%
→ Collateral falls by 0.48% (less sensitive than revenue)
```

### Data Flow

```
1. User enters P&L revenue sector breakdown in financials form
   → Saved as JSON: {"Manufacturing": 60000, "Retail": 40000}

2. User runs Monte Carlo simulation
   → MonteCarloController.RunSimulation()
   → EnrichRequestWithSectorData()
      - Loads sector definitions (volatilities)
      - Builds 12×12 correlation matrix
      - Parses P&L JSON → calculates weights {Mfg: 0.6, Retail: 0.4}
      - Loads sector-collateral correlations for property type
      - Populates request object

3. MonteCarloSimulationService.RunSimulation()
   For each simulation (1-1000):
      For each year (1-10):
         - Generate 12 correlated sector shocks (Cholesky)
         - Calculate revenue shock (weighted combination)
         - Calculate collateral shock (correlated with revenue sectors)
         - Generate independent operating cost shock
         - Apply shocks to growth rates
         - Calculate P&L and balance sheet
         - Check for default conditions

4. Aggregate results
   → Calculate statistics (PD, EL, LGD, ROI)
   → Return to frontend
```

### Architecture

**Files Modified:**
- `MonteCarloSimulationService.cs`:
  - `GenerateSectorBasedRevenueShock()`: NEW
  - `GenerateSectorBasedCollateralShock()`: NEW
  - `GenerateCorrelatedNormals()`: Uses Cholesky decomposition
  - `CholeskyDecomposition()`: Matrix factorization L×L^T = ρ

- `MonteCarloController.cs`:
  - `EnrichRequestWithSectorData()`: Loads all correlation data from DB
  - Determines primary property type from loan collateral
  - Filters correlations to relevant property type

- `Debtor.cs` (DebtorPL model):
  - Added `RevenueSectorBreakdown` string property (JSON storage)

- `MonteCarloSimulationRequest`:
  - `SectorWeights`: Dictionary<Sector, decimal>
  - `SectorVolatilities`: Dictionary<Sector, decimal>
  - `SectorCorrelationMatrix`: double[,]
  - `SectorCollateralCorrelations`: Dictionary<(Sector, string), decimal>

### Fallback Behavior

If no sector data available:
```csharp
if (request.SectorWeights == null || !request.SectorWeights.Any())
{
    // LEGACY APPROACH: Simple independent shock
    revenueShock = GenerateNormalRandom(0, request.RevenueVolatility);
    collateralShock = correlation × revenueShock + sqrt(1-correlation²) × independentShock;
}
```

## Next Steps: Portfolio-Level Simulation

### Goal
Run Monte Carlo across **multiple debtors simultaneously** with:
- **ONE set of sector shocks per simulation** (all debtors in same macroeconomic environment)
- **Systematic risk**: Debtors with similar sector exposure experience correlated outcomes
- **Diversification effects**: Portfolio risk < sum of individual risks

### Implementation Plan

**1. API Endpoint** ✅ Created
```http
POST /api/montecarlo/simulate-portfolio
Body: {
  "DebtorIds": [1, 2, 3],
  "NumberOfSimulations": 1000,
  "SimulationYears": 10
}
```

**2. DTOs** ✅ Created
- `PortfolioMonteCarloRequest`
- `PortfolioMonteCarloResponse`
- `PortfolioStatistics`
- `DebtorSimulationSummary`
- `PortfolioYearlyStatistics`

**3. Simulation Logic** (TODO)
```csharp
for (sim = 1 to 1000):
    // Generate ONE set of sector shocks for this simulation
    sectorShocks = GenerateCorrelatedNormals(correlationMatrix)
    
    for (year = 1 to 10):
        for each debtor:
            // All debtors use SAME sector shocks (systematic risk!)
            revenueShock[debtor] = Σ(weight[debtor,i] × vol[i] × sectorShocks[i])
            collateralShock[debtor] = Σ(weight[debtor,i] × corr[i,property] × sectorShocks[i])
            
            // Apply shocks and calculate P&L
            // Check for defaults
        
        // Aggregate portfolio metrics for this year
        totalRevenue = Σ(revenue[debtor])
        totalDefaults = count(debtors in default)
    
    // Store results for this simulation path
```

**4. Portfolio Metrics**
- **Concentration risk**: % of portfolio in each sector
- **Correlation impact**: Measure diversification benefit
- **Aggregate PD**: Portfolio-level probability of any default
- **Expected Loss**: Total expected loss across all loans
- **ROI**: Portfolio return on investment

**5. Visualization** (Frontend)
- Multi-debtor scenario charts
- Sector exposure breakdown
- Correlation heat maps
- Portfolio risk dashboard

### Benefits of Portfolio Mode

1. **Realistic systematic risk**: Manufacturing recession affects all manufacturing debtors
2. **Diversification quantification**: Measure benefit of sector/property type mix
3. **Concentration risk**: Identify over-exposure to specific sectors
4. **Portfolio optimization**: Test different compositions
5. **Capital requirements**: Calculate regulatory capital for entire portfolio

### Technical Challenges

- **Performance**: Simulating 5 debtors × 1000 simulations × 10 years = 50,000 debtor-years
- **Memory**: Storing all paths for all debtors
- **Aggregation**: Efficiently combine results
- **Frontend**: Visualizing multi-dimensional data

**Solution**: 
- Run simulations in parallel (Task.WhenAll)
- Store only summary statistics + sample paths
- Implement efficient aggregation logic
- Progressive disclosure UI (drill-down from portfolio → debtor)

## References

- **Box-Muller Transform**: Converts uniform random → normal distribution
- **Cholesky Decomposition**: Factors correlation matrix ρ = L×L^T for generating correlated random variables
- **Systematic Risk**: Risk affecting all entities in portfolio (cannot diversify away)
- **Idiosyncratic Risk**: Entity-specific risk (can diversify away)
