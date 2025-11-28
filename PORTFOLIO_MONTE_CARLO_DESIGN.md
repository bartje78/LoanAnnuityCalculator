# Portfolio-Level Monte Carlo Simulation with Sector Correlations

## Implementation Status: ✅ INDIVIDUAL DEBTOR MODE COMPLETE

### Current Implementation (November 2025)
- ✅ 12-sector correlation matrix with Cholesky decomposition
- ✅ 108 sector-collateral correlations (12 sectors × 9 property types)
- ✅ Revenue sector breakdown in P&L (JSON field `RevenueSectorBreakdown`)
- ✅ Automatic enrichment of simulation requests from database
- ✅ Sector-based revenue shocks with weighted calculation
- ✅ Sector-based collateral shocks correlated with revenue sectors
- ✅ Full integration in `MonteCarloSimulationService` and `MonteCarloController`

### How Shocks Work (Annual Frequency)

**Shock Types Applied Each Year:**

1. **Revenue Shock** (Sector-Based)
   - Generates 12 correlated sector shocks using Cholesky decomposition
   - Calculates weighted shock: `Σ(weight[i] × volatility[i] × sectorShock[i])`
   - Example: 60% Manufacturing (σ=15%, shock=+0.8) + 40% Retail (σ=20%, shock=-1.2)
     = 0.6×15%×0.8 + 0.4×20%×(-1.2) = +7.2% - 9.6% = **-2.4% shock**

2. **Operating Cost Shock** (Independent)
   - Independent from revenue (captures operational unpredictability)
   - Default volatility: 10%
   - Can spike when revenue falls (worst case) or fall when revenue rises (best case)

3. **Collateral Shock** (Correlated with Revenue Sectors)
   - Uses SAME sector shocks as revenue (systematic risk!)
   - Applies sector-collateral correlations per property type
   - Example: Manufacturing (60%) + Retail (40%) with Residential property:
     - Manufacturing→Residential correlation = 0.35
     - Retail→Residential correlation = 0.45
     - Weighted correlation = 0.6×0.35×shock[Mfg] + 0.4×0.45×shock[Retail]
   - Adds independent component to maintain total volatility

**Shock Generation Process:**
```
1. Generate 12 independent standard normals: Z₁, Z₂, ..., Z₁₂
2. Apply Cholesky decomposition: ε = L × Z (creates correlation structure)
3. Calculate revenue shock: Σ(weight[i] × volatility[i] × ε[i])
4. Calculate collateral shock: Σ(weight[i] × correlation[i,propertyType] × ε[i]) + independent
5. Apply to growth: Value_t = Value_(t-1) × (1 + growth + shock)
```

**Frequency:** Shocks are applied **once per year** (annual frequency)
- Each simulation year generates new random shocks
- No autocorrelation between years (memoryless random walk)
- Models macroeconomic cycles rather than daily fluctuations

**For Multiple Revenue Streams + One Collateral:**
- Each sector contributes to collateral shock based on:
  1. Its weight in revenue mix (e.g., 60% Manufacturing, 40% Retail)
  2. Its correlation with property type (e.g., Mfg→Residential = 0.35)
  3. The same sector shock used for revenue (ensures consistency)

**Next Step:** Portfolio-level simulation across multiple debtors

---

## Design Overview - UPDATED: Revenue Breakdown Approach

### 1. Revenue Category Classification
**Use existing `RevenueDetail.RevenueCategory` field** - debtors already break down revenue by source!

Instead of assigning one sector per debtor, we:
- Use the most recent year's P&L revenue breakdown
- Calculate weighted sector exposure per debtor
- Model each debtor's revenue as weighted sum of sector-specific shocks

**Example:**
```
Debtor A (Manufacturing company):
- Product Sales (Manufacturing): €800k (80%)
- Real Estate Rental: €150k (15%)
- Consulting Services: €50k (5%)

→ Exposure vector: [0.80 Mfg, 0.15 RealEst, 0.05 ProfSvc]
```

### 2. Correlation Matrix (Unchanged)
Store sector-to-sector correlation coefficients for standardized revenue categories.

### 3. Portfolio Simulation Methodology - ENHANCED

#### Revenue Modeling with Multi-Sector Exposure

For debtor i with revenue breakdown across sectors j:

**Step 1:** Get latest year P&L revenue breakdown
```
w_ij = revenue from sector j / total revenue for debtor i
```

**Step 2:** Generate correlated sector shocks using Cholesky
```
Z ~ N(0, I_n)  [independent normals for n sectors]
ε = L * Z      [correlated shocks where L*L^T = ρ]
```

**Step 3:** Calculate debtor-specific revenue shock
```
ε_i = Σ(w_ij * σ_j * ε_j)  [weighted combination of sector shocks]

where:
- w_ij = weight of sector j in debtor i's revenue
- σ_j = volatility for sector j (from ModelSettings)
- ε_j = correlated shock for sector j
```

**Step 4:** Apply to revenue evolution
```
R_i(t) = R_i(t-1) * (1 + growth_i + ε_i)
```

**Benefits of this approach:**
- **No sector assignment needed** - uses actual revenue data
- **Granular risk modeling** - diversified debtors have lower volatility
- **Realistic correlations** - manufacturing company with real estate income gets both shocks
- **Automatic weighting** - larger revenue sources have more impact

### 4. Database Schema Changes - SIMPLIFIED

**No changes to Debtor model needed!** ✓

**Only add:**
```csharp
// New model: SectorCorrelation (already implemented)
public class SectorCorrelation
{
    public int Id { get; set; }
    public int ModelSettingsId { get; set; }
    public string Sector1 { get; set; }  // Maps to RevenueCategory values
    public string Sector2 { get; set; }
    public decimal CorrelationCoefficient { get; set; }
}

// Add to ModelSettings (already implemented)
public decimal SectorVolatilityManufacturing { get; set; } = 0.15m;
public decimal SectorVolatilityRetail { get; set; } = 0.20m;
public decimal SectorVolatilityRealEstate { get; set; } = 0.12m;
// ... one for each standard sector
```

**Mapping RevenueCategory to Sectors:**
Create a configuration mapping from free-text `RevenueCategory` to standardized sectors:
- "Product Sales", "Manufacturing Revenue" → Manufacturing
- "Rental Income", "Property Revenue" → Real Estate  
- "Consulting", "Advisory Services" → Professional Services
- etc.

This can be done via:
1. Configuration table
2. Pattern matching in service layer
3. UI dropdown with predefined categories

### 5. Implementation Steps - UPDATED

#### Phase 1: Data Model & Mapping ✓ DONE
- [x] Create Sector enum with 12 standard sectors
- [x] Create SectorCorrelation model  
- [x] Extend ModelSettings with sector correlations
- [x] **NEW:** Create RevenueSectorMappingService
  - Maps RevenueCategory strings to Sector enum
  - Calculates weighted sector exposure per debtor
  - Provides fuzzy matching for flexibility
- [ ] Create migration for SectorCorrelation table
- [ ] Seed default correlation matrix

#### Phase 2: Correlation Matrix Service
- [ ] Implement Cholesky decomposition utility
- [ ] Create correlated random number generator using Cholesky
- [ ] Build correlation matrix manager (get/set/validate)
- [ ] Add sector-specific volatility parameters to ModelSettings

#### Phase 3: Portfolio Monte Carlo Service
- [ ] Extend MonteCarloSimulationService with portfolio methods
- [ ] For each debtor: Calculate sector weights from latest P&L
- [ ] Generate correlated sector shocks (one per sector per year)
- [ ] Apply weighted shocks to each debtor's revenue
- [ ] Aggregate portfolio-level statistics (PD, LGD, expected loss)

#### Phase 4: API & DTOs
- [ ] Create PortfolioMonteCarloRequest DTO
  ```csharp
  {
      List<int> DebtorIds,
      int SimulationYears,
      int NumberOfSimulations,
      int ModelSettingsId
  }
  ```
- [ ] Create PortfolioMonteCarloResponse DTO
  ```csharp
  {
      PortfolioStatistics Statistics,
      List<DebtorContribution> DebtorBreakdown,
      Dictionary<Sector, SectorStatistics> SectorAnalysis
  }
  ```
- [ ] Add POST /api/montecarlo/portfolio endpoint
- [ ] Add sector correlation CRUD endpoints

#### Phase 5: Frontend
- [ ] Add portfolio simulation page
- [ ] Multi-select debtors for portfolio analysis
- [ ] Visualize correlation heat map
- [ ] Show sector exposure pie chart per debtor
- [ ] Display portfolio-level metrics vs sum of individual

### 6. Mathematical Details

#### Correlation Matrix Structure
For n sectors, ρ is n×n symmetric positive definite matrix:
- Diagonal: 1.0 (perfect self-correlation)
- Off-diagonal: 0.3 to 0.8 (typical sector correlations)
- Must be positive definite for Cholesky decomposition

#### Cholesky Decomposition
Given ρ, find lower triangular L such that:
ρ = L * L^T

Then correlated normals:
ε = L * Z where Z ~ N(0, I)
ensures ε ~ N(0, ρ)

#### Portfolio Default Probability
With correlations, portfolio PD ≠ sum of individual PDs
Use copula approach or direct simulation to capture joint default events

### 7. Default Sector Correlations (Suggested)

| Sector 1 | Sector 2 | Correlation |
|----------|----------|-------------|
| Manufacturing | Retail | 0.65 |
| Manufacturing | Construction | 0.70 |
| Retail | Real Estate | 0.45 |
| Technology | Professional Services | 0.55 |
| Healthcare | Professional Services | 0.40 |
| Hospitality | Retail | 0.60 |
| Agriculture | Manufacturing | 0.35 |
| Financial Services | Real Estate | 0.50 |
| Transportation | Manufacturing | 0.55 |
| All sectors | Financial Services | 0.40-0.60 |

**Within-sector correlation:** 0.85 (high co-movement)
**Cross-sector baseline:** 0.35 (moderate systemic risk)
**Unrelated sectors:** 0.15-0.25 (minimal correlation)

### 8. API Endpoints

```
POST /api/montecarlo/portfolio
Body: {
  debtorIds: [1, 2, 3, ...],
  simulationYears: 5,
  numberOfSimulations: 10000,
  modelSettingsId: 1
}

GET /api/settings/sector-correlations
POST /api/settings/sector-correlations
PUT /api/settings/sector-correlations/{id}
```

### 9. Benefits of This Approach

1. **Realistic Risk Modeling**: Captures market-wide shocks affecting multiple debtors
2. **Proper Diversification**: Shows true portfolio benefits (not just sum of parts)
3. **Scenario Analysis**: Can model sector-specific stress scenarios
4. **Regulatory Compliance**: Aligns with Basel requirements for portfolio risk
5. **Granular Control**: Correlation matrix can be calibrated to historical data
6. **Scalability**: Efficient for large portfolios (O(n²) for correlation matrix)

### 10. Future Enhancements

- **Dynamic correlations**: Time-varying correlations based on economic cycle
- **Factor models**: Decompose correlations into systematic factors
- **Tail dependence**: Copula-based modeling for extreme events
- **Revenue source breakdown**: Model correlations at revenue stream level
- **Geographic correlations**: Add location-based correlation factors
