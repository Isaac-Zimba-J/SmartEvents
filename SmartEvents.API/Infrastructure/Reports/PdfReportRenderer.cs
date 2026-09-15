using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SmartEvents.API.Infrastructure.Reports;

public class PdfReportRenderer : IReportRenderer
{
    static PdfReportRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string Format => "pdf";
    public string ContentType => "application/pdf";
    public string FileExtension => "pdf";

    private const string DateTimeFmt = "yyyy-MM-dd HH:mm";
    private const string DateFmt = "yyyy-MM-dd";

    public byte[] Render(AttendeeReport report) => Build(
        report.Header,
        report.Summary,
        columns: ["#", "Name", "Email", "Phone", "Ticket #", "Status", "Registered (UTC)", "Checked in"],
        widths: [0.4f, 1.6f, 2.2f, 1.2f, 1.7f, 1f, 1.3f, 0.9f],
        rightAligned: [0],
        rows: report.Rows.Select(r => new[]
        {
            r.Number.ToString(), r.Name, r.Email, r.Phone, r.TicketNumber, r.Status,
            r.RegisteredAt.ToString(DateTimeFmt), r.CheckedInAt?.ToString("HH:mm") ?? "—"
        }));

    public byte[] Render(SalesReport report) => Build(
        report.Header,
        report.Summary,
        columns: ["Date (UTC)", "Attendee", "Ticket #", "Method", "Amount", "Status", "Reference"],
        widths: [1.3f, 1.8f, 1.7f, 1.2f, 1f, 1f, 2.4f],
        rightAligned: [4],
        rows: report.Rows.Select(r => new[]
        {
            r.Date.ToString(DateTimeFmt), r.Attendee, r.TicketNumber, r.Method,
            Money(r.Amount), r.Status, r.Reference
        }));

    public byte[] Render(VenueBookingsReport report) => Build(
        report.Header,
        report.Summary,
        columns: ["Booked by", "Email", "Start", "End", "Days", "Status", "Payment", "Method", "Amount", "Ref"],
        widths: [1.5f, 2f, 1f, 1f, 0.6f, 1f, 1f, 1.1f, 1f, 1.4f],
        rightAligned: [4, 8],
        rows: report.Rows.Select(r => new[]
        {
            r.BookedBy, r.Email, r.Start.ToString(DateFmt), r.End.ToString(DateFmt), r.Days.ToString(),
            r.Status, r.PaymentStatus, r.Method, Money(r.Amount), r.Reference
        }));

    private static string Money(decimal amount) => $"K {amount:N2}";

    private static byte[] Build(
        ReportHeader header,
        IReadOnlyList<SummaryItem> summary,
        string[] columns,
        float[] widths,
        int[] rightAligned,
        IEnumerable<string[]> rows)
    {
        var document = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text(header.CompanyName).FontSize(11).SemiBold().FontColor(Colors.Grey.Darken2);
                        left.Item().Text(header.Title).FontSize(18).Bold();
                        left.Item().Text(header.Subject).FontSize(10);
                    });
                    row.ConstantItem(220).AlignRight().Column(right =>
                    {
                        right.Item().AlignRight().Text($"Generated {header.GeneratedAt:yyyy-MM-dd HH:mm} UTC")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        right.Item().AlignRight().Text($"by {header.GeneratedBy}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });

                col.Item().PaddingTop(10).Row(row =>
                {
                    foreach (var item in summary)
                    {
                        row.RelativeItem().PaddingRight(6).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(tile =>
                        {
                            tile.Item().Text(item.Label).FontSize(8).FontColor(Colors.Grey.Darken1);
                            tile.Item().Text(item.Value).FontSize(12).SemiBold();
                        });
                    }
                });

                col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    foreach (var w in widths) cols.RelativeColumn(w);
                });

                table.Header(h =>
                {
                    for (var i = 0; i < columns.Length; i++)
                    {
                        var cell = h.Cell().Background(Colors.Grey.Lighten3).Padding(4);
                        if (rightAligned.Contains(i)) cell.AlignRight().Text(columns[i]).SemiBold();
                        else cell.Text(columns[i]).SemiBold();
                    }
                });

                var rowIndex = 0;
                foreach (var row in rows)
                {
                    var background = rowIndex++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                    for (var i = 0; i < row.Length; i++)
                    {
                        var cell = table.Cell()
                            .Background(background)
                            .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(4);
                        if (rightAligned.Contains(i)) cell.AlignRight().Text(row[i]);
                        else cell.Text(row[i]);
                    }
                }

                if (rowIndex == 0)
                {
                    table.Cell().ColumnSpan((uint)columns.Length).Padding(14).AlignCenter()
                        .Text("No data").FontColor(Colors.Grey.Darken1);
                }
            });

            page.Footer().Row(row =>
            {
                row.RelativeItem().Text("Generated by SmartEvents").FontSize(8).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        }));

        return document.GeneratePdf();
    }
}
