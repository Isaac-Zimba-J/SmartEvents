namespace SmartEvents.Shared.Enums;

public enum NotificationType
{
    Email,
    SMS,
    Push
}

public enum NotificationEvent
{
    RegistrationConfirmed,
    RegistrationWaitlisted,
    PaymentReceived,
    EventReminder,
    EventCancelled,
    EventUpdated,
    CheckInConfirmed
}
