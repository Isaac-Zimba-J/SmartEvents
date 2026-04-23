using Microsoft.AspNetCore.SignalR;
using SmartEvents.API.Application.Interfaces;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;
using SmartEvents.API.Infrastructure.Hubs;

namespace SmartEvents.API.Infrastructure.Notifications;

public class NotificationDispatcher(
    IEmailService emailService,
    ISmsService smsService,
    IHubContext<NotificationHub> hubContext,
    SmartEventsDbContext db,
    ILogger<NotificationDispatcher> logger
) : INotificationDispatcher
{
    public async Task SendRegistrationConfirmedAsync(Registration registration)
    {
        var user = registration.User;
        var ev = registration.Event;
        var ticket = registration.Ticket;

        var subject = $"Registration Confirmed – {ev.Title}";
        var html = EmailTemplates.RegistrationConfirmed(user.FirstName, ev, ticket);
        var sms = $"Hi {user.FirstName}, your registration for '{ev.Title}' is confirmed! Ticket: {ticket?.TicketNumber ?? "N/A"}";

        await DispatchAsync(user, subject, html, sms, NotificationType.Email, NotificationEvent.RegistrationConfirmed);
        await DispatchAsync(user, subject, sms, sms, NotificationType.SMS, NotificationEvent.RegistrationConfirmed);
        await PushToUserAsync(user.Id.ToString(), "registration_confirmed", new
        {
            message = $"You're registered for {ev.Title}!",
            eventId = ev.Id,
            eventTitle = ev.Title,
            ticketNumber = ticket?.TicketNumber
        });
    }

    public async Task SendRegistrationWaitlistedAsync(Registration registration)
    {
        var user = registration.User;
        var ev = registration.Event;

        var subject = $"You're on the Waitlist – {ev.Title}";
        var html = EmailTemplates.Waitlisted(user.FirstName, ev, registration.WaitlistPosition);
        var sms = $"Hi {user.FirstName}, you're on the waitlist (#{registration.WaitlistPosition}) for '{ev.Title}'. We'll notify you if a spot opens.";

        await DispatchAsync(user, subject, html, sms, NotificationType.Email, NotificationEvent.RegistrationWaitlisted);
        await DispatchAsync(user, subject, sms, sms, NotificationType.SMS, NotificationEvent.RegistrationWaitlisted);
        await PushToUserAsync(user.Id.ToString(), "waitlisted", new
        {
            message = $"You're #{registration.WaitlistPosition} on the waitlist for {ev.Title}",
            eventId = ev.Id,
            eventTitle = ev.Title,
            position = registration.WaitlistPosition
        });
    }

    public async Task SendWaitlistPromotedAsync(Registration registration)
    {
        var user = registration.User;
        var ev = registration.Event;
        var ticket = registration.Ticket;

        var subject = $"Great news! You've got a spot – {ev.Title}";
        var html = EmailTemplates.RegistrationConfirmed(user.FirstName, ev, ticket, promoted: true);
        var sms = $"Hi {user.FirstName}, a spot opened up for '{ev.Title}'! Your registration is confirmed. Ticket: {ticket?.TicketNumber ?? "N/A"}";

        await DispatchAsync(user, subject, html, sms, NotificationType.Email, NotificationEvent.RegistrationConfirmed);
        await DispatchAsync(user, subject, sms, sms, NotificationType.SMS, NotificationEvent.RegistrationConfirmed);
        await PushToUserAsync(user.Id.ToString(), "promoted_from_waitlist", new
        {
            message = $"You've been confirmed for {ev.Title}!",
            eventId = ev.Id,
            eventTitle = ev.Title,
            ticketNumber = ticket?.TicketNumber
        });
    }

    public async Task SendEventReminderAsync(Event ev, IEnumerable<Registration> registrations)
    {
        foreach (var reg in registrations.Where(r => r.Status == RegistrationStatus.Confirmed))
        {
            var user = reg.User;
            var subject = $"Reminder: {ev.Title} is coming up!";
            var html = EmailTemplates.EventReminder(user.FirstName, ev);
            var sms = $"Reminder: '{ev.Title}' starts on {ev.StartDate:MMM d, yyyy}. See you there!";

            await DispatchAsync(user, subject, html, sms, NotificationType.Email, NotificationEvent.EventReminder);
            await PushToUserAsync(user.Id.ToString(), "event_reminder", new
            {
                message = $"Reminder: {ev.Title} is coming up on {ev.StartDate:MMM d}!",
                eventId = ev.Id,
                eventTitle = ev.Title
            });
        }
    }

    public async Task SendEventCancelledAsync(Event ev, IEnumerable<Registration> registrations)
    {
        foreach (var reg in registrations.Where(r =>
            r.Status is RegistrationStatus.Confirmed or RegistrationStatus.Waitlisted))
        {
            var user = reg.User;
            var subject = $"Event Cancelled – {ev.Title}";
            var html = EmailTemplates.EventCancelled(user.FirstName, ev);
            var sms = $"Sorry, '{ev.Title}' has been cancelled. We apologize for the inconvenience.";

            await DispatchAsync(user, subject, html, sms, NotificationType.Email, NotificationEvent.EventCancelled);
            await DispatchAsync(user, subject, sms, sms, NotificationType.SMS, NotificationEvent.EventCancelled);
            await PushToUserAsync(user.Id.ToString(), "event_cancelled", new
            {
                message = $"{ev.Title} has been cancelled.",
                eventId = ev.Id,
                eventTitle = ev.Title
            });
        }
    }

    public async Task SendEventUpdatedAsync(Event ev, IEnumerable<Registration> registrations)
    {
        foreach (var reg in registrations.Where(r => r.Status == RegistrationStatus.Confirmed))
        {
            var user = reg.User;
            var subject = $"Event Updated – {ev.Title}";
            var html = EmailTemplates.EventUpdated(user.FirstName, ev);
            var sms = $"'{ev.Title}' has been updated. Check SmartEvents for the latest details.";

            await DispatchAsync(user, subject, html, sms, NotificationType.Email, NotificationEvent.EventUpdated);
            await PushToUserAsync(user.Id.ToString(), "event_updated", new
            {
                message = $"{ev.Title} has been updated.",
                eventId = ev.Id,
                eventTitle = ev.Title
            });
        }
    }

    public async Task SendCheckInConfirmedAsync(Registration registration)
    {
        var user = registration.User;
        var ev = registration.Event;

        var subject = $"Check-In Confirmed – {ev.Title}";
        var html = EmailTemplates.CheckInConfirmed(user.FirstName, ev);

        await DispatchAsync(user, subject, html, string.Empty, NotificationType.Email, NotificationEvent.CheckInConfirmed);
        await PushToUserAsync(user.Id.ToString(), "checked_in", new
        {
            message = $"Welcome to {ev.Title}! Enjoy the event.",
            eventId = ev.Id,
            eventTitle = ev.Title
        });
    }

    // ---

    private async Task DispatchAsync(User user, string subject, string htmlBody, string smsBody,
        NotificationType type, NotificationEvent notifEvent)
    {
        var recipient = type == NotificationType.SMS ? (user.Phone ?? string.Empty) : user.Email;
        if (string.IsNullOrWhiteSpace(recipient)) return;

        try
        {
            if (type == NotificationType.Email)
                await emailService.SendAsync(recipient, subject, htmlBody);
            else if (type == NotificationType.SMS && !string.IsNullOrWhiteSpace(user.Phone))
                await smsService.SendAsync(recipient, smsBody);

            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Type = type,
                Event = notifEvent,
                Recipient = recipient,
                Subject = subject,
                Body = type == NotificationType.SMS ? smsBody : htmlBody,
                IsSent = true,
                SentAt = DateTime.UtcNow,
                UserId = user.Id
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch {Type} notification to {Recipient}", type, recipient);
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                Type = type,
                Event = notifEvent,
                Recipient = recipient,
                Subject = subject,
                Body = type == NotificationType.SMS ? smsBody : htmlBody,
                IsSent = false,
                ErrorMessage = ex.Message,
                UserId = user.Id
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task PushToUserAsync(string userId, string eventType, object payload)
    {
        try
        {
            await hubContext.Clients.Group($"user-{userId}").SendAsync("notification", new
            {
                type = eventType,
                data = payload,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SignalR push failed for user {UserId}", userId);
        }
    }
}
