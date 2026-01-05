using System.Text.Json;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class EcbApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EcbApiService> _logger;

        // ECB API base URL
        private const string ECB_API_BASE = "https://data-api.ecb.europa.eu/service/data";
        
        // Dataset for Yield Curves (AAA-rated euro area government bonds)
        private const string YIELD_CURVE_DATASET = "YC/B.U2.EUR.4F.G_N_A.SV_C_YM";
        private const string YIELD_CURVE_MATURITIES = "SR_3M+SR_6M+SR_1Y+SR_2Y+SR_3Y+SR_5Y+SR_7Y+SR_10Y+SR_15Y+SR_20Y+SR_30Y";
        
        // Dataset for 1-year EURIBOR/IBOR (for BSE reference rate)
        // FM - Financial Market rates, M = Monthly frequency
        private const string EURIBOR_DATASET = "FM/M.U2.EUR.RT.MM.EURIBOR1YD_.HSTA";

        public EcbApiService(HttpClient httpClient, ILogger<EcbApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Get the latest yield curve for AAA-rated euro area government bonds
        /// Used for loan pricing (tariff base rate)
        /// </summary>
        public async Task<YieldCurveResponse> GetYieldCurveAsync()
        {
            try
            {
                var url = $"{ECB_API_BASE}/{YIELD_CURVE_DATASET}.{YIELD_CURVE_MATURITIES}?format=jsondata&lastNObservations=1";
                
                _logger.LogInformation("Fetching yield curve from ECB: {Url}", url);
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var ecbData = JsonDocument.Parse(content);
                
                var result = new YieldCurveResponse
                {
                    Data = new List<YieldCurveDataPoint>(),
                    LastUpdated = DateTime.UtcNow.ToString("o")
                };

                // Parse ECB response structure
                var dataSets = ecbData.RootElement.GetProperty("dataSets");
                if (dataSets.GetArrayLength() > 0)
                {
                    var observations = dataSets[0].GetProperty("series");
                    var structure = ecbData.RootElement.GetProperty("structure");
                    var dimensions = structure.GetProperty("dimensions").GetProperty("series");
                    
                    // Get maturity labels
                    var dataTypeDimension = dimensions.EnumerateArray()
                        .FirstOrDefault(d => d.GetProperty("id").GetString() == "DATA_TYPE_FM");
                    
                    if (dataTypeDimension.ValueKind != JsonValueKind.Undefined)
                    {
                        var values = dataTypeDimension.GetProperty("values");
                        var maturityMap = new Dictionary<string, string>(); // Map from id (SR_10Y) to name
                        
                        foreach (var value in values.EnumerateArray())
                        {
                            var id = value.GetProperty("id").GetString();
                            var name = value.GetProperty("name").GetString();
                            if (id != null && name != null)
                            {
                                maturityMap[id] = name;
                            }
                        }

                        // Extract observation values
                        foreach (var series in observations.EnumerateObject())
                        {
                            var seriesKey = series.Name;
                            var seriesData = series.Value;
                            
                            if (seriesData.TryGetProperty("observations", out var obs))
                            {
                                foreach (var observation in obs.EnumerateObject())
                                {
                                    var valueArray = observation.Value;
                                    if (valueArray.GetArrayLength() > 0)
                                    {
                                        var rate = valueArray[0].GetDecimal();
                                        
                                        // Extract maturity from series key (format: "0:0:0:0:X:0:0")
                                        // Position 4 is the DATA_TYPE_FM dimension index
                                        var keyParts = seriesKey.Split(':');
                                        if (keyParts.Length > 4 && int.TryParse(keyParts[4], out int maturityIndex))
                                        {
                                            // Look up the maturity id by iterating the map
                                            var maturityIds = maturityMap.Keys.ToList();
                                            if (maturityIndex >= 0 && maturityIndex < maturityIds.Count)
                                            {
                                                var maturityId = maturityIds[maturityIndex];
                                                // Parse maturity id like "SR_3M" -> "3M", "SR_10Y" -> "10Y"
                                                var parsedMaturity = ParseMaturityFromId(maturityId);
                                                
                                                result.Data.Add(new YieldCurveDataPoint
                                                {
                                                    Date = result.LastUpdated,
                                                    Maturity = parsedMaturity,
                                                    Rate = rate
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation("Successfully fetched {Count} yield curve data points", result.Data.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching yield curve from ECB");
                throw;
            }
        }

        /// <summary>
        /// Get the BSE reference rate based on 1-year EURIBOR following EU methodology
        /// Base rate = Sept-Oct-Nov average of previous year
        /// If 3-month rolling average deviates >15% from base rate, 
        /// the rate is updated 2 months after detection with the 3-month average at that time
        /// </summary>
        public async Task<BseReferenceRateResponse> GetBseReferenceRateAsync()
        {
            var now = DateTime.Now;
            var previousYear = now.Year - 1;
            
            // Start with the annual base rate (Sept-Oct-Nov of previous year)
            var annualBaseStartDate = new DateTime(previousYear, 9, 1);
            var annualBaseEndDate = new DateTime(previousYear, 11, 30);
            
            _logger.LogInformation("Calculating EU BSE reference rate with 15% deviation check");
            
            // Fetch all monthly data from Sept of previous year until now
            var dataStartDate = annualBaseStartDate.ToString("yyyy-MM-dd");
            var dataEndDate = now.ToString("yyyy-MM-dd");
            
            var url = $"{ECB_API_BASE}/{EURIBOR_DATASET}?format=jsondata&startPeriod={dataStartDate}&endPeriod={dataEndDate}";
            
            _logger.LogInformation("Fetching 1-year EURIBOR data from ECB: {Url}", url);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var ecbData = JsonDocument.Parse(content);

            // Parse all monthly rates with their dates
            var monthlyRates = new List<(DateTime date, decimal rate)>();
            
            var dataSets = ecbData.RootElement.GetProperty("dataSets");
            if (dataSets.GetArrayLength() > 0)
            {
                var series = dataSets[0].GetProperty("series");
                var structure = ecbData.RootElement.GetProperty("structure");
                var dimensions = structure.GetProperty("dimensions").GetProperty("observation");
                
                // Get time period dimension
                var timeDimension = dimensions.EnumerateArray().FirstOrDefault(d => d.GetProperty("id").GetString() == "TIME_PERIOD");
                var timeValues = timeDimension.GetProperty("values");
                
                foreach (var seriesItem in series.EnumerateObject())
                {
                    if (seriesItem.Value.TryGetProperty("observations", out var observations))
                    {
                        foreach (var obs in observations.EnumerateObject())
                        {
                            var timeIndex = int.Parse(obs.Name);
                            var valueArray = obs.Value;
                            
                            if (valueArray.GetArrayLength() > 0 && timeIndex < timeValues.GetArrayLength())
                            {
                                var rate = valueArray[0].GetDecimal();
                                var timeStr = timeValues[timeIndex].GetProperty("id").GetString();
                                
                                if (DateTime.TryParse(timeStr + "-01", out var date))
                                {
                                    monthlyRates.Add((date, rate));
                                }
                            }
                        }
                    }
                }
            }

            if (monthlyRates.Count == 0)
            {
                throw new Exception($"No 1-year EURIBOR data available for period {dataStartDate} to {dataEndDate}");
            }

            // Sort by date
            monthlyRates = monthlyRates.OrderBy(r => r.date).ToList();
            
            // Calculate annual base rate (Sept-Oct-Nov average)
            var annualBaseRates = monthlyRates
                .Where(r => r.date >= annualBaseStartDate && r.date <= annualBaseEndDate)
                .Select(r => r.rate)
                .ToList();
            
            if (annualBaseRates.Count == 0)
            {
                throw new Exception($"No data available for Sept-Oct-Nov {previousYear}");
            }
            
            decimal currentBaseRate = annualBaseRates.Average();
            var currentBaseRatePeriod = $"{previousYear}-09 to {previousYear}-11";
            
            // Check for 15% deviation and apply 2-month offset rule
            for (int i = 2; i < monthlyRates.Count; i++)
            {
                // Calculate 3-month rolling average ending at current month
                var threeMonthAvg = monthlyRates.Skip(i - 2).Take(3).Average(r => r.rate);
                var currentMonth = monthlyRates[i].date;
                var deviationPercent = Math.Abs((threeMonthAvg - currentBaseRate) / currentBaseRate * 100);
                
                _logger.LogInformation("Month {Month}: 3-month avg = {Avg}%, base rate = {Base}%, deviation = {Dev}%",
                    currentMonth.ToString("yyyy-MM"), Math.Round(threeMonthAvg, 4), Math.Round(currentBaseRate, 4), Math.Round(deviationPercent, 2));
                
                if (deviationPercent > 15)
                {
                    _logger.LogInformation("Deviation >15% detected at {Month} (3-month avg ending at this month)", currentMonth.ToString("yyyy-MM"));
                    
                    // Deviation detected at month i with 3-month average ending at month i
                    // New base rate becomes effective 1 month later (i + 1)
                    // The new rate is the 3-month average ending at the detection month (i)
                    var effectiveIndex = i + 1;  // 1 month after detection
                    
                    if (effectiveIndex < monthlyRates.Count)
                    {
                        var effectiveDate = monthlyRates[effectiveIndex].date;
                        
                        // Only apply if we've reached that date
                        if (effectiveDate <= now)
                        {
                            // Use 3-month average ending at detection month (months i-2, i-1, i)
                            var newRates = monthlyRates.Skip(i - 2).Take(3).ToList();
                            currentBaseRate = newRates.Average(r => r.rate);
                            currentBaseRatePeriod = $"{newRates[0].date:yyyy-MM} to {newRates[2].date:yyyy-MM}";
                            
                            _logger.LogInformation("New base rate {NewRate}% effective from {EffectiveDate} (calculated from period: {Period})",
                                Math.Round(currentBaseRate, 4), effectiveDate.ToString("yyyy-MM"), currentBaseRatePeriod);
                            
                            // Continue from the effective date to check for further deviations
                            i = effectiveIndex;
                        }
                        else
                        {
                            _logger.LogInformation("Deviation detected but effective date {EffectiveDate} not yet reached", effectiveDate.ToString("yyyy-MM"));
                        }
                    }
                }
            }
            
            _logger.LogInformation("Final BSE reference rate: {Rate}% (period: {Period})", 
                currentBaseRate, currentBaseRatePeriod);

            return new BseReferenceRateResponse
            {
                ReferenceRate = Math.Round(currentBaseRate, 2),
                CalculationPeriod = currentBaseRatePeriod,
                ObservationCount = monthlyRates.Count,
                CalculatedAt = DateTime.UtcNow.ToString("o"),
                IsOfficialRate = true,
                DataSource = EURIBOR_DATASET,
                WarningMessage = null
            };
        }

        /// <summary>
        /// Parse ECB maturity ID to standard format
        /// Examples: "SR_3M" -> "3M", "SR_10Y" -> "10Y"
        /// </summary>
        private string ParseMaturityFromId(string id)
        {
            // Remove "SR_" prefix to get the maturity
            if (id.StartsWith("SR_"))
            {
                return id.Substring(3);
            }
            
            _logger.LogWarning("Could not parse maturity id: {Id}", id);
            return id;
        }

        /// <summary>
        /// Parse ECB maturity labels to standard format (kept for backwards compatibility)
        /// Examples: "Yield curve spot rate, 3-month maturity" -> "3M"
        ///          "Yield curve spot rate, 10-year maturity" -> "10Y"
        /// </summary>
        private string ParseMaturityLabel(string label)
        {
            // Extract the numeric value and unit (month/year)
            if (label.Contains("month"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(label, @"(\d+)-month");
                if (match.Success)
                {
                    return $"{match.Groups[1].Value}M";
                }
            }
            else if (label.Contains("year"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(label, @"(\d+)-year");
                if (match.Success)
                {
                    return $"{match.Groups[1].Value}Y";
                }
            }
            
            // Fallback: return the label as-is
            _logger.LogWarning("Could not parse maturity label: {Label}", label);
            return label;
        }
    }

    public class YieldCurveResponse
    {
        public List<YieldCurveDataPoint> Data { get; set; } = new();
        public string LastUpdated { get; set; } = string.Empty;
    }

    public class YieldCurveDataPoint
    {
        public string Date { get; set; } = string.Empty;
        public string Maturity { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }

    public class BseReferenceRateResponse
    {
        public decimal ReferenceRate { get; set; }
        public string CalculationPeriod { get; set; } = string.Empty;
        public int ObservationCount { get; set; }
        public string CalculatedAt { get; set; } = string.Empty;
        public bool IsOfficialRate { get; set; }
        public string? DataSource { get; set; }
        public string? WarningMessage { get; set; }
    }
}
