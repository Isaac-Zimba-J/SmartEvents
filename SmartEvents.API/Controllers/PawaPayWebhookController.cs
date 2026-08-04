using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Application.Interfaces;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/pawapay")]
[AllowAnonymous]
public class PawaPayWebhookController(SmartEventsDbContext db, INotificationDispatcher notificationDispatcher) : ControllerBase
{
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] PawaPayWebhookPayload payload)
    {
        var payment = await db.Payments
            .Include(p => p.Registration)
            .FirstOrDefaultAsync(p => p.PawaPayDepositId == payload.DepositId);

        if (payment is null) return Ok();

        if (payment.Status is PaymentStatus.Completed or PaymentStatus.Failed)
            return Ok();

        if (payload.Status == "COMPLETED")
        {
            payment.Status = PaymentStatus.Completed;
            payment.TransactionReference = payload.DepositId;
            payment.GatewayResponse = payload.Status;
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
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Webhook notification warning: {ex.Message}");
            }
        }
        else if (payload.Status == "FAILED")
        {
            payment.Status = PaymentStatus.Failed;
            payment.GatewayResponse = payload.Status;
            payment.Registration.Status = RegistrationStatus.Cancelled;
            await db.SaveChangesAsync();
        }

        return Ok();
    }
}
