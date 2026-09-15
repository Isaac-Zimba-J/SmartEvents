using ClosedXML.Excel;

namespace SmartEvents.API.Infrastructure.Reports;

public class ExcelReportRenderer : IReportRenderer
{
    public string Format => "xlsx";
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public string FileExtension => "xlsx";

    private const string MoneyFormat = "\"K \"#,##0.00";
    private const string DateTimeFormat = "yyyy-mm-dd hh:mm";
    private const string DateFormat = "yyyy-mm-dd";

    // Wrapper so the row builder can tell "date only" from "date + time".
    private readonly record struct DateOnlyCell(DateTime Value);

    public byte[] Render(AttendeeReport report) => Build(
        report.Header,
        report.Summary,
        sheetName: "Attendees",
        columns: ["#", "Name", "Email", "Phone", "Ticket #", "Status", "Registered (UTC)", "Checked in (UTC)"],
        rows: report.Rows.Select(r => new object?[]
        {
            r.Number, r.Name, r.Email, r.Phone, r.TicketNumber, r.Status, r.RegisteredAt, r.CheckedInAt
        }));

    public byte[] Render(SalesReport report) => Build(
        report.Header,
        report.Summary,
        sheetName: "Sales",
        columns: ["Date (UTC)", "Attendee", "Ticket #", "Method", "Amount", "Status", "Reference"],
        rows: report.Rows.Select(r => new object?[]
        {
            r.Date, r.Attendee, r.TicketNumber, r.Method, r.Amount, r.Status, r.Reference
        }));

    public byte[] Render(VenueBookingsReport report) => Build(
        report.Header,
        report.Summary,
        sheetName: "Bookings",
        columns: ["Booked by", "Email", "Start", "End", "Days", "Status", "Payment", "Method", "Amount", "Ref"],
        rows: report.Rows.Select(r => new object?[]
        {
            r.BookedBy, r.Email, new DateOnlyCell(r.Start), new DateOnlyCell(r.End), r.Days,
            r.Status, r.PaymentStatus, r.Method, r.Amount, r.Reference
        }));

    private static byte[] Build(
        ReportHeader header,
        IReadOnlyList<SummaryItem> summary,
        string sheetName,
        string[] columns,
        IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();

        var summarySheet = workbook.AddWorksheet("Summary");
        summarySheet.Cell(1, 1).Value = header.Title;
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 14;
        summarySheet.Cell(2, 1).Value = header.CompanyName;
        summarySheet.Cell(3, 1).Value = header.Subject;
        summarySheet.Cell(4, 1).Value = $"Generated {header.GeneratedAt:yyyy-MM-dd HH:mm} UTC by {header.GeneratedBy}";

        var summaryRow = 6;
        foreach (var item in summary)
        {
            summarySheet.Cell(summaryRow, 1).Value = item.Label;
            summarySheet.Cell(summaryRow, 1).Style.Font.Bold = true;
            summarySheet.Cell(summaryRow, 2).Value = item.Value;
            summaryRow++;
        }
        summarySheet.Columns().AdjustToContents();

        var dataSheet = workbook.AddWorksheet(sheetName);
        for (var i = 0; i < columns.Length; i++)
            dataSheet.Cell(1, i + 1).Value = columns[i];
        dataSheet.Row(1).Style.Font.Bold = true;
        dataSheet.SheetView.FreezeRows(1);

        var rowNumber = 2;
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length; i++)
                WriteCell(dataSheet.Cell(rowNumber, i + 1), row[i]);
            rowNumber++;
        }
        dataSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteCell(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = "—";
                break;
            case decimal amount:
                cell.Value = amount;
                cell.Style.NumberFormat.Format = MoneyFormat;
                break;
            case DateOnlyCell d:
                cell.Value = d.Value;
                cell.Style.NumberFormat.Format = DateFormat;
                break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.NumberFormat.Format = DateTimeFormat;
                break;
            case int n:
                cell.Value = n;
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }
}
