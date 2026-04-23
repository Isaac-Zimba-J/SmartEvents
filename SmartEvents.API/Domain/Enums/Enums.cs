namespace SmartEvents.API.Domain.Enums;

public enum UserRole
{
    SuperAdmin,
    CompanyAdmin,
    Organizer,
    Attendee
}

public enum EventStatus
{
    Draft,
    Published,
    Cancelled,
    Completed
}

public enum EventCategory
{
    Conference,
    Workshop,
    Concert,
    Exhibition,
    Sports,
    Networking,
    Webinar,
    Other
}

public enum RegistrationStatus
{
    Pending,
    Confirmed,
    Waitlisted,
    Cancelled,
    CheckedIn
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public enum PaymentMethod
{
    Stripe,
    AirtelMoney,
    MTNMoMo,
    Free
}

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

public enum VenueType
{
    Indoor,
    Outdoor,
    Virtual,
    Hybrid
}
