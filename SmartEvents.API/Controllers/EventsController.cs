using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Application.Helpers;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(SmartEventsDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<EventSummaryResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] EventCategory? category = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? companyId = null)
    {
        var query = db.Events
            .Include(e => e.Company)
            .Include(e => e.Venue)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .Where(e => e.Status == EventStatus.Published && e.IsPublic);

        if (category.HasValue)
            query = query.Where(e => e.Category == category.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Title.Contains(search) || (e.Description != null && e.Description.Contains(search)));

        if (companyId.HasValue)
            query = query.Where(e => e.CompanyId == companyId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<EventSummaryResponse>(
            items.Select(ToSummary),
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        ));
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<EventDetailResponse>> GetBySlug(string slug)
    {
        var ev = await db.Events
            .Include(e => e.Company)
            .Include(e => e.Venue).ThenInclude(v => v!.Company)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Slug == slug);

        if (ev is null) return NotFound();
        return Ok(ToDetail(ev));
    }

    [HttpGet("company/{companyId:guid}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<EventSummaryResponse>>> GetByCompany(Guid companyId)
    {
        var events = await db.Events
            .Include(e => e.Company)
            .Include(e => e.Venue)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .Where(e => e.CompanyId == companyId)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();

        return Ok(events.Select(ToSummary));
    }

    [HttpGet("recommended")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<EventSummaryResponse>>> GetRecommended(
        [FromQuery] Guid? excludeEventId = null)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var categories = await db.Registrations
            .Where(r => r.UserId == userId &&
                        (r.Status == RegistrationStatus.Confirmed ||
                         r.Status == RegistrationStatus.Waitlisted))
            .Select(r => r.Event.Category)
            .Distinct()
            .ToListAsync();

        if (categories.Count == 0)
            return Ok(Array.Empty<EventSummaryResponse>());

        var registeredEventIds = db.Registrations
            .Where(r => r.UserId == userId && r.Status != RegistrationStatus.Cancelled)
            .Select(r => r.EventId);

        var events = await db.Events
            .Include(e => e.Company)
            .Include(e => e.Venue)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .Where(e => e.Status == EventStatus.Published
                     && e.IsPublic
                     && e.StartDate >= DateTime.UtcNow
                     && categories.Contains(e.Category)
                     && !registeredEventIds.Contains(e.Id)
                     && (excludeEventId == null || e.Id != excludeEventId.Value))
            .OrderBy(e => e.StartDate)
            .Take(4)
            .ToListAsync();

        return Ok(events.Select(ToSummary));
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)},{nameof(UserRole.Organizer)}")]
    public async Task<ActionResult<EventSummaryResponse>> Create(CreateEventRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(userId);
        if (user is null) return Unauthorized();

        var companyId = user.CompanyId;
        if (companyId is null) return BadRequest(new { message = "User must belong to a company to create events." });

        if (request.StartDate >= request.EndDate)
            return BadRequest(new { message = "End date must be after start date." });

        if (request.VenueId.HasValue)
        {
            var venue = await db.Venues.FindAsync(request.VenueId.Value);
            if (venue is null) return BadRequest(new { message = "Venue not found." });
        }

        var slug = SlugHelper.Generate(request.Title);
        if (await db.Events.AnyAsync(e => e.Slug == slug))
            slug = SlugHelper.GenerateUnique(request.Title, Guid.NewGuid().ToString("N")[..6]);

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = slug,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            Category = request.Category,
            Status = EventStatus.Draft,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Timezone = request.Timezone,
            MaxAttendees = request.MaxAttendees,
            IsTicketed = request.IsTicketed,
            TicketPrice = request.IsTicketed ? request.TicketPrice : 0,
            WaitlistEnabled = request.WaitlistEnabled,
            IsPublic = request.IsPublic,
            Tags = request.Tags,
            CompanyId = companyId.Value,
            VenueId = request.VenueId,
            OrganizerId = userId
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        await db.Entry(ev).Reference(e => e.Company).LoadAsync();
        await db.Entry(ev).Reference(e => e.Organizer).LoadAsync();
        if (ev.VenueId.HasValue)
            await db.Entry(ev).Reference(e => e.Venue).LoadAsync();

        return CreatedAtAction(nameof(GetBySlug), new { slug = ev.Slug }, ToSummary(ev));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)},{nameof(UserRole.Organizer)}")]
    public async Task<ActionResult<EventSummaryResponse>> Update(Guid id, UpdateEventRequest request)
    {
        var ev = await db.Events
            .Include(e => e.Company)
            .Include(e => e.Venue)
            .Include(e => e.Organizer)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev is null) return NotFound();

        if (request.StartDate >= request.EndDate)
            return BadRequest(new { message = "End date must be after start date." });

        ev.Title = request.Title;
        ev.Description = request.Description;
        ev.ImageUrl = request.ImageUrl;
        ev.Category = request.Category;
        ev.Status = request.Status;
        ev.StartDate = request.StartDate;
        ev.EndDate = request.EndDate;
        ev.Timezone = request.Timezone;
        ev.MaxAttendees = request.MaxAttendees;
        ev.IsTicketed = request.IsTicketed;
        ev.TicketPrice = request.IsTicketed ? request.TicketPrice : 0;
        ev.WaitlistEnabled = request.WaitlistEnabled;
        ev.IsPublic = request.IsPublic;
        ev.Tags = request.Tags;
        ev.VenueId = request.VenueId;
        ev.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToSummary(ev));
    }

    [HttpPatch("{id:guid}/publish")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        ev.Status = EventStatus.Published;
        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        ev.Status = EventStatus.Cancelled;
        ev.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        db.Events.Remove(ev);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static VenueResponse? ToVenueResponse(Venue? v) => v is null ? null : new(
        v.Id, v.Name, v.Description, v.Address, v.City, v.Country,
        v.Latitude, v.Longitude, v.Capacity, v.Type, v.ImageUrl,
        v.Amenities, v.PricePerDay, v.IsAvailable, v.CompanyId,
        v.Company?.Name ?? string.Empty, v.CreatedAt
    );

    private static EventSummaryResponse ToSummary(Event e) => new(
        e.Id, e.Title, e.Slug, e.Description, e.ImageUrl, e.Category, e.Status,
        e.StartDate, e.EndDate, e.MaxAttendees,
        e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed),
        e.IsTicketed, e.TicketPrice, e.WaitlistEnabled, e.IsPublic, e.Tags,
        e.CompanyId, e.Company?.Name ?? string.Empty,
        ToVenueResponse(e.Venue),
        $"{e.Organizer?.FirstName} {e.Organizer?.LastName}".Trim(),
        e.CreatedAt
    );

    private static EventDetailResponse ToDetail(Event e) => new(
        e.Id, e.Title, e.Slug, e.Description, e.ImageUrl, e.Category, e.Status,
        e.StartDate, e.EndDate, e.Timezone, e.MaxAttendees,
        e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed),
        e.Registrations.Count(r => r.Status == RegistrationStatus.Waitlisted),
        e.IsTicketed, e.TicketPrice, e.WaitlistEnabled, e.IsPublic, e.Tags,
        e.CompanyId, e.Company?.Name ?? string.Empty,
        ToVenueResponse(e.Venue),
        $"{e.Organizer?.FirstName} {e.Organizer?.LastName}".Trim(),
        e.CreatedAt
    );
}
