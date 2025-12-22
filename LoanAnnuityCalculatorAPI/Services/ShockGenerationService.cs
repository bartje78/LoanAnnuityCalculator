using LoanAnnuityCalculatorAPI.Models;

namespace LoanAnnuityCalculatorAPI.Services
{
    /// <summary>
    /// Service responsible for generating all economic shocks upfront for Monte Carlo simulations.
    /// This ensures consistent shock generation between individual and portfolio simulations.
    /// </summary>
    public class ShockGenerationService
    {
        private readonly Random _random = new Random();

        /// <summary>
        /// Container for all pre-generated shocks for a simulation run
        /// </summary>
        public class SimulationShockSet
        {
            /// <summary>
            /// Sector shocks indexed by [simulationNumber][year][sector]
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<Sector, double>>> SectorShocks { get; set; } = new();

            /// <summary>
            /// Collateral shocks indexed by [simulationNumber][year][collateralType]
            /// </summary>
            public Dictionary<int, Dictionary<int, Dictionary<string, double>>> CollateralShocks { get; set; } = new();

            /// <summary>
            /// Get sector shocks for a specific simulation and year
            /// </summary>
            public Dictionary<Sector, double>? GetSectorShocks(int simulationNumber, int year)
            {
                if (SectorShocks.TryGetValue(simulationNumber, out var simShocks) &&
                    simShocks.TryGetValue(year, out var yearShocks))
                {
                    return yearShocks;
                }
                return null;
            }

            /// <summary>
            /// Get collateral shocks for a specific simulation and year
            /// </summary>
            public Dictionary<string, double>? GetCollateralShocks(int simulationNumber, int year)
            {
                if (CollateralShocks.TryGetValue(simulationNumber, out var simShocks) &&
                    simShocks.TryGetValue(year, out var yearShocks))
                {
                    return yearShocks;
                }
                return null;
            }
        }

        /// <summary>
        /// Generate all shocks upfront for the entire simulation run.
        /// This ensures consistent shock generation between individual and portfolio simulations.
        /// </summary>
        /// <param name="numberOfSimulations">Number of Monte Carlo simulations to run</param>
        /// <param name="simulationYears">Number of years to simulate</param>
        /// <param name="sectors">List of sectors to generate shocks for</param>
        /// <param name="collateralTypes">List of collateral property types to generate shocks for</param>
        /// <param name="sectorCorrelationMatrix">Correlation matrix for sector shocks</param>
        /// <param name="sectorVolatilities">Volatility for each sector</param>
        /// <param name="sectorCollateralCorrelations">Correlations between sectors and collateral types</param>
        /// <param name="collateralVolatilities">Volatility for each collateral type</param>
        /// <returns>Complete set of pre-generated shocks</returns>
        public SimulationShockSet GenerateShocks(
            int numberOfSimulations,
            int simulationYears,
            List<Sector> sectors,
            List<string> collateralTypes,
            double[,] sectorCorrelationMatrix,
            Dictionary<Sector, decimal> sectorVolatilities,
            Dictionary<(Sector, string), decimal> sectorCollateralCorrelations,
            Dictionary<string, decimal> collateralVolatilities)
        {
            var shockSet = new SimulationShockSet();

            Console.WriteLine($"[SHOCK GENERATION] Generating shocks for {numberOfSimulations} simulations, {simulationYears} years");
            Console.WriteLine($"[SHOCK GENERATION] Sectors: {string.Join(", ", sectors)}");
            Console.WriteLine($"[SHOCK GENERATION] Collateral types: {string.Join(", ", collateralTypes)}");

            for (int sim = 1; sim <= numberOfSimulations; sim++)
            {
                shockSet.SectorShocks[sim] = new Dictionary<int, Dictionary<Sector, double>>();
                shockSet.CollateralShocks[sim] = new Dictionary<int, Dictionary<string, double>>();

                for (int year = 1; year <= simulationYears; year++)
                {
                    // Generate correlated sector shocks for this simulation-year
                    var sectorShocks = GenerateCorrelatedSectorShocks(
                        sectors, 
                        sectorCorrelationMatrix, 
                        sectorVolatilities);
                    
                    shockSet.SectorShocks[sim][year] = sectorShocks;

                    // Generate correlated collateral shocks for this simulation-year
                    var collateralShocks = GenerateCorrelatedCollateralShocks(
                        collateralTypes,
                        sectorShocks,
                        sectors,
                        sectorCollateralCorrelations,
                        collateralVolatilities);
                    
                    shockSet.CollateralShocks[sim][year] = collateralShocks;

                    // Log first simulation for debugging
                    if (sim == 1 && year <= 3)
                    {
                        Console.WriteLine($"[SHOCK GEN] Sim#{sim}, Year {year}:");
                        foreach (var kvp in sectorShocks.Take(3))
                        {
                            Console.WriteLine($"  Sector {kvp.Key}: {kvp.Value:P2}");
                        }
                        foreach (var kvp in collateralShocks)
                        {
                            Console.WriteLine($"  Collateral {kvp.Key}: {kvp.Value:P2}");
                        }
                    }
                }
            }

            return shockSet;
        }

        /// <summary>
        /// Generate correlated sector shocks using Cholesky decomposition
        /// </summary>
        private Dictionary<Sector, double> GenerateCorrelatedSectorShocks(
            List<Sector> sectors,
            double[,] correlationMatrix,
            Dictionary<Sector, decimal> volatilities)
        {
            // Generate independent standard normal random variables
            var independentNormals = new double[sectors.Count];
            for (int i = 0; i < sectors.Count; i++)
            {
                independentNormals[i] = GenerateStandardNormal();
            }

            // Apply correlation using matrix multiplication
            var correlatedNormals = GenerateCorrelatedNormals(correlationMatrix, independentNormals);

            // Create shock dictionary with volatility scaling
            var shocks = new Dictionary<Sector, double>();
            for (int i = 0; i < sectors.Count; i++)
            {
                // Shock = correlated normal * volatility
                shocks[sectors[i]] = correlatedNormals[i] * (double)volatilities[sectors[i]];
            }

            return shocks;
        }

        /// <summary>
        /// Generate correlated collateral shocks based on sector shocks
        /// </summary>
        private Dictionary<string, double> GenerateCorrelatedCollateralShocks(
            List<string> collateralTypes,
            Dictionary<Sector, double> sectorShocks,
            List<Sector> sectors,
            Dictionary<(Sector, string), decimal> sectorCollateralCorrelations,
            Dictionary<string, decimal> collateralVolatilities)
        {
            var collateralShocks = new Dictionary<string, double>();

            foreach (var propertyType in collateralTypes)
            {
                // For collateral, we use a simpler approach:
                // Generate an independent normal shock scaled by collateral volatility
                double volatility = (double)collateralVolatilities[propertyType];
                double independentNormal = GenerateStandardNormal();
                
                // Apply correlation with sector shocks (weighted average)
                double correlatedComponent = 0;
                double totalCorrelation = 0;

                foreach (var sector in sectors)
                {
                    var correlationKey = (sector, propertyType);
                    if (sectorCollateralCorrelations.TryGetValue(correlationKey, out decimal correlation) &&
                        sectorShocks.TryGetValue(sector, out double sectorShock))
                    {
                        correlatedComponent += (double)correlation * sectorShock;
                        totalCorrelation += Math.Abs((double)correlation);
                    }
                }

                // Normalize the correlated component by total correlation weight
                // This prevents amplification when summing multiple correlated sectors
                if (totalCorrelation > 0)
                {
                    correlatedComponent = correlatedComponent / totalCorrelation;
                }

                // The final shock is a blend of correlated (from sectors) and independent components
                // If totalCorrelation is high, use more of the correlated shock
                // If totalCorrelation is low, use more of the independent shock
                double correlationStrength = Math.Min(1.0, totalCorrelation);
                double independentStrength = Math.Sqrt(Math.Max(0, 1.0 - Math.Pow(correlationStrength, 2)));
                
                collateralShocks[propertyType] = correlatedComponent + independentNormal * independentStrength * volatility;
            }

            return collateralShocks;
        }

        /// <summary>
        /// Apply Cholesky decomposition to correlation matrix and multiply by independent normals
        /// </summary>
        private double[] GenerateCorrelatedNormals(double[,] correlationMatrix, double[] independentNormals)
        {
            int n = independentNormals.Length;
            var cholesky = CholeskyDecomposition(correlationMatrix);
            
            var correlatedNormals = new double[n];
            for (int i = 0; i < n; i++)
            {
                correlatedNormals[i] = 0;
                for (int j = 0; j <= i; j++)
                {
                    correlatedNormals[i] += cholesky[i, j] * independentNormals[j];
                }
            }

            return correlatedNormals;
        }

        /// <summary>
        /// Cholesky decomposition of a symmetric positive definite matrix
        /// </summary>
        private double[,] CholeskyDecomposition(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            var L = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < j; k++)
                    {
                        sum += L[i, k] * L[j, k];
                    }

                    if (i == j)
                    {
                        L[i, j] = Math.Sqrt(Math.Max(0, matrix[i, i] - sum));
                    }
                    else
                    {
                        L[i, j] = L[j, j] > 0 ? (matrix[i, j] - sum) / L[j, j] : 0;
                    }
                }
            }

            return L;
        }

        /// <summary>
        /// Generate independent collateral shocks (no sector correlation)
        /// Used when sector data is not available
        /// </summary>
        public SimulationShockSet GenerateIndependentCollateralShocks(
            int numberOfSimulations,
            int simulationYears,
            List<string> collateralTypes,
            Dictionary<string, decimal> collateralVolatilities)
        {
            var shockSet = new SimulationShockSet();
            
            Console.WriteLine($"[SHOCK GENERATION] ===== INDEPENDENT SHOCK GENERATION CALLED =====");
            Console.WriteLine($"[SHOCK GENERATION] Generating independent shocks for {numberOfSimulations} simulations, {simulationYears} years");
            Console.WriteLine($"[SHOCK GENERATION] Collateral types: {string.Join(", ", collateralTypes)}");
            Console.WriteLine($"[SHOCK GENERATION] Volatilities: {string.Join(", ", collateralVolatilities.Select(kvp => $"{kvp.Key}: {kvp.Value:P2}"))})");

            for (int sim = 1; sim <= numberOfSimulations; sim++)
            {
                shockSet.SectorShocks[sim] = new Dictionary<int, Dictionary<Sector, double>>();
                shockSet.CollateralShocks[sim] = new Dictionary<int, Dictionary<string, double>>();

                for (int year = 1; year <= simulationYears; year++)
                {
                    // Empty sector shocks
                    shockSet.SectorShocks[sim][year] = new Dictionary<Sector, double>();
                    
                    // Generate independent collateral shocks
                    var collateralShocks = new Dictionary<string, double>();
                    foreach (var propertyType in collateralTypes)
                    {
                        double volatility = (double)collateralVolatilities[propertyType];
                        collateralShocks[propertyType] = GenerateStandardNormal() * volatility;
                    }
                    
                    shockSet.CollateralShocks[sim][year] = collateralShocks;

                    // Log first simulation for debugging
                    if (sim == 1 && year <= 3)
                    {
                        Console.WriteLine($"[SHOCK GEN] Sim#{sim}, Year {year}:");
                        foreach (var kvp in collateralShocks)
                        {
                            Console.WriteLine($"  Collateral {kvp.Key}: {kvp.Value:P2}");
                        }
                    }
                }
            }

            return shockSet;
        }

        /// <summary>
        /// Generate a standard normal random variable using Box-Muller transform
        /// </summary>
        private double GenerateStandardNormal()
        {
            double u1 = 1.0 - _random.NextDouble(); // Uniform(0,1] random
            double u2 = 1.0 - _random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }
}
