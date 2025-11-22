using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LoanAnnuityCalculatorAPI.Data;
using LoanAnnuityCalculatorAPI.Models;
using System.IO;

namespace LoanAnnuityCalculatorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication for contract generation
    public class ContractGeneratorController : ControllerBase
    {
        private readonly LoanDbContext _context;
        private readonly ILogger<ContractGeneratorController> _logger;

        public ContractGeneratorController(LoanDbContext context, ILogger<ContractGeneratorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Request model for generating a contract document
        /// </summary>
        public class GenerateContractRequest
        {
            public decimal LoanAmount { get; set; }
            public int LoanTermMonths { get; set; }
            public decimal InterestRate { get; set; }
            public decimal MonthlyPayment { get; set; }
            public decimal TotalPayment { get; set; }
            public decimal TotalInterest { get; set; }
            public decimal LTV { get; set; }
            public decimal CollateralValue { get; set; }
            public string CreditRating { get; set; } = string.Empty;
            public string RedemptionScheme { get; set; } = string.Empty;
            public int InterestOnlyMonths { get; set; }
            public decimal BaseRate { get; set; }
            public decimal LtvSpread { get; set; }
            public decimal RatingSpread { get; set; }
            
            // Optional customer information (legacy)
            public string? CustomerName { get; set; }
            public string? CustomerAddress { get; set; }
            public string? ContractDate { get; set; }

            // Debtor company information
            public string? DebtorName { get; set; }
            
            // Contact person details
            public string? ContactPerson { get; set; }
            public string? ContactCallingName { get; set; }
            public string? ContactFirstNames { get; set; }
            public string? ContactLastName { get; set; }
            
            // Address details
            public string? Street { get; set; }
            public string? HouseNumber { get; set; }
            public string? PostalCode { get; set; }
            public string? City { get; set; }
            
            // Contact information
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            
            // Signatories
            public string? Signatory1Name { get; set; }
            public string? Signatory1Function { get; set; }
            public string? Signatory2Name { get; set; }
            public string? Signatory2Function { get; set; }
            public string? Signatory3Name { get; set; }
            public string? Signatory3Function { get; set; }

            // Chart image (base64 encoded PNG/JPEG from frontend)
            public string? ChartImageBase64 { get; set; }

            // Security type for conditional text blocks
            // Values: "FirstLien", "SecondLien", "NoSecurity"
            public string? SecurityType { get; set; }

            // Payment schedule data for chart
            public List<PaymentScheduleItem>? PaymentSchedule { get; set; }

            // BSE calculation data for table
            public BseBreakdownData? BseBreakdown { get; set; }
        }

        public class PaymentScheduleItem
        {
            public int Month { get; set; }
            public decimal InterestComponent { get; set; }
            public decimal CapitalComponent { get; set; }
            public decimal RemainingLoan { get; set; }
        }

        public class BseBreakdownData
        {
            public decimal MarketRate { get; set; }
            public decimal LoanRate { get; set; }
            public decimal TotalBSE { get; set; }
            public List<YearlyBreakdownItem>? YearlyBreakdown { get; set; }
        }

        public class YearlyBreakdownItem
        {
            public int Year { get; set; }
            public decimal MarketInterest { get; set; }
            public decimal LoanInterest { get; set; }
            public decimal Difference { get; set; }
            public decimal DiscountedValue { get; set; }
        }

        /// <summary>
        /// Generate a Word document contract from text blocks and loan data
        /// POST: api/ContractGenerator/generate
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateContract([FromBody] GenerateContractRequest request)
        {
            try
            {
                _logger.LogInformation("Generating contract document for loan amount {Amount}", request.LoanAmount);

                // Fetch all active text blocks ordered by section and sort order
                // Filter based on redemption schedule type and security type
                var textBlocks = await _context.ContractTextBlocks
                    .Where(b => b.IsActive &&
                        // Include blocks with no redemption schedule filter OR matching redemption schedule
                        (b.RedemptionScheduleType == null || b.RedemptionScheduleType == request.RedemptionScheme) &&
                        // Include blocks with no security type filter OR matching security type
                        (b.SecurityType == null || b.SecurityType == request.SecurityType))
                    .OrderBy(b => b.Section)
                    .ThenBy(b => b.SortOrder)
                    .ToListAsync();

                if (!textBlocks.Any())
                {
                    return BadRequest("No active contract text blocks found. Please configure text blocks in settings.");
                }

                // Generate the Word document
                var documentBytes = GenerateWordDocument(textBlocks, request);

                // Return the document as a file download
                var fileName = $"Contract_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
                return File(documentBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contract document");
                return StatusCode(500, "Error generating contract document: " + ex.Message);
            }
        }

        private byte[] GenerateWordDocument(List<ContractTextBlock> textBlocks, GenerateContractRequest data)
        {
            using var memoryStream = new MemoryStream();
            using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
            {
                // Add main document part
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Add title
                AddHeading(body, "Leningovereenkomst", 1);
                AddParagraph(body, "");

                // Add contract date if provided
                if (!string.IsNullOrEmpty(data.ContractDate))
                {
                    AddParagraph(body, $"Datum: {data.ContractDate}");
                    AddParagraph(body, "");
                }

                // Group text blocks by section
                var sections = textBlocks.GroupBy(b => b.Section);

                foreach (var section in sections)
                {
                    // Add section heading only if the first block in this section has ShowSectionHeader = true
                    var firstBlock = section.FirstOrDefault();
                    if (firstBlock?.ShowSectionHeader == true)
                    {
                        AddHeading(body, section.Key, 2);
                    }

                    // Add all text blocks in this section
                    foreach (var block in section)
                    {
                        var content = ReplaceTokens(block.Content, data);
                        AddFormattedParagraph(body, content);

                        // Check if we need to insert charts/tables after this block
                        if (block.InsertPaymentChart)
                        {
                            AddParagraph(body, "");
                            AddHeading(body, "Aflossingsschema", 3);
                            
                            // Insert chart image if provided, otherwise show a message
                            if (!string.IsNullOrEmpty(data.ChartImageBase64))
                            {
                                AddChartImage(mainPart, body, data.ChartImageBase64);
                            }
                            else if (data.PaymentSchedule != null && data.PaymentSchedule.Any())
                            {
                                AddPaymentScheduleTable(body, data.PaymentSchedule);
                            }
                            else
                            {
                                AddParagraph(body, "[Grafiek niet beschikbaar - voer eerst een berekening uit]");
                            }
                        }

                        if (block.InsertBseTable && data.BseBreakdown != null)
                        {
                            AddParagraph(body, "");
                            AddHeading(body, "BSE Berekening (Bruto Steun Equivalent)", 3);
                            AddBseBreakdownTable(body, data.BseBreakdown);
                        }
                    }

                    AddParagraph(body, ""); // Add spacing between sections
                }

                // Add loan details table
                AddHeading(body, "Leningspecificaties", 2);
                AddLoanDetailsTable(body, data);

                mainPart.Document.Save();
            }

            return memoryStream.ToArray();
        }

        private void AddHeading(Body body, string text, int level)
        {
            var paragraph = body.AppendChild(new Paragraph());
            var run = paragraph.AppendChild(new Run());
            run.AppendChild(new Text(text));

            var paragraphProperties = paragraph.InsertAt(new ParagraphProperties(), 0);
            paragraphProperties.ParagraphStyleId = new ParagraphStyleId() { Val = $"Heading{level}" };
            
            var runProperties = run.InsertAt(new RunProperties(), 0);
            runProperties.Bold = new Bold();
            runProperties.FontSize = new FontSize() { Val = (level == 1 ? "32" : "28") };
        }

        private void AddParagraph(Body body, string text)
        {
            var paragraph = body.AppendChild(new Paragraph());
            
            // Remove extra spacing between paragraphs
            var paragraphProperties = new ParagraphProperties();
            paragraphProperties.SpacingBetweenLines = new SpacingBetweenLines() 
            { 
                After = "0",
                Line = "240",
                LineRule = LineSpacingRuleValues.Auto 
            };
            paragraph.InsertAt(paragraphProperties, 0);
            
            var run = paragraph.AppendChild(new Run());
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        }

        private void AddFormattedParagraph(Body body, string text)
        {
            // Split on newlines to preserve formatting
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            var i = 0;
            while (i < lines.Length)
            {
                var line = lines[i];
                
                // Check if this is the start of a table (line contains | at start and end)
                if (line.Trim().StartsWith("|") && line.Trim().EndsWith("|"))
                {
                    // Collect all consecutive table lines
                    var tableLines = new List<string>();
                    while (i < lines.Length && lines[i].Trim().StartsWith("|") && lines[i].Trim().EndsWith("|"))
                    {
                        // Skip separator lines (lines with only |, -, and whitespace)
                        if (!System.Text.RegularExpressions.Regex.IsMatch(lines[i].Trim(), @"^\|[\s\-|]+\|$"))
                        {
                            tableLines.Add(lines[i]);
                        }
                        i++;
                    }
                    
                    if (tableLines.Count > 0)
                    {
                        AddMarkdownTable(body, tableLines);
                    }
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    AddParagraph(body, "");
                }
                else
                {
                    AddParagraphWithFormatting(body, line);
                }
                i++;
            }
        }

        private void AddMarkdownTable(Body body, List<string> tableLines)
        {
            if (tableLines.Count == 0) return;

            var table = body.AppendChild(new Table());

            // Table properties with solid continuous borders
            var tableProperties = new TableProperties(
                new TableBorders(
                    new TopBorder() 
                    { 
                        Val = new EnumValue<BorderValues>(BorderValues.Single), 
                        Size = 12,  // Increased from 6 to 12 for more prominent borders
                        Color = "000000"  // Black color
                    },
                    new BottomBorder() 
                    { 
                        Val = new EnumValue<BorderValues>(BorderValues.Single), 
                        Size = 12, 
                        Color = "000000" 
                    },
                    new LeftBorder() 
                    { 
                        Val = new EnumValue<BorderValues>(BorderValues.Single), 
                        Size = 12, 
                        Color = "000000" 
                    },
                    new RightBorder() 
                    { 
                        Val = new EnumValue<BorderValues>(BorderValues.Single), 
                        Size = 12, 
                        Color = "000000" 
                    },
                    new InsideHorizontalBorder() 
                    { 
                        Val = new EnumValue<BorderValues>(BorderValues.Single), 
                        Size = 12, 
                        Color = "000000" 
                    },
                    new InsideVerticalBorder() 
                    { 
                        Val = new EnumValue<BorderValues>(BorderValues.Single), 
                        Size = 12, 
                        Color = "000000" 
                    }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableLook() 
                { 
                    Val = "04A0",  // Professional table look
                    FirstRow = true, 
                    LastRow = false, 
                    FirstColumn = false, 
                    LastColumn = false, 
                    NoHorizontalBand = false, 
                    NoVerticalBand = true 
                }
            );
            table.AppendChild(tableProperties);

            // Process each line as a row
            var isFirstRow = true;
            foreach (var line in tableLines)
            {
                var cells = line.Split('|')
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim())
                    .ToList();

                if (cells.Count > 0)
                {
                    var row = table.AppendChild(new TableRow());
                    
                    foreach (var cellContent in cells)
                    {
                        var cell = row.AppendChild(new TableCell());
                        
                        // Add cell properties with padding
                        var cellProperties = new TableCellProperties(
                            new TableCellMargin(
                                new TopMargin() { Width = "100", Type = TableWidthUnitValues.Dxa },
                                new BottomMargin() { Width = "100", Type = TableWidthUnitValues.Dxa },
                                new LeftMargin() { Width = "100", Type = TableWidthUnitValues.Dxa },
                                new RightMargin() { Width = "100", Type = TableWidthUnitValues.Dxa }
                            )
                        );
                        cell.AppendChild(cellProperties);
                        
                        var cellParagraph = cell.AppendChild(new Paragraph());
                        
                        // Parse inline formatting for cell content
                        ParseInlineFormatting(cellParagraph, cellContent, isFirstRow ? 1 : 0);
                        
                        // Make header row bold by default if no formatting specified
                        if (isFirstRow && !cellContent.Contains("**") && !cellContent.Contains("__"))
                        {
                            var firstRun = cellParagraph.Elements<Run>().FirstOrDefault();
                            if (firstRun != null)
                            {
                                var runProps = firstRun.GetFirstChild<RunProperties>();
                                if (runProps == null)
                                {
                                    runProps = new RunProperties();
                                    firstRun.InsertAt(runProps, 0);
                                }
                                if (runProps.Bold == null)
                                {
                                    runProps.AppendChild(new Bold());
                                }
                            }
                        }
                    }
                    
                    isFirstRow = false;
                }
            }
            
            AddParagraph(body, ""); // Add spacing after table
        }

        private void AddParagraphWithFormatting(Body body, string text)
        {
            var paragraph = body.AppendChild(new Paragraph());
            var paragraphProperties = new ParagraphProperties();
            
            // Remove extra spacing between paragraphs (Word default is ~10pt after)
            paragraphProperties.SpacingBetweenLines = new SpacingBetweenLines() 
            { 
                After = "0",  // No spacing after paragraph
                Line = "240", // Single line spacing (240 twips = 12pt)
                LineRule = LineSpacingRuleValues.Auto 
            };
            
            // Check for alignment markers at the start
            var alignedText = text.TrimStart();
            if (alignedText.StartsWith("[CENTER]"))
            {
                paragraphProperties.Justification = new Justification() { Val = JustificationValues.Center };
                alignedText = alignedText.Substring(8).TrimStart();
            }
            else if (alignedText.StartsWith("[RIGHT]"))
            {
                paragraphProperties.Justification = new Justification() { Val = JustificationValues.Right };
                alignedText = alignedText.Substring(7).TrimStart();
            }
            else if (alignedText.StartsWith("[JUSTIFY]"))
            {
                paragraphProperties.Justification = new Justification() { Val = JustificationValues.Both };
                alignedText = alignedText.Substring(9).TrimStart();
            }
            else if (alignedText.StartsWith("[LEFT]"))
            {
                paragraphProperties.Justification = new Justification() { Val = JustificationValues.Left };
                alignedText = alignedText.Substring(6).TrimStart();
            }
            
            // Check for heading markers
            int headingLevel = 0;
            if (alignedText.StartsWith("#1 "))
            {
                headingLevel = 1;
                alignedText = alignedText.Substring(3);
            }
            else if (alignedText.StartsWith("#2 "))
            {
                headingLevel = 2;
                alignedText = alignedText.Substring(3);
            }
            else if (alignedText.StartsWith("#3 "))
            {
                headingLevel = 3;
                alignedText = alignedText.Substring(3);
            }
            
            // Check for bullet points
            bool isBullet = false;
            if (alignedText.StartsWith("- ") || alignedText.StartsWith("* "))
            {
                isBullet = true;
                alignedText = alignedText.Substring(2);
                
                // Add bullet formatting
                var numberingProperties = new NumberingProperties(
                    new NumberingLevelReference() { Val = 0 },
                    new NumberingId() { Val = 1 }
                );
                paragraphProperties.AppendChild(numberingProperties);
            }
            
            // Check for numbered list
            var numberedMatch = System.Text.RegularExpressions.Regex.Match(alignedText, @"^(\d+)\.\s");
            if (numberedMatch.Success)
            {
                alignedText = alignedText.Substring(numberedMatch.Length);
                
                // Add numbering formatting
                var numberingProperties = new NumberingProperties(
                    new NumberingLevelReference() { Val = 0 },
                    new NumberingId() { Val = 2 }
                );
                paragraphProperties.AppendChild(numberingProperties);
            }
            
            // Apply paragraph properties if any were set
            if (paragraphProperties.HasChildren)
            {
                paragraph.InsertAt(paragraphProperties, 0);
            }
            
            // Parse inline formatting (bold, italic)
            ParseInlineFormatting(paragraph, alignedText, headingLevel);
        }

        private void ParseInlineFormatting(Paragraph paragraph, string text, int headingLevel = 0)
        {
            var position = 0;
            var currentRun = new Run();
            var currentText = new System.Text.StringBuilder();
            
            while (position < text.Length)
            {
                // Check for bold (**text** or __text__)
                if (position < text.Length - 1 && 
                    (text.Substring(position, 2) == "**" || text.Substring(position, 2) == "__"))
                {
                    // Flush current text
                    if (currentText.Length > 0)
                    {
                        AddTextRun(paragraph, currentText.ToString(), false, false, headingLevel);
                        currentText.Clear();
                    }
                    
                    var marker = text.Substring(position, 2);
                    position += 2;
                    var endPos = text.IndexOf(marker, position);
                    
                    if (endPos > position)
                    {
                        var boldText = text.Substring(position, endPos - position);
                        AddTextRun(paragraph, boldText, true, false, headingLevel);
                        position = endPos + 2;
                    }
                }
                // Check for italic (*text* or _text_) - but not ** or __
                else if (position < text.Length - 1 && 
                         (text[position] == '*' || text[position] == '_') &&
                         (position == 0 || text[position - 1] != text[position]))
                {
                    // Flush current text
                    if (currentText.Length > 0)
                    {
                        AddTextRun(paragraph, currentText.ToString(), false, false, headingLevel);
                        currentText.Clear();
                    }
                    
                    var marker = text[position];
                    position++;
                    var endPos = position;
                    
                    // Find matching closing marker
                    while (endPos < text.Length && 
                           (text[endPos] != marker || (endPos < text.Length - 1 && text[endPos + 1] == marker)))
                    {
                        endPos++;
                    }
                    
                    if (endPos < text.Length)
                    {
                        var italicText = text.Substring(position, endPos - position);
                        AddTextRun(paragraph, italicText, false, true, headingLevel);
                        position = endPos + 1;
                    }
                }
                else
                {
                    currentText.Append(text[position]);
                    position++;
                }
            }
            
            // Flush any remaining text
            if (currentText.Length > 0)
            {
                AddTextRun(paragraph, currentText.ToString(), false, false, headingLevel);
            }
        }

        private void AddTextRun(Paragraph paragraph, string text, bool bold, bool italic, int headingLevel)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            var run = paragraph.AppendChild(new Run());
            var runProperties = new RunProperties();
            
            if (bold || headingLevel > 0)
            {
                runProperties.Bold = new Bold();
            }
            
            if (italic)
            {
                runProperties.Italic = new Italic();
            }
            
            // Set font size based on heading level
            if (headingLevel > 0)
            {
                var fontSize = headingLevel switch
                {
                    1 => "32", // 16pt
                    2 => "28", // 14pt
                    3 => "24", // 12pt
                    _ => "22"  // 11pt (default)
                };
                runProperties.FontSize = new FontSize() { Val = fontSize };
            }
            
            if (runProperties.HasChildren)
            {
                run.InsertAt(runProperties, 0);
            }
            
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        }

        private void AddLoanDetailsTable(Body body, GenerateContractRequest data)
        {
            var table = body.AppendChild(new Table());

            // Table properties
            var tableProperties = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
            );
            table.AppendChild(tableProperties);

            // Add rows with loan details
            AddTableRow(table, "Leningbedrag", FormatCurrency(data.LoanAmount));
            AddTableRow(table, "Looptijd", $"{data.LoanTermMonths} maanden ({data.LoanTermMonths / 12.0:F1} jaar)");
            AddTableRow(table, "Rentepercentage", $"{data.InterestRate:F2}%");
            AddTableRow(table, "Aflossingsschema", data.RedemptionScheme);
            
            if (data.InterestOnlyMonths > 0)
            {
                AddTableRow(table, "Aflossingsvrije periode", $"{data.InterestOnlyMonths} maanden");
            }
            
            AddTableRow(table, "Maandlast", FormatCurrency(data.MonthlyPayment));
            AddTableRow(table, "Totaal te betalen", FormatCurrency(data.TotalPayment));
            AddTableRow(table, "Totale rente", FormatCurrency(data.TotalInterest));
            AddTableRow(table, "Onderpandwaarde", FormatCurrency(data.CollateralValue));
            AddTableRow(table, "Loan-to-Value (LTV)", $"{data.LTV:F1}%");
            AddTableRow(table, "Kredietrating", data.CreditRating);
            
            // Rate breakdown
            AddTableRow(table, "Basisrente (ECB)", $"{data.BaseRate:F2}%");
            AddTableRow(table, "LTV Opslag", $"{data.LtvSpread:F2}%");
            AddTableRow(table, "Rating Opslag", $"{data.RatingSpread:F2}%");
        }

        private void AddTableRow(Table table, string label, string value)
        {
            var row = table.AppendChild(new TableRow());

            // Label cell
            var labelCell = row.AppendChild(new TableCell());
            labelCell.Append(new Paragraph(new Run(new Text(label))));
            labelCell.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Pct, Width = "40" }));

            // Value cell
            var valueCell = row.AppendChild(new TableCell());
            var valueParagraph = valueCell.AppendChild(new Paragraph());
            var valueRun = valueParagraph.AppendChild(new Run());
            valueRun.AppendChild(new RunProperties(new Bold()));
            valueRun.AppendChild(new Text(value));
            valueCell.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Pct, Width = "60" }));
        }

        private void AddPaymentScheduleTable(Body body, List<PaymentScheduleItem> schedule)
        {
            var table = body.AppendChild(new Table());

            // Table properties
            var tableProperties = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                ),
                new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
            );
            table.AppendChild(tableProperties);

            // Add header row
            var headerRow = table.AppendChild(new TableRow());
            AddTableCell(headerRow, "Maand", true);
            AddTableCell(headerRow, "Rente", true);
            AddTableCell(headerRow, "Aflossing", true);
            AddTableCell(headerRow, "Restschuld", true);

            // Add data rows (show every 12th month to keep table manageable)
            var filteredSchedule = schedule.Where((item, index) => index % 12 == 0 || index == schedule.Count - 1).ToList();
            
            foreach (var item in filteredSchedule)
            {
                var row = table.AppendChild(new TableRow());
                AddTableCell(row, item.Month.ToString(), false);
                AddTableCell(row, FormatCurrency(item.InterestComponent), false);
                AddTableCell(row, FormatCurrency(item.CapitalComponent), false);
                AddTableCell(row, FormatCurrency(item.RemainingLoan), false);
            }

            AddParagraph(body, "");
            AddParagraph(body, "* Tabel toont jaarlijkse tussenstand en laatste maand");
        }

        private void AddBseBreakdownTable(Body body, BseBreakdownData bseData)
        {
            // Summary
            AddParagraph(body, $"Marktrente: {bseData.MarketRate:F2}%");
            AddParagraph(body, $"Lening rente: {bseData.LoanRate:F2}%");
            AddParagraph(body, $"Totaal BSE: {FormatCurrency(bseData.TotalBSE)}");
            AddParagraph(body, "");

            if (bseData.YearlyBreakdown != null && bseData.YearlyBreakdown.Any())
            {
                var table = body.AppendChild(new Table());

                // Table properties
                var tableProperties = new TableProperties(
                    new TableBorders(
                        new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                    ),
                    new TableWidth() { Width = "5000", Type = TableWidthUnitValues.Pct }
                );
                table.AppendChild(tableProperties);

                // Add header row
                var headerRow = table.AppendChild(new TableRow());
                AddTableCell(headerRow, "Jaar", true);
                AddTableCell(headerRow, "Marktrente", true);
                AddTableCell(headerRow, "Lening rente", true);
                AddTableCell(headerRow, "Verschil", true);
                AddTableCell(headerRow, "Contante Waarde", true);

                // Add data rows
                foreach (var item in bseData.YearlyBreakdown)
                {
                    var row = table.AppendChild(new TableRow());
                    AddTableCell(row, item.Year.ToString(), false);
                    AddTableCell(row, FormatCurrency(item.MarketInterest), false);
                    AddTableCell(row, FormatCurrency(item.LoanInterest), false);
                    AddTableCell(row, FormatCurrency(item.Difference), false);
                    AddTableCell(row, FormatCurrency(item.DiscountedValue), false);
                }
            }
        }

        private void AddTableCell(TableRow row, string text, bool isHeader)
        {
            var cell = row.AppendChild(new TableCell());
            var paragraph = cell.AppendChild(new Paragraph());
            var run = paragraph.AppendChild(new Run());
            
            if (isHeader)
            {
                run.AppendChild(new RunProperties(new Bold()));
            }
            
            run.AppendChild(new Text(text));
        }

        /// <summary>
        /// Replace tokens in text with actual values from loan data
        /// Tokens: {{LoanAmount}}, {{InterestRate}}, {{LoanTerm}}, {{MonthlyPayment}}, etc.
        /// </summary>
        private string ReplaceTokens(string text, GenerateContractRequest data)
        {
            var result = text;

            // Replace monetary values
            result = result.Replace("{{LoanAmount}}", FormatCurrency(data.LoanAmount));
            result = result.Replace("{{MonthlyPayment}}", FormatCurrency(data.MonthlyPayment));
            result = result.Replace("{{TotalPayment}}", FormatCurrency(data.TotalPayment));
            result = result.Replace("{{TotalInterest}}", FormatCurrency(data.TotalInterest));
            result = result.Replace("{{CollateralValue}}", FormatCurrency(data.CollateralValue));

            // Replace percentages
            result = result.Replace("{{InterestRate}}", $"{data.InterestRate:F2}%");
            result = result.Replace("{{LTV}}", $"{data.LTV:F1}%");
            result = result.Replace("{{BaseRate}}", $"{data.BaseRate:F2}%");
            result = result.Replace("{{LtvSpread}}", $"{data.LtvSpread:F2}%");
            result = result.Replace("{{RatingSpread}}", $"{data.RatingSpread:F2}%");

            // Replace other values
            result = result.Replace("{{LoanTermMonths}}", data.LoanTermMonths.ToString());
            result = result.Replace("{{LoanTermYears}}", (data.LoanTermMonths / 12.0).ToString("F1"));
            result = result.Replace("{{CreditRating}}", data.CreditRating);
            result = result.Replace("{{RedemptionScheme}}", data.RedemptionScheme);
            result = result.Replace("{{InterestOnlyMonths}}", data.InterestOnlyMonths.ToString());

            // Replace customer info if provided (legacy fields)
            result = result.Replace("{{CustomerName}}", data.CustomerName ?? "[Klantnaam]");
            result = result.Replace("{{CustomerAddress}}", data.CustomerAddress ?? "[Adres]");
            
            // Replace debtor company information
            result = result.Replace("{{DebtorName}}", data.DebtorName ?? "[Bedrijfsnaam]");
            
            // Replace contact person details
            result = result.Replace("{{ContactPerson}}", data.ContactPerson ?? "[Contactpersoon]");
            result = result.Replace("{{ContactCallingName}}", data.ContactCallingName ?? "[Roepnaam]");
            result = result.Replace("{{ContactFirstNames}}", data.ContactFirstNames ?? "[Voornamen]");
            result = result.Replace("{{ContactLastName}}", data.ContactLastName ?? "[Achternaam]");
            
            // Replace address details
            result = result.Replace("{{Street}}", data.Street ?? "[Straat]");
            result = result.Replace("{{HouseNumber}}", data.HouseNumber ?? "[Huisnummer]");
            result = result.Replace("{{PostalCode}}", data.PostalCode ?? "[Postcode]");
            result = result.Replace("{{City}}", data.City ?? "[Plaats]");
            
            // Replace contact information
            result = result.Replace("{{Email}}", data.Email ?? "[E-mail]");
            result = result.Replace("{{PhoneNumber}}", data.PhoneNumber ?? "[Telefoonnummer]");
            
            // Replace signatories
            result = result.Replace("{{Signatory1Name}}", data.Signatory1Name ?? "[Naam ondertekenaar 1]");
            result = result.Replace("{{Signatory1Function}}", data.Signatory1Function ?? "[Functie ondertekenaar 1]");
            result = result.Replace("{{Signatory2Name}}", data.Signatory2Name ?? "[Naam ondertekenaar 2]");
            result = result.Replace("{{Signatory2Function}}", data.Signatory2Function ?? "[Functie ondertekenaar 2]");
            result = result.Replace("{{Signatory3Name}}", data.Signatory3Name ?? "[Naam ondertekenaar 3]");
            result = result.Replace("{{Signatory3Function}}", data.Signatory3Function ?? "[Functie ondertekenaar 3]");
            
            // Replace dates
            result = result.Replace("{{ContractDate}}", data.ContractDate ?? DateTime.Now.ToString("dd-MM-yyyy"));
            result = result.Replace("{{CurrentDate}}", DateTime.Now.ToString("dd-MM-yyyy"));

            return result;
        }

        private void AddChartImage(MainDocumentPart mainPart, Body body, string base64Image)
        {
            try
            {
                // Remove the data:image/png;base64, prefix if present
                var imageData = base64Image;
                if (imageData.Contains(","))
                {
                    imageData = imageData.Split(',')[1];
                }

                // Convert base64 to byte array
                var imageBytes = Convert.FromBase64String(imageData);

                // Add image part to the document
                var imagePart = mainPart.AddImagePart(ImagePartType.Png);
                using (var stream = new MemoryStream(imageBytes))
                {
                    imagePart.FeedData(stream);
                }

                // Get the relationship ID
                var relationshipId = mainPart.GetIdOfPart(imagePart);

                // Define the image size (in EMUs - English Metric Units)
                // 1 inch = 914400 EMUs, 1 cm = 360000 EMUs
                const long imageWidthEmus = 5486400L;  // ~6 inches / 15cm
                const long imageHeightEmus = 3657600L; // ~4 inches / 10cm

                // Create the drawing element with the image
                var element = new Drawing(
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent() { Cx = imageWidthEmus, Cy = imageHeightEmus },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent()
                        {
                            LeftEdge = 0L,
                            TopEdge = 0L,
                            RightEdge = 0L,
                            BottomEdge = 0L
                        },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties()
                        {
                            Id = 1U,
                            Name = "Chart Image"
                        },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                            new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks() { NoChangeAspect = true }),
                        new DocumentFormat.OpenXml.Drawing.Graphic(
                            new DocumentFormat.OpenXml.Drawing.GraphicData(
                                new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                        new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties()
                                        {
                                            Id = 0U,
                                            Name = "Chart.png"
                                        },
                                        new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()),
                                    new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                        new DocumentFormat.OpenXml.Drawing.Blip(
                                            new DocumentFormat.OpenXml.Drawing.BlipExtensionList(
                                                new DocumentFormat.OpenXml.Drawing.BlipExtension()
                                                {
                                                    Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}"
                                                })
                                        )
                                        {
                                            Embed = relationshipId,
                                            CompressionState = DocumentFormat.OpenXml.Drawing.BlipCompressionValues.Print
                                        },
                                        new DocumentFormat.OpenXml.Drawing.Stretch(
                                            new DocumentFormat.OpenXml.Drawing.FillRectangle())),
                                    new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                        new DocumentFormat.OpenXml.Drawing.Transform2D(
                                            new DocumentFormat.OpenXml.Drawing.Offset() { X = 0L, Y = 0L },
                                            new DocumentFormat.OpenXml.Drawing.Extents() { Cx = imageWidthEmus, Cy = imageHeightEmus }),
                                        new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                            new DocumentFormat.OpenXml.Drawing.AdjustValueList()
                                        )
                                        { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle }))
                            )
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                    )
                    {
                        DistanceFromTop = 0U,
                        DistanceFromBottom = 0U,
                        DistanceFromLeft = 0U,
                        DistanceFromRight = 0U
                    });

                // Add the image to the document body
                var paragraph = body.AppendChild(new Paragraph());
                var run = paragraph.AppendChild(new Run());
                run.AppendChild(element);
                
                AddParagraph(body, ""); // Add spacing
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding chart image to document");
                AddParagraph(body, $"[Fout bij toevoegen grafiek: {ex.Message}]");
            }
        }

        private string FormatCurrency(decimal amount)
        {
            return $"€ {amount:N2}";
        }
    }
}
