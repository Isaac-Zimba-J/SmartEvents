namespace SmartEvents.API.Infrastructure.Reports;

public interface IReportRenderer
{
    /// <summary>Value accepted by the ?format= query parameter, e.g. "pdf".</summary>
    string Format { get; }
    string ContentType { get; }
    string FileExtension { get; }

    byte[] Render(AttendeeReport report);
    byte[] Render(SalesReport report);
    byte[] Render(VenueBookingsReport report);
}
