using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models.Debtor;
using LoanAnnuityCalculatorAPI.Models.Loan;
using System.Globalization;
using System.Text;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoanImportController : ControllerBase
    {
        private readonly LoanDbContext _dbContext;
        private readonly ILogger<LoanImportController> _logger;

        public LoanImportController(LoanDbContext dbContext, ILogger<LoanImportController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost("upload-csv")]
        public async Task<IActionResult> UploadLoansFromCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded or file is empty." });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "File must be a CSV file." });
            }

            var importStats = new
            {
                TotalRows = 0,
                DebtorsCreated = 0,
                DebtorsUpdated = 0,
                LoansCreated = 0,
                LoansUpdated = 0,
                Errors = new List<string>()
            };

            var stats = new ImportStatistics();
            var debtorCache = new Dictionary<string, DebtorDetails>(); // Cache to avoid duplicate lookups/creations

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
                {
                    // Read header line
                    var headerLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(headerLine))
                    {
                        return BadRequest(new { message = "CSV file is empty or has no header." });
                    }

                    var headers = ParseCsvLine(headerLine);
                    var columnMap = MapColumns(headers);

                    int rowNumber = 1;
                    string? line;
                    
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        rowNumber++;
                        stats.TotalRows++;

                        try
                        {
                            var values = ParseCsvLine(line);
                            
                            if (values.Length != headers.Length)
                            {
                                stats.Errors.Add($"Row {rowNumber}: Column count mismatch");
                                continue;
                            }

                            var row = new Dictionary<string, string>();
                            for (int i = 0; i < headers.Length; i++)
                            {
                                row[headers[i]] = values[i];
                            }

                            await ProcessRow(row, columnMap, stats, rowNumber, debtorCache);
                        }
                        catch (Exception ex)
                        {
                            stats.Errors.Add($"Row {rowNumber}: {ex.Message}");
                            _logger.LogError(ex, "Error processing row {RowNumber}", rowNumber);
                        }
                    }

                    // Save all changes
                    await _dbContext.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "CSV import completed",
                    totalRows = stats.TotalRows,
                    debtorsCreated = stats.DebtorsCreated,
                    debtorsUpdated = stats.DebtorsUpdated,
                    loansCreated = stats.LoansCreated,
                    loansUpdated = stats.LoansUpdated,
                    errors = stats.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CSV import");
                return StatusCode(500, new { message = "Error during import", error = ex.Message });
            }
        }

        private async Task ProcessRow(Dictionary<string, string> row, Dictionary<string, int> columnMap, ImportStatistics stats, int rowNumber, Dictionary<string, DebtorDetails> debtorCache)
        {
            // Extract debtor information
            var debtorName = GetValue(row, columnMap, "Hoofdaanvrager");
            if (string.IsNullOrWhiteSpace(debtorName))
            {
                stats.Errors.Add($"Row {rowNumber}: Missing debtor name");
                return;
            }

            // Check cache first to avoid duplicate database queries
            DebtorDetails? debtor;
            if (debtorCache.TryGetValue(debtorName, out debtor))
            {
                // Debtor already processed in this import
            }
            else
            {
                // Check for fuzzy match in cache first (typo detection)
                var similarDebtor = FindSimilarDebtor(debtorName, debtorCache.Keys);
                if (similarDebtor != null)
                {
                    debtor = debtorCache[similarDebtor];
                    debtorCache[debtorName] = debtor; // Add variant to cache
                    _logger.LogInformation($"Row {rowNumber}: Matched '{debtorName}' to existing debtor '{similarDebtor}' (typo detected)");
                }
                else
                {
                    // Find or create debtor in database
                    debtor = await _dbContext.DebtorDetails
                        .FirstOrDefaultAsync(d => d.DebtorName == debtorName);
                    
                    // If not found by exact match, check for similar names in database
                    if (debtor == null)
                    {
                        var allDebtors = await _dbContext.DebtorDetails
                            .Select(d => d.DebtorName)
                            .ToListAsync();
                        
                        var similarInDb = FindSimilarDebtor(debtorName, allDebtors);
                        if (similarInDb != null)
                        {
                            debtor = await _dbContext.DebtorDetails
                                .FirstOrDefaultAsync(d => d.DebtorName == similarInDb);
                            _logger.LogInformation($"Row {rowNumber}: Matched '{debtorName}' to existing database debtor '{similarInDb}' (typo detected)");
                        }
                    }

                if (debtor == null)
                {
                    debtor = new DebtorDetails
                    {
                        DebtorName = debtorName,
                        Street = GetValue(row, columnMap, "Straat"),
                        HouseNumber = CombineHouseNumber(
                            GetValue(row, columnMap, "Huisnummer"),
                            GetValue(row, columnMap, "Huisnummer toevoeging")
                        ),
                        PostalCode = GetValue(row, columnMap, "Postcode"),
                        City = GetValue(row, columnMap, "Plaats"),
                        IsProspect = false
                    };

                    _dbContext.DebtorDetails.Add(debtor);
                    await _dbContext.SaveChangesAsync(); // Save to get DebtorID
                    stats.DebtorsCreated++;
                }
                else
                {
                    // Update debtor address if provided and currently empty
                    bool updated = false;
                    
                    if (string.IsNullOrEmpty(debtor.Street))
                    {
                        debtor.Street = GetValue(row, columnMap, "Straat");
                        updated = true;
                    }
                    
                    if (string.IsNullOrEmpty(debtor.HouseNumber))
                    {
                        debtor.HouseNumber = CombineHouseNumber(
                            GetValue(row, columnMap, "Huisnummer"),
                            GetValue(row, columnMap, "Huisnummer toevoeging")
                        );
                        updated = true;
                    }
                    
                    if (string.IsNullOrEmpty(debtor.PostalCode))
                    {
                        debtor.PostalCode = GetValue(row, columnMap, "Postcode");
                        updated = true;
                    }
                    
                    if (string.IsNullOrEmpty(debtor.City))
                    {
                        debtor.City = GetValue(row, columnMap, "Plaats");
                        updated = true;
                    }

                    if (updated)
                    {
                        stats.DebtorsUpdated++;
                    }
                }

                    // Add to cache
                    debtorCache[debtorName] = debtor;
                }
            }

            // Extract loan information
            var externalLoanNumber = GetValue(row, columnMap, "Leningnummer");
            var loanStatus = GetValue(row, columnMap, "Leningstatus");
            
            // Parse loan amount ("Leningdeel hoofdsom")
            var loanAmountStr = GetValue(row, columnMap, "Leningdeel hoofdsom");
            if (!TryParseDecimal(loanAmountStr, out decimal loanAmount))
            {
                stats.Errors.Add($"Row {rowNumber}: Invalid loan amount '{loanAmountStr}'");
                return;
            }

            // Parse interest rate ("Rente")
            var interestRateStr = GetValue(row, columnMap, "Rente");
            if (!TryParseDecimal(interestRateStr, out decimal interestRate))
            {
                stats.Errors.Add($"Row {rowNumber}: Invalid interest rate '{interestRateStr}'");
                return;
            }

            // Parse tenor/term ("Looptijd")
            var tenorStr = GetValue(row, columnMap, "Looptijd");
            int tenorMonths = ParseTenorToMonths(tenorStr);
            if (tenorMonths <= 0)
            {
                stats.Errors.Add($"Row {rowNumber}: Invalid tenor '{tenorStr}'");
                return;
            }

            // Parse start date ("Ingangsdatum")
            var startDateStr = GetValue(row, columnMap, "Ingangsdatum");
            if (!TryParseDate(startDateStr, out DateTime startDate))
            {
                stats.Errors.Add($"Row {rowNumber}: Invalid start date '{startDateStr}'");
                return;
            }

            // Check if loan already exists (by external reference or debtor + amount + date)
            var existingLoan = await _dbContext.Loans
                .FirstOrDefaultAsync(l => 
                    l.DebtorID == debtor.DebtorID &&
                    l.LoanAmount == loanAmount &&
                    l.StartDate == startDate);

            if (existingLoan == null)
            {
                var loan = new Loan
                {
                    DebtorID = debtor.DebtorID,
                    LoanAmount = loanAmount,
                    AnnualInterestRate = interestRate,
                    TenorMonths = tenorMonths,
                    InterestOnlyMonths = 0, // Not provided in CSV
                    StartDate = startDate,
                    Status = MapLoanStatus(loanStatus),
                    RedemptionSchedule = "Annuity" // Default, can be enhanced
                };

                _dbContext.Loans.Add(loan);
                stats.LoansCreated++;
            }
            else
            {
                // Update existing loan
                existingLoan.AnnualInterestRate = interestRate;
                existingLoan.Status = MapLoanStatus(loanStatus);
                stats.LoansUpdated++;
            }
        }

        private Dictionary<string, int> MapColumns(string[] headers)
        {
            var map = new Dictionary<string, int>();
            for (int i = 0; i < headers.Length; i++)
            {
                map[headers[i].Trim()] = i;
            }
            return map;
        }

        private string GetValue(Dictionary<string, string> row, Dictionary<string, int> columnMap, string columnName)
        {
            if (columnMap.TryGetValue(columnName, out int index))
            {
                var key = columnMap.Keys.ElementAt(index);
                if (row.TryGetValue(key, out string? value))
                {
                    return value?.Trim() ?? string.Empty;
                }
            }
            return string.Empty;
        }

        private string CombineHouseNumber(string number, string addition)
        {
            var result = number?.Trim() ?? string.Empty;
            var add = addition?.Trim() ?? string.Empty;
            
            if (!string.IsNullOrEmpty(add) && add != "\"\"")
            {
                result += " " + add;
            }
            
            return result.Trim();
        }

        private bool TryParseDecimal(string value, out decimal result)
        {
            // Handle Dutch number format (comma as decimal separator)
            value = value?.Replace(".", "").Replace(",", ".");
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        private bool TryParseDate(string value, out DateTime result)
        {
            // Try different date formats
            string[] formats = { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "yyyy/MM/dd" };
            return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out result);
        }

        private int ParseTenorToMonths(string tenor)
        {
            // Handle values like "180", "360", "60", etc.
            if (int.TryParse(tenor, out int months))
            {
                return months;
            }

            // Handle potential text formats like "15 jaar", "5 jaar", etc.
            tenor = tenor.ToLower().Trim();
            if (tenor.Contains("jaar"))
            {
                var parts = tenor.Split(' ');
                if (parts.Length > 0 && int.TryParse(parts[0], out int years))
                {
                    return years * 12;
                }
            }

            return 0;
        }

        private string MapLoanStatus(string status)
        {
            return status switch
            {
                "Lopend" => "Active",
                "Beëindigd" => "Closed",
                "Afgelost" => "Paid Off",
                _ => status
            };
        }

        private string? FindSimilarDebtor(string name, IEnumerable<string> existingNames)
        {
            var normalizedName = NormalizeName(name);
            const int maxDistance = 2; // Allow up to 2 character differences
            
            foreach (var existing in existingNames)
            {
                var normalizedExisting = NormalizeName(existing);
                var distance = LevenshteinDistance(normalizedName, normalizedExisting);
                
                // If very similar (1-2 chars different) and similar length
                if (distance <= maxDistance && 
                    Math.Abs(normalizedName.Length - normalizedExisting.Length) <= 2)
                {
                    return existing;
                }
            }
            
            return null;
        }

        private string NormalizeName(string name)
        {
            // Remove extra spaces, convert to lowercase for comparison
            return string.Join(" ", name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .ToLowerInvariant();
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
            if (string.IsNullOrEmpty(s2)) return s1.Length;

            var d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;
            
            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ';' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            // Add the last field
            result.Add(currentField.ToString());

            return result.ToArray();
        }

        private class ImportStatistics
        {
            public int TotalRows { get; set; }
            public int DebtorsCreated { get; set; }
            public int DebtorsUpdated { get; set; }
            public int LoansCreated { get; set; }
            public int LoansUpdated { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }
    }
}
