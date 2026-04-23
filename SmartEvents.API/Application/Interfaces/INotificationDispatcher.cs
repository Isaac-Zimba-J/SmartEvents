using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;

namespace SmartEvents.API.Application.Interfaces;

public interface INotificationDispatcher
{
    Task SendRegistrationConfirmedAsync(Registration registration);
    Task SendRegistrationWaitlistedAsync(Registration registration);
    Task SendWaitlistPromotedAsync(Registration registration);
    Task SendEventReminderAsync(Event ev, IEnumerable<Registration> registrations);
    Task SendEventCancelledAsync(Event ev, IEnumerable<Registration> registrations);
    Task SendEventUpdatedAsync(Event ev, IEnumerable<Registration> registrations);
    Task SendCheckInConfirmedAsync(Registration registration);
}
