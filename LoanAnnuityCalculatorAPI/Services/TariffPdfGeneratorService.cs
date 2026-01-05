using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LoanAnnuityCalculatorAPI.Models.DTOs;

namespace LoanAnnuityCalculatorAPI.Services
{
    public class TariffPdfGeneratorService
    {
        public byte[] GenerateTariffPdf(TariffPdfRequest request)
        {
            // Set QuestPDF license to Community (free for non-commercial use)
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text("Tariefberekening")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .Column(column =>
                        {
                            column.Spacing(15);

                            // Loan Details Section
                            column.Item().Element(c => ComposeMainDetails(c, request));

                            // Payment Schedule Section
                            column.Item().Element(c => ComposePaymentSchedule(c, request));
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Gegenereerd op: ");
                            x.Span(DateTime.Now.ToString("dd-MM-yyyy HH:mm")).SemiBold();
                        });
                });

                // BSE Breakdown on separate page
                if (request.BseBreakdown != null)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header()
                            .Text("BSE Berekening (Bruto Steun Equivalent)")
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(15);
                                column.Item().Element(c => ComposeBseBreakdown(c, request));
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Pagina 2 - BSE Details");
                            });
                    });
                }
            });

            return document.GeneratePdf();
        }

        private void ComposeMainDetails(IContainer container, TariffPdfRequest request)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                // Title
                column.Item().Text("Tarief Details").Bold().FontSize(14);

                // Details table
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    // Header styling
                    IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

                    // Loan characteristics
                    table.Cell().Element(CellStyle).Text("Leningbedrag").Bold();
                    table.Cell().Element(CellStyle).AlignRight().Text($"€ {request.LoanAmount:N2}");

                    table.Cell().Element(CellStyle).Text("Looptijd").Bold();
                    table.Cell().Element(CellStyle).AlignRight().Text($"{request.LoanTermYears} jaar ({request.LoanTermMonths} maanden)");

                    table.Cell().Element(CellStyle).Text("Aflossingsschema").Bold();
                    table.Cell().Element(CellStyle).AlignRight().Text(GetRedemptionSchemeText(request.RedemptionScheme));

                    if (request.InterestOnlyMonths > 0)
                    {
                        table.Cell().Element(CellStyle).Text("Aflossingsvrije periode").Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text($"{request.InterestOnlyMonths / 12} jaar");
                    }

                    // Spacing
                    table.Cell().ColumnSpan(2).PaddingTop(10);

                    // Show only final tariff (hide breakdown for proprietary reasons)
                    table.Cell().Element(CellStyle).Background(Colors.Blue.Lighten4).Text("Tarief").Bold();
                    table.Cell().Element(CellStyle).Background(Colors.Blue.Lighten4).AlignRight().Text($"{request.InterestRate:F2}%").Bold();

                    // Spacing
                    table.Cell().ColumnSpan(2).PaddingTop(10);

                    // Payment details
                    table.Cell().Element(CellStyle).Text("Maandelijkse Betaling").Bold();
                    table.Cell().Element(CellStyle).AlignRight().Text($"€ {request.MonthlyPayment:N2}");

                    table.Cell().Element(CellStyle).Text("Totaal Rente").Bold();
                    table.Cell().Element(CellStyle).AlignRight().Text($"€ {request.TotalInterest:N2}");

                    table.Cell().Element(CellStyle).Background(Colors.Green.Lighten4).Text("Totaal te Betalen").Bold();
                    table.Cell().Element(CellStyle).Background(Colors.Green.Lighten4).AlignRight().Text($"€ {request.TotalPayment:N2}").Bold();

                    if (request.BSE > 0)
                    {
                        table.Cell().Element(CellStyle).Background(Colors.Yellow.Lighten3).Text("BSE (Bruto Steun Equivalent)").Bold();
                        table.Cell().Element(CellStyle).Background(Colors.Yellow.Lighten3).AlignRight().Text($"€ {request.BSE:N2}").Bold();
                    }
                });
            });
        }

        private void ComposePaymentSchedule(IContainer container, TariffPdfRequest request)
        {
            if (request.YearlySchedule == null || !request.YearlySchedule.Any())
                return;

            container.Column(column =>
            {
                column.Spacing(10);
                column.Item().Text("Betalingsschema (per jaar)").Bold().FontSize(14);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    // Header
                    IContainer HeaderStyle(IContainer c) => c.Background(Colors.Blue.Medium).Padding(5);
                    IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("Jaar").FontColor(Colors.White).Bold();
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Rente").FontColor(Colors.White).Bold();
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Aflossing").FontColor(Colors.White).Bold();
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Restschuld").FontColor(Colors.White).Bold();
                    });

                    // Rows
                    foreach (var item in request.YearlySchedule)
                    {
                        table.Cell().Element(CellStyle).Text(item.Year.ToString());
                        table.Cell().Element(CellStyle).AlignRight().Text($"€ {item.InterestComponent:N2}");
                        table.Cell().Element(CellStyle).AlignRight().Text($"€ {item.CapitalComponent:N2}");
                        table.Cell().Element(CellStyle).AlignRight().Text($"€ {item.RemainingLoan:N2}");
                    }
                });
            });
        }

        private void ComposeBseBreakdown(IContainer container, TariffPdfRequest request)
        {
            if (request.BseBreakdown == null)
                return;

            container.Column(column =>
            {
                column.Spacing(15);

                // Market Rate Breakdown
                if (request.BseBreakdown.MarketRateBreakdown != null)
                {
                    column.Item().Text("Markttarief Opbouw").Bold().FontSize(14);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

                        var mrb = request.BseBreakdown.MarketRateBreakdown;

                        table.Cell().Element(CellStyle).Text("Basisrente (ECB 1-jaar EURIBOR)").Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text($"{mrb.BaseRate:F4}%");

                        table.Cell().Element(CellStyle).Text($"Risicopremie (Rating: {mrb.CreditRating}, Zekerheid: {mrb.SecurityLevel}, LTV: {mrb.LTV:F2}%)").Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text($"{mrb.RiskPremium:F4}%");

                        if (mrb.IsNewCompany && mrb.NewCompanyMinimumApplied)
                        {
                            table.Cell().Element(CellStyle).Text("Nieuwe onderneming (minimum 400bps toegepast)").Italic();
                            table.Cell().Element(CellStyle).AlignRight().Text("✓");
                        }

                        table.Cell().Element(CellStyle).Background(Colors.Blue.Lighten4).Text("Totaal Markttarief").Bold();
                        table.Cell().Element(CellStyle).Background(Colors.Blue.Lighten4).AlignRight().Text($"{request.BseBreakdown.MarketRate:F2}%").Bold();
                    });
                }

                // Rate Comparison
                column.Item().PaddingTop(20).Text("Tarief Vergelijking").Bold().FontSize(14);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

                    table.Cell().Element(CellStyle).Text("Markttarief").Bold();
                    table.Cell().Element(CellStyle).AlignRight().Text($"{request.BseBreakdown.MarketRate:F2}%");

                    table.Cell().Element(CellStyle).Text("Leningtarief").Bold();
                    table.Cell().Element(CellStyle).AlignRight().Text($"{request.BseBreakdown.LoanRate:F2}%");

                    table.Cell().Element(CellStyle).Background(Colors.Yellow.Lighten3).Text("Verschil").Bold();
                    table.Cell().Element(CellStyle).Background(Colors.Yellow.Lighten3).AlignRight()
                        .Text($"{(request.BseBreakdown.MarketRate - request.BseBreakdown.LoanRate):F2}%").Bold();
                });

                // Yearly BSE Breakdown
                if (request.BseBreakdown.YearlyBreakdown != null && request.BseBreakdown.YearlyBreakdown.Any())
                {
                    column.Item().PaddingTop(20).Text("Jaarlijkse BSE Berekening").Bold().FontSize(14);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        IContainer HeaderStyle(IContainer c) => c.Background(Colors.Blue.Medium).Padding(5);
                        IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Jaar").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Marktrente").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Leningrente").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Verschil").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Contante Waarde").FontColor(Colors.White).Bold();
                        });

                        decimal totalBse = 0;
                        foreach (var item in request.BseBreakdown.YearlyBreakdown)
                        {
                            table.Cell().Element(CellStyle).Text(item.Year.ToString());
                            table.Cell().Element(CellStyle).AlignRight().Text($"€ {item.MarketInterest:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"€ {item.LoanInterest:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"€ {item.Difference:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"€ {item.DiscountedValue:N2}");
                            totalBse += item.DiscountedValue;
                        }

                        // Total row
                        table.Cell().ColumnSpan(4).Element(c => c.Background(Colors.Green.Lighten3).Padding(5)).Text("Totaal BSE (Contante Waarde)").Bold();
                        table.Cell().Element(c => c.Background(Colors.Green.Lighten3).Padding(5)).AlignRight().Text($"€ {totalBse:N2}").Bold();
                    });
                }

                // Explanation
                column.Item().PaddingTop(15).Background(Colors.Blue.Lighten5).Padding(10).Text(text =>
                {
                    text.Span("Toelichting: ").Bold();
                    text.Span("De BSE wordt berekend volgens de EU referentietarief methodologie. Het markttarief is gebaseerd op de risicovrije rente plus een risicopremie afhankelijk van de kredietwaardigheid en onderpandkwaliteit. Het verschil tussen marktrente en leningrente wordt contant gemaakt met de risicovrije rente als disconteringsvoet.");
                });
            });
        }

        private string GetRedemptionSchemeText(string scheme)
        {
            return scheme?.ToLower() switch
            {
                "annuity" => "Annuïtair",
                "linear" => "Lineair",
                "bullet" => "Bullet (aflossingsvrij)",
                "buildingdepot" => "Bouwdepot",
                _ => scheme ?? "Onbekend"
            };
        }
    }

    public class TariffPdfRequest
    {
        public decimal LoanAmount { get; set; }
        public int LoanTermMonths { get; set; }
        public decimal LoanTermYears { get; set; }
        public decimal InterestRate { get; set; }
        public decimal BaseRate { get; set; }
        public decimal ImpactDiscount { get; set; }
        public string? ImpactLevel { get; set; }
        public decimal LtvSpread { get; set; }
        public decimal RatingSpread { get; set; }
        public decimal MonthlyPayment { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal LTV { get; set; }
        public string CreditRating { get; set; } = string.Empty;
        public string RedemptionScheme { get; set; } = string.Empty;
        public int InterestOnlyMonths { get; set; }
        public decimal BSE { get; set; }
        public List<YearlyScheduleItem>? YearlySchedule { get; set; }
        public BseBreakdownDto? BseBreakdown { get; set; }
    }

    public class YearlyScheduleItem
    {
        public int Year { get; set; }
        public decimal InterestComponent { get; set; }
        public decimal CapitalComponent { get; set; }
        public decimal RemainingLoan { get; set; }
    }

    public class BseBreakdownDto
    {
        public decimal MarketRate { get; set; }
        public decimal LoanRate { get; set; }
        public MarketRateBreakdownDto? MarketRateBreakdown { get; set; }
        public List<YearlyBseItem>? YearlyBreakdown { get; set; }
    }

    public class MarketRateBreakdownDto
    {
        public decimal BaseRate { get; set; }
        public decimal RiskPremium { get; set; }
        public string CreditRating { get; set; } = string.Empty;
        public decimal LTV { get; set; }
        public string SecurityLevel { get; set; } = string.Empty;
        public bool IsNewCompany { get; set; }
        public bool NewCompanyMinimumApplied { get; set; }
    }

    public class YearlyBseItem
    {
        public int Year { get; set; }
        public decimal MarketInterest { get; set; }
        public decimal LoanInterest { get; set; }
        public decimal Difference { get; set; }
        public decimal DiscountedValue { get; set; }
    }
}
