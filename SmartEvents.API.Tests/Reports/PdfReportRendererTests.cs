using System.Text;
using SmartEvents.API.Infrastructure.Reports;

namespace SmartEvents.API.Tests.Reports;

public class PdfReportRendererTests
{
    private readonly PdfReportRenderer _sut = new();

    [Fact]
    public void Describes_itself_as_pdf()
    {
        Assert.Equal("pdf", _sut.Format);
        Assert.Equal("application/pdf", _sut.ContentType);
        Assert.Equal("pdf", _sut.FileExtension);
    }

    [Fact]
    public void Renders_attendee_report_as_pdf_bytes()
    {
        var bytes = _sut.Render(ReportFixtures.Attendees());
        AssertIsPdf(bytes);
    }

    [Fact]
    public void Renders_sales_report_as_pdf_bytes()
    {
        var bytes = _sut.Render(ReportFixtures.Sales());
        AssertIsPdf(bytes);
    }

    [Fact]
    public void Renders_venue_bookings_report_as_pdf_bytes()
    {
        var bytes = _sut.Render(ReportFixtures.VenueBookings());
        AssertIsPdf(bytes);
    }

    [Fact]
    public void Renders_report_with_no_rows()
    {
        var bytes = _sut.Render(ReportFixtures.Attendees(rows: 0));
        AssertIsPdf(bytes);
    }

    [Fact]
    public void Renders_many_rows_across_pages()
    {
        // 120 rows will not fit on one landscape A4 page; QuestPDF must paginate, not throw.
        var bytes = _sut.Render(ReportFixtures.Attendees(rows: 120));
        AssertIsPdf(bytes);
        Assert.True(bytes.Length > 10_000);
    }

    private static void AssertIsPdf(byte[] bytes)
    {
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
