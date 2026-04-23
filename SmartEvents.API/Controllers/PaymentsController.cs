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
public class PaymentsController(SmartEventsDbContext db, IQrCodeService qrCodeService) : ControllerBase
{
    /// <summary>
    /// Mock checkout — always succeeds. Creates registration + ticket + payment in one step.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<PaymentCheckoutResponse>> Checkout(PaymentCheckoutRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(userId);
        if (user is null) return Unauthorized();

        var ev = await db.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId);

        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.Status != EventStatus.Published)
            return BadRequest(new { message = "Event is not open for registration." });
        if (ev.EndDate < DateTime.UtcNow)
            return BadRequest(new { message = "Event has already ended." });
        if (!ev.IsTicketed)
            return BadRequest(new { message = "This event is free. Use /api/registrations instead." });

        if (await db.Registrations.AnyAsync(r => r.EventId == request.EventId && r.UserId == userId))
            return Conflict(new { message = "You are already registered for this event." });

        var confirmedCount = ev.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
        if (confirmedCount >= ev.MaxAttendees && !ev.WaitlistEnabled)
            return BadRequest(new { message = "Event is full and waitlist is disabled." });

        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = request.EventId,
            UserId = userId,
            Status = RegistrationStatus.Confirmed,
            WaitlistPosition = 0,
            Notes = request.Notes
        };
        db.Registrations.Add(registration);

        var ticketNumber = GenerateTicketNumber();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketNumber = ticketNumber,
            QrCode = qrCodeService.GenerateBase64($"SE:{ticketNumber}:{registration.Id}"),
            RegistrationId = registration.Id,
            EventId = ev.Id
        };
        db.Tickets.Add(ticket);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = ev.TicketPrice,
            Currency = "ZMW",
            Status = PaymentStatus.Completed,
            Method = request.PaymentMethod,
            TransactionReference = $"MOCK-{Guid.NewGuid().ToString("N")[..12].ToUpper()}",
            GatewayResponse = "mock_payment_succeeded",
            PaidAt = DateTime.UtcNow,
            RegistrationId = registration.Id
        };
        db.Payments.Add(payment);

        await db.SaveChangesAsync();

        return Ok(new PaymentCheckoutResponse(
            payment.Id,
            payment.TransactionReference!,
            payment.Amount,
            payment.Status,
            payment.Method,
            new RegistrationResponse(
                registration.Id,
                registration.Status,
                registration.WaitlistPosition,
                registration.Notes,
                registration.RegisteredAt,
                registration.CheckedInAt,
                ev.Id,
                ev.Title,
                userId,
                $"{user.FirstName} {user.LastName}",
                new TicketResponse(ticket.Id, ticket.TicketNumber, ticket.QrCode, ticket.IsUsed, ticket.IssuedAt)
            )
        ));
    }

    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<PaymentSummaryResponse>>> GetMyPayments()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var payments = await db.Payments
            .Include(p => p.Registration).ThenInclude(r => r.Event)
            .Where(p => p.Registration.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentSummaryResponse(
                p.Id,
                p.Amount,
                p.Currency,
                p.Status,
                p.Method,
                p.TransactionReference,
                p.CreatedAt,
                p.PaidAt,
                p.Registration.Event.Title
            ))
            .ToListAsync();

        return Ok(payments);
    }

    private static string GenerateTicketNumber()
        => $"SE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
}
