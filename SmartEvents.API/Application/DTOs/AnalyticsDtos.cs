namespace SmartEvents.API.Application.DTOs;

public record OverviewAnalytics(
    int TotalEvents,
    int PublishedEvents,
    int TotalRegistrations,
    int TotalCheckedIn,
    decimal TotalRevenue,
    int TotalUsers,
    int TotalVenues,
    int TotalCompanies
);

public record EventAnalytics(
    Guid EventId,
    string Title,
    string Status,
    int MaxAttendees,
    int Confirmed,
    int Waitlisted,
    int CheckedIn,
    int Cancelled,
    decimal Revenue,
    double FillRate
);
