using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.Helpers;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;
using SmartEvents.API.Infrastructure.Reports;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(SmartEventsDbContext db, IEnumerable<IReportRenderer> renderers) : ControllerBase
{
    [HttpGet("events/{eventId:guid}/attendees")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)},{nameof(UserRole.Organizer)}")]
    public async Task<IActionResult> EventAttendees(Guid eventId, [FromQuery] string format = "pdf")
    {
        var renderer = ResolveRenderer(format);
        if (renderer is null) return BadRequest(new { message = "Unsupported format. Use pdf or xlsx." });

        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized();

        var ev = await db.Events
            .Include(e => e.Company)
            .Include(e => e.Venue)
            .Include(e => e.Registrations).ThenInclude(r => r.User)
            .Include(e => e.Registrations).ThenInclude(r => r.Ticket)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev is null) return NotFound(new { message = "Event not found." });
        if (!CanAccessEvent(ev, user)) return Forbid();

        var rows = ev.Registrations
            .Where(r => r.Status != RegistrationStatus.Cancelled)
            .OrderBy(r => r.User.LastName).ThenBy(r => r.User.FirstName)
            .Select((r, i) => new AttendeeRow(
                i + 1,
                $"{r.User.FirstName} {r.User.LastName}",
                r.User.Email,
                r.User.Phone ?? "—",
                r.Ticket?.TicketNumber ?? "—",
                r.Status.ToString(),
                r.RegisteredAt,
                r.CheckedInAt))
            .ToList();

        var confirmed = ev.Registrations.Count(r => r.Status is RegistrationStatus.Confirmed or RegistrationStatus.CheckedIn);
        var waitlisted = ev.Registrations.Count(r => r.Status == RegistrationStatus.Waitlisted);
        var checkedIn = ev.Registrations.Count(r => r.Status == RegistrationStatus.CheckedIn || r.CheckedInAt != null);

        var report = new AttendeeReport(
            BuildHeader(ev.Company.Name, "Attendee List", EventSubject(ev), user),
            [
                new("Confirmed", confirmed.ToString()),
                new("Waitlisted", waitlisted.ToString()),
                new("Checked in", checkedIn.ToString()),
                new("Capacity", ev.MaxAttendees.ToString())
            ],
            rows);

        return File(renderer.Render(report), renderer.ContentType, FileName("attendees", ev.Slug, renderer));
    }

    [HttpGet("events/{eventId:guid}/sales")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)},{nameof(UserRole.Organizer)}")]
    public async Task<IActionResult> EventSales(Guid eventId, [FromQuery] string format = "pdf")
    {
        var renderer = ResolveRenderer(format);
        if (renderer is null) return BadRequest(new { message = "Unsupported format. Use pdf or xlsx." });

        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized();

        var ev = await db.Events
            .Include(e => e.Company)
            .Include(e => e.Venue)
            .Include(e => e.Registrations).ThenInclude(r => r.User)
            .Include(e => e.Registrations).ThenInclude(r => r.Ticket)
            .Include(e => e.Registrations).ThenInclude(r => r.Payment)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev is null) return NotFound(new { message = "Event not found." });
        if (!CanAccessEvent(ev, user)) return Forbid();

        var payments = ev.Registrations
            .Where(r => r.Payment is not null)
            .Select(r => (Registration: r, Payment: r.Payment!))
            .OrderByDescending(x => x.Payment.CreatedAt)
            .ToList();

        var rows = payments.Select(x => new SalesRow(
                x.Payment.CreatedAt,
                $"{x.Registration.User.FirstName} {x.Registration.User.LastName}",
                x.Registration.Ticket?.TicketNumber ?? "—",
                MethodLabel(x.Payment.Method),
                x.Payment.Amount,
                x.Payment.Status.ToString(),
                x.Payment.TransactionReference ?? "—"))
            .ToList();

        var completed = payments.Where(x => x.Payment.Status == PaymentStatus.Completed).Select(x => x.Payment).ToList();
        var airtel = completed.Where(p => p.Method == PaymentMethod.AirtelMoney).ToList();
        var mtn = completed.Where(p => p.Method == PaymentMethod.MTNMoMo).ToList();

        var report = new SalesReport(
            BuildHeader(ev.Company.Name, "Sales Report", EventSubject(ev), user),
            [
                new("Tickets sold", completed.Count.ToString()),
                new("Gross revenue", Money(completed.Sum(p => p.Amount))),
                new("Airtel Money", $"{airtel.Count} · {Money(airtel.Sum(p => p.Amount))}"),
                new("MTN MoMo", $"{mtn.Count} · {Money(mtn.Sum(p => p.Amount))}"),
                new("Pending", payments.Count(x => x.Payment.Status == PaymentStatus.Pending).ToString()),
                new("Failed", payments.Count(x => x.Payment.Status == PaymentStatus.Failed).ToString())
            ],
            rows);

        return File(renderer.Render(report), renderer.ContentType, FileName("sales", ev.Slug, renderer));
    }

    [HttpGet("venues/{venueId:guid}/bookings")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<IActionResult> VenueBookings(Guid venueId, [FromQuery] string format = "pdf")
    {
        var renderer = ResolveRenderer(format);
        if (renderer is null) return BadRequest(new { message = "Unsupported format. Use pdf or xlsx." });

        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized();

        var venue = await db.Venues
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == venueId);

        if (venue is null) return NotFound(new { message = "Venue not found." });
        if (!CanAccessVenue(venue, user)) return Forbid();

        var bookings = await db.VenueBookings
            .Include(b => b.User)
            .Where(b => b.VenueId == venueId)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync();

        var rows = bookings.Select(b => new VenueBookingRow(
                $"{b.User.FirstName} {b.User.LastName}",
                b.User.Email,
                b.StartDate,
                b.EndDate,
                DaysBooked(b),
                b.Status.ToString(),
                b.PaymentStatus.ToString(),
                MethodLabel(b.PaymentMethod),
                b.TotalAmount,
                b.TransactionRef ?? "—"))
            .ToList();

        var confirmedBookings = bookings.Where(b => b.Status == VenueBookingStatus.Confirmed).ToList();
        var price = venue.PricePerDay is { } p ? $"K {p:N0}/day" : "price on request";
        var subject = $"{venue.Name} — {venue.Address}, {venue.City} · Capacity {venue.Capacity:N0} · {price}";

        var report = new VenueBookingsReport(
            BuildHeader(venue.Company.Name, "Venue Bookings", subject, user),
            [
                new("Total bookings", bookings.Count.ToString()),
                new("Confirmed", confirmedBookings.Count.ToString()),
                new("Days booked", confirmedBookings.Sum(DaysBooked).ToString()),
                new("Revenue", Money(bookings.Where(b => b.PaymentStatus == PaymentStatus.Completed).Sum(b => b.TotalAmount)))
            ],
            rows);

        return File(renderer.Render(report), renderer.ContentType, FileName("venue-bookings", SlugHelper.Generate(venue.Name), renderer));
    }

    // ---

    private IReportRenderer? ResolveRenderer(string format) =>
        renderers.FirstOrDefault(r => r.Format.Equals(format, StringComparison.OrdinalIgnoreCase));

    private async Task<User?> CurrentUserAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await db.Users.FindAsync(userId);
    }

    private static bool CanAccessEvent(Event ev, User user) => user.Role switch
    {
        UserRole.SuperAdmin => true,
        UserRole.CompanyAdmin => user.CompanyId == ev.CompanyId,
        UserRole.Organizer => ev.OrganizerId == user.Id,
        _ => false
    };

    private static bool CanAccessVenue(Venue venue, User user) => user.Role switch
    {
        UserRole.SuperAdmin => true,
        UserRole.CompanyAdmin => user.CompanyId == venue.CompanyId,
        _ => false
    };

    private static ReportHeader BuildHeader(string companyName, string title, string subject, User user) =>
        new(companyName, title, subject, DateTime.UtcNow, $"{user.FirstName} {user.LastName}");

    private static string EventSubject(Event ev)
    {
        var venue = ev.Venue?.Name ?? ev.VenueText ?? "Venue TBA";
        return $"{ev.Title} · {ev.StartDate:yyyy-MM-dd} · {venue}";
    }

    private static string FileName(string report, string slug, IReportRenderer renderer) =>
        $"{report}-{slug}-{DateTime.UtcNow:yyyyMMdd}.{renderer.FileExtension}";

    private static string Money(decimal amount) => $"K {amount:N2}";

    private static string MethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.AirtelMoney => "Airtel Money",
        PaymentMethod.MTNMoMo => "MTN MoMo",
        PaymentMethod.Free => "Free",
        _ => method.ToString()
    };

    // Same rule VenueBookingsController uses to price a booking.
    private static int DaysBooked(VenueBooking b) => Math.Max(1, (b.EndDate.Date - b.StartDate.Date).Days);
}
