using SmartEvents.API.Infrastructure.Reports;

namespace SmartEvents.API.Tests.Reports;

public static class ReportFixtures
{
    private static readonly DateTime T = new(2026, 9, 15, 10, 30, 0, DateTimeKind.Utc);

    public static ReportHeader Header(string title) =>
        new("TechEvents Co.", title, "Dev Summit Malawi 2026 · 2026-10-01 · Lilongwe ICC", T, "Olivia Organizer");

    public static AttendeeReport Attendees(int rows = 2) => new(
        Header("Attendee List"),
        [new("Confirmed", "2"), new("Waitlisted", "0"), new("Checked in", "1"), new("Capacity", "100")],
        Enumerable.Range(1, rows).Select(i => new AttendeeRow(
            i, $"Person {i}", $"person{i}@example.com", "260971234567",
            $"SE-20260915-{i:D8}", i == 1 ? "CheckedIn" : "Confirmed",
            T.AddDays(-i), i == 1 ? T : null)).ToList());

    public static SalesReport Sales(int rows = 2) => new(
        Header("Sales Report"),
        [new("Tickets sold", "2"), new("Gross revenue", "K 100.00"), new("Airtel Money", "1 · K 50.00"),
         new("MTN MoMo", "1 · K 50.00"), new("Pending", "0"), new("Failed", "0")],
        Enumerable.Range(1, rows).Select(i => new SalesRow(
            T.AddHours(-i), $"Person {i}", $"SE-20260915-{i:D8}",
            i % 2 == 0 ? "MTN MoMo" : "Airtel Money", 50m, "Completed", $"ref-{i}")).ToList());

    public static VenueBookingsReport VenueBookings(int rows = 2) => new(
        new ReportHeader("TechEvents Co.", "Venue Bookings", "Lilongwe ICC — Area 3, Lilongwe · Capacity 500 · K 2,000/day", T, "Alice Admin"),
        [new("Total bookings", "2"), new("Confirmed", "2"), new("Days booked", "3"), new("Revenue", "K 6,000.00")],
        Enumerable.Range(1, rows).Select(i => new VenueBookingRow(
            $"Person {i}", $"person{i}@example.com", T.AddDays(i), T.AddDays(i + 1), i,
            "Confirmed", "Completed", "Airtel Money", 2000m * i, $"VB-{i:D6}")).ToList());
}
