using ClosedXML.Excel;
using SmartEvents.API.Infrastructure.Reports;

namespace SmartEvents.API.Tests.Reports;

public class ExcelReportRendererTests
{
    private readonly ExcelReportRenderer _sut = new();

    [Fact]
    public void Describes_itself_as_xlsx()
    {
        Assert.Equal("xlsx", _sut.Format);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", _sut.ContentType);
        Assert.Equal("xlsx", _sut.FileExtension);
    }

    [Fact]
    public void Output_starts_with_zip_signature()
    {
        var bytes = _sut.Render(ReportFixtures.Attendees());
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public void Attendee_workbook_has_summary_and_data_sheets()
    {
        using var wb = Open(_sut.Render(ReportFixtures.Attendees(rows: 3)));

        Assert.Equal(["Summary", "Attendees"], wb.Worksheets.Select(w => w.Name).ToArray());

        var summary = wb.Worksheet("Summary");
        Assert.Equal("Attendee List", summary.Cell(1, 1).GetString());
        Assert.Equal("Confirmed", summary.Cell(6, 1).GetString());
        Assert.Equal("2", summary.Cell(6, 2).GetString());

        var data = wb.Worksheet("Attendees");
        Assert.Equal("#", data.Cell(1, 1).GetString());
        Assert.Equal("Checked in (UTC)", data.Cell(1, 8).GetString());
        Assert.True(data.Row(1).Style.Font.Bold);
        Assert.Equal(1, data.SheetView.SplitRow);            // header row frozen
        Assert.Equal(4, data.LastRowUsed()!.RowNumber());     // header + 3 rows
        Assert.Equal("Person 1", data.Cell(2, 2).GetString());
        Assert.Equal("—", data.Cell(3, 8).GetString());       // null CheckedInAt renders as a dash
    }

    [Fact]
    public void Sales_workbook_formats_amounts_as_kwacha_and_dates()
    {
        using var wb = Open(_sut.Render(ReportFixtures.Sales(rows: 2)));

        var data = wb.Worksheet("Sales");
        Assert.Equal("Amount", data.Cell(1, 5).GetString());
        Assert.Equal(50m, data.Cell(2, 5).GetValue<decimal>());
        Assert.Equal("\"K \"#,##0.00", data.Cell(2, 5).Style.NumberFormat.Format);
        Assert.Equal("yyyy-mm-dd hh:mm", data.Cell(2, 1).Style.NumberFormat.Format);
        Assert.Equal(new DateTime(2026, 9, 15, 9, 30, 0), data.Cell(2, 1).GetDateTime());
    }

    [Fact]
    public void Venue_bookings_workbook_has_bookings_sheet_with_ten_columns()
    {
        using var wb = Open(_sut.Render(ReportFixtures.VenueBookings(rows: 1)));

        var data = wb.Worksheet("Bookings");
        Assert.Equal(10, data.LastColumnUsed()!.ColumnNumber());
        Assert.Equal("Ref", data.Cell(1, 10).GetString());
        Assert.Equal(2000m, data.Cell(2, 9).GetValue<decimal>());
    }

    [Fact]
    public void Renders_report_with_no_rows()
    {
        using var wb = Open(_sut.Render(ReportFixtures.Sales(rows: 0)));
        var data = wb.Worksheet("Sales");
        Assert.Equal(1, data.LastRowUsed()!.RowNumber());     // header only
    }

    private static XLWorkbook Open(byte[] bytes) => new(new MemoryStream(bytes));
}
