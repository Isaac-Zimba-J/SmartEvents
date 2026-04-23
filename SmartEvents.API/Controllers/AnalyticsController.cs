using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController(SmartEventsDbContext db) : ControllerBase
{
    [HttpGet("overview")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<ActionResult<OverviewAnalytics>> GetOverview()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isSuperAdmin = User.IsInRole("SuperAdmin");

        var eventsQuery = isSuperAdmin
            ? db.Events.AsQueryable()
            : db.Events.Where(e => e.Company.Users.Any(u => u.Id == userId));

        var registrationsQuery = isSuperAdmin
            ? db.Registrations.AsQueryable()
            : db.Registrations.Where(r => r.Event.Company.Users.Any(u => u.Id == userId));

        var paymentsQuery = isSuperAdmin
            ? db.Payments.AsQueryable()
            : db.Payments.Where(p => p.Registration.Event.Company.Users.Any(u => u.Id == userId));

        var totalEvents = await eventsQuery.CountAsync();
        var publishedEvents = await eventsQuery.CountAsync(e => e.Status == EventStatus.Published);
        var totalRegistrations = await registrationsQuery.CountAsync();
        var totalCheckedIn = await registrationsQuery.CountAsync(r => r.Status == RegistrationStatus.CheckedIn);
        var totalRevenue = await paymentsQuery
            .Where(p => p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var totalUsers = isSuperAdmin ? await db.Users.CountAsync() : 0;
        var totalVenues = isSuperAdmin ? await db.Venues.CountAsync() : 0;
        var totalCompanies = isSuperAdmin ? await db.Companies.CountAsync() : 0;

        return Ok(new OverviewAnalytics(
            totalEvents, publishedEvents, totalRegistrations,
            totalCheckedIn, totalRevenue, totalUsers, totalVenues, totalCompanies
        ));
    }

    [HttpGet("events")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin,Organizer")]
    public async Task<ActionResult<IEnumerable<EventAnalytics>>> GetEventStats()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        var isCompanyAdmin = User.IsInRole("CompanyAdmin");

        var eventsQuery = db.Events
            .Include(e => e.Registrations)
                .ThenInclude(r => r.Payment)
            .AsQueryable();

        if (!isSuperAdmin && !isCompanyAdmin)
            eventsQuery = eventsQuery.Where(e => e.OrganizerId == userId);
        else if (!isSuperAdmin)
            eventsQuery = eventsQuery.Where(e => e.Company.Users.Any(u => u.Id == userId));

        var events = await eventsQuery.ToListAsync();

        var stats = events.Select(e =>
        {
            var confirmed = e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
            var waitlisted = e.Registrations.Count(r => r.Status == RegistrationStatus.Waitlisted);
            var checkedIn = e.Registrations.Count(r => r.Status == RegistrationStatus.CheckedIn);
            var cancelled = e.Registrations.Count(r => r.Status == RegistrationStatus.Cancelled);
            var revenue = e.Registrations
                .Where(r => r.Payment?.Status == PaymentStatus.Completed)
                .Sum(r => r.Payment?.Amount ?? 0);
            var fillRate = e.MaxAttendees > 0
                ? Math.Round((double)(confirmed + checkedIn) / e.MaxAttendees * 100, 1)
                : 0;

            return new EventAnalytics(e.Id, e.Title, e.Status.ToString(),
                e.MaxAttendees, confirmed, waitlisted, checkedIn, cancelled, revenue, fillRate);
        });

        return Ok(stats);
    }
}
