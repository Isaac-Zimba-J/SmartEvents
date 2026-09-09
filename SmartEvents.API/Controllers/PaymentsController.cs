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
public class PaymentsController(
    SmartEventsDbContext db,
    IQrCodeService qrCodeService,
    IPawaPayService pawaPayService,
    INotificationDispatcher notificationDispatcher) : ControllerBase
{
    private static readonly Dictionary<PaymentMethod, string> CorrespondentMap = new()
    {
        [PaymentMethod.AirtelMoney] = "AIRTEL_OAPI_ZMB",
        [PaymentMethod.MTNMoMo] = "MTN_MOMO_ZMB"
    };

    [HttpPost("checkout")]
    public async Task<ActionResult<PaymentCheckoutResponse>> Checkout(PaymentCheckoutRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(userId);
        if (user is null) return Unauthorized();

        if (request.PaymentMethod == PaymentMethod.Free)
            return BadRequest(new { message = "Use /api/registrations for free events." });

        if (!CorrespondentMap.TryGetValue(request.PaymentMethod, out var correspondent))
            return BadRequest(new { message = "Unsupported payment method." });

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

        // Optimistic: create registration + ticket as Pending before calling PawaPay
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = request.EventId,
            UserId = userId,
            Status = RegistrationStatus.Pending,
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

        var depositId = Guid.NewGuid().ToString();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = ev.TicketPrice,
            Currency = "ZMW",
            Status = PaymentStatus.Pending,
            Method = request.PaymentMethod,
            PawaPayDepositId = depositId,
            RegistrationId = registration.Id
        };
        db.Payments.Add(payment);

        await db.SaveChangesAsync();

        // Initiate PawaPay deposit — if this fails, clean up and return an error immediately
        try
        {
            await pawaPayService.InitiateDepositAsync(
                depositId,
                ev.TicketPrice,
                "ZMW",
                correspondent,
                request.PhoneNumber
            );
        }
        catch (Exception ex)
        {
            payment.Status = PaymentStatus.Failed;
            payment.GatewayResponse = ex.Message;
            registration.Status = RegistrationStatus.Cancelled;
            await db.SaveChangesAsync();
            Console.Error.WriteLine($"PawaPay initiation failed: {ex.Message}");
            return UnprocessableEntity(new { message = "Payment initiation failed. Please verify your phone number is in format 260XXXXXXXXX and try again." });
        }

        return Ok(new PaymentCheckoutResponse(
            payment.Id,
            depositId,
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

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<PaymentStatusResponse>> GetStatus(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var payment = await db.Payments
            .Include(p => p.Registration)
            .FirstOrDefaultAsync(p => p.Id == id && p.Registration.UserId == userId);

        if (payment is null) return NotFound(new { message = "Payment not found." });

        // Return cached status if already terminal
        if (payment.Status is PaymentStatus.Completed or PaymentStatus.Failed)
            return Ok(new PaymentStatusResponse(payment.Id, payment.Status, payment.TransactionReference, payment.PaidAt));

        // Poll PawaPay for current status
        if (payment.PawaPayDepositId is not null)
        {
            try
            {
                var remote = await pawaPayService.GetDepositStatusAsync(payment.PawaPayDepositId);

                if (remote.Status == "COMPLETED")
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.TransactionReference = payment.PawaPayDepositId;
                    payment.GatewayResponse = remote.Status;
                    payment.PaidAt = DateTime.UtcNow;
                    payment.Registration.Status = RegistrationStatus.Confirmed;
                    await db.SaveChangesAsync();

                    try
                    {
                        var fullReg = await db.Registrations
                            .Include(r => r.User)
                            .Include(r => r.Event)
                            .Include(r => r.Ticket)
                            .FirstAsync(r => r.Id == payment.Registration.Id);
                        await notificationDispatcher.SendRegistrationConfirmedAsync(fullReg);
                    }
                    catch (Exception notifEx)
                    {
                        Console.Error.WriteLine($"Notification dispatch warning: {notifEx.Message}");
                    }
                }
                else if (remote.Status == "FAILED")
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.GatewayResponse = remote.Status;
                    payment.Registration.Status = RegistrationStatus.Cancelled;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"PawaPay status poll warning: {ex.Message}");
            }
        }

        return Ok(new PaymentStatusResponse(payment.Id, payment.Status, payment.TransactionReference, payment.PaidAt));
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
