namespace SmartEvents.API.Infrastructure.Reports;

// Shared by every report. Renderers depend only on the records in this file.
public record ReportHeader(
    string CompanyName,
    string Title,
    string Subject,
    DateTime GeneratedAt,
    string GeneratedBy
);

public record SummaryItem(string Label, string Value);

public record AttendeeRow(
    int Number,
    string Name,
    string Email,
    string Phone,
    string TicketNumber,
    string Status,
    DateTime RegisteredAt,
    DateTime? CheckedInAt
);

public record AttendeeReport(
    ReportHeader Header,
    IReadOnlyList<SummaryItem> Summary,
    IReadOnlyList<AttendeeRow> Rows
);

public record SalesRow(
    DateTime Date,
    string Attendee,
    string TicketNumber,
    string Method,
    decimal Amount,
    string Status,
    string Reference
);

public record SalesReport(
    ReportHeader Header,
    IReadOnlyList<SummaryItem> Summary,
    IReadOnlyList<SalesRow> Rows
);

public record VenueBookingRow(
    string BookedBy,
    string Email,
    DateTime Start,
    DateTime End,
    int Days,
    string Status,
    string PaymentStatus,
    string Method,
    decimal Amount,
    string Reference
);

public record VenueBookingsReport(
    ReportHeader Header,
    IReadOnlyList<SummaryItem> Summary,
    IReadOnlyList<VenueBookingRow> Rows
);
