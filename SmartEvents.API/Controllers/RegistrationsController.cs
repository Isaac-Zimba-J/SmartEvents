using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Application.Interfaces;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegistrationsController(SmartEventsDbContext db, INotificationDispatcher notificationDispatcher) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RegistrationResponse>> Register(CreateRegistrationRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ev = await db.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId);

        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.Status != EventStatus.Published) return BadRequest(new { message = "Event is not open for registration." });
        if (ev.EndDate < DateTime.UtcNow) return BadRequest(new { message = "Event has already ended." });

        if (await db.Registrations.AnyAsync(r => r.EventId == request.EventId && r.UserId == userId))
            return Conflict(new { message = "You are already registered for this event." });

        var confirmedCount = ev.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        var isWaitlisted = confirmedCount >= ev.MaxAttendees;

        if (isWaitlisted && !ev.WaitlistEnabled)
            return BadRequest(new { message = "Event is full and waitlist is disabled." });

        int waitlistPosition = 0;
        if (isWaitlisted)
            waitlistPosition = ev.Registrations.Count(r => r.Status == RegistrationStatus.Waitlisted) + 1;

        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = request.EventId,
            UserId = userId,
            Status = isWaitlisted ? RegistrationStatus.Waitlisted : RegistrationStatus.Confirmed,
            WaitlistPosition = waitlistPosition,
            Notes = request.Notes
        };

        db.Registrations.Add(registration);

        // Generate ticket immediately if confirmed and free event
        if (registration.Status == RegistrationStatus.Confirmed && !ev.IsTicketed)
        {
            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                TicketNumber = GenerateTicketNumber(),
                QrCode = Guid.NewGuid().ToString("N"),
                RegistrationId = registration.Id,
                EventId = ev.Id
            };
            db.Tickets.Add(ticket);
        }

        await db.SaveChangesAsync();

        try
        {
            var fullReg = await db.Registrations
                .Include(r => r.User)
                .Include(r => r.Event)
                .Include(r => r.Ticket)
                .FirstAsync(r => r.Id == registration.Id);

            if (fullReg.Status == RegistrationStatus.Confirmed)
                await notificationDispatcher.SendRegistrationConfirmedAsync(fullReg);
            else if (fullReg.Status == RegistrationStatus.Waitlisted)
                await notificationDispatcher.SendRegistrationWaitlistedAsync(fullReg);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Notification dispatch warning: {ex.Message}");
        }

        return CreatedAtAction(nameof(GetById), new { id = registration.Id },
            await BuildResponse(registration.Id));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RegistrationResponse>> GetById(Guid id)
    {
        var response = await BuildResponse(id);
        if (response is null) return NotFound();
        return Ok(response);
    }

    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<RegistrationResponse>>> GetMyRegistrations()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ids = await db.Registrations
            .Where(r => r.UserId == userId)
            .Select(r => r.Id)
            .ToListAsync();

        var responses = new List<RegistrationResponse>();
        foreach (var id in ids)
        {
            var r = await BuildResponse(id);
            if (r is not null) responses.Add(r);
        }

        return Ok(responses);
    }

    [HttpGet("event/{eventId:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)},{nameof(UserRole.Organizer)}")]
    public async Task<ActionResult<IEnumerable<RegistrationResponse>>> GetByEvent(Guid eventId)
    {
        var ids = await db.Registrations
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.RegisteredAt)
            .Select(r => r.Id)
            .ToListAsync();

        var responses = new List<RegistrationResponse>();
        foreach (var id in ids)
        {
            var r = await BuildResponse(id);
            if (r is not null) responses.Add(r);
        }

        return Ok(responses);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var registration = await db.Registrations
            .Include(r => r.Ticket)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration is null) return NotFound();
        if (registration.UserId != userId) return Forbid();
        if (registration.Status == RegistrationStatus.CheckedIn)
            return BadRequest(new { message = "Cannot cancel a registration that has already checked in." });

        registration.Status = RegistrationStatus.Cancelled;
        registration.UpdatedAt = DateTime.UtcNow;

        // Promote next person on waitlist
        var nextWaitlisted = await db.Registrations
            .Where(r => r.EventId == registration.EventId && r.Status == RegistrationStatus.Waitlisted)
            .OrderBy(r => r.WaitlistPosition)
            .FirstOrDefaultAsync();

        if (nextWaitlisted is not null)
        {
            nextWaitlisted.Status = RegistrationStatus.Confirmed;
            nextWaitlisted.WaitlistPosition = 0;
            nextWaitlisted.UpdatedAt = DateTime.UtcNow;

            var ev = await db.Events.FindAsync(registration.EventId);
            if (ev is not null && !ev.IsTicketed)
            {
                db.Tickets.Add(new Ticket
                {
                    Id = Guid.NewGuid(),
                    TicketNumber = GenerateTicketNumber(),
                    QrCode = Guid.NewGuid().ToString("N"),
                    RegistrationId = nextWaitlisted.Id,
                    EventId = ev.Id
                });
            }
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("check-in")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)},{nameof(UserRole.Organizer)}")]
    public async Task<IActionResult> CheckIn(CheckInRequest request)
    {
        var ticket = await db.Tickets
            .Include(t => t.Registration)
            .FirstOrDefaultAsync(t => t.TicketNumber == request.TicketNumber);

        if (ticket is null) return NotFound(new { message = "Ticket not found." });
        if (ticket.IsUsed) return BadRequest(new { message = "Ticket has already been used." });
        if (ticket.Registration.Status != RegistrationStatus.Confirmed)
            return BadRequest(new { message = "Registration is not confirmed." });

        ticket.IsUsed = true;
        ticket.UsedAt = DateTime.UtcNow;
        ticket.Registration.Status = RegistrationStatus.CheckedIn;
        ticket.Registration.CheckedInAt = DateTime.UtcNow;
        ticket.Registration.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(new { message = "Check-in successful." });
    }

    private async Task<RegistrationResponse?> BuildResponse(Guid id)
    {
        var r = await db.Registrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .Include(r => r.Ticket)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r is null) return null;

        return new RegistrationResponse(
            r.Id, r.Status, r.WaitlistPosition, r.Notes,
            r.RegisteredAt, r.CheckedInAt,
            r.EventId, r.Event.Title,
            r.UserId, $"{r.User.FirstName} {r.User.LastName}",
            r.Ticket is null ? null : new TicketResponse(
                r.Ticket.Id, r.Ticket.TicketNumber,
                r.Ticket.QrCode, r.Ticket.IsUsed, r.Ticket.IssuedAt
            )
        );
    }

    private static string GenerateTicketNumber()
        => $"SE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
}
