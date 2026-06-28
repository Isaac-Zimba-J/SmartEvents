using SmartEvents.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartEvents.API.Application.DTOs;

public record CreateEventRequest(
    [Required, MaxLength(300)] string Title,
    string? Description,
    string? ImageUrl,
    EventCategory Category,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    string? Timezone,
    [Range(1, 1000000)] int MaxAttendees,
    bool IsTicketed,
    [Range(0, double.MaxValue)] decimal TicketPrice,
    bool WaitlistEnabled,
    bool IsPublic,
    string? Tags,
    Guid? VenueId,
    string? VenueText,
    Guid? CompanyId
);

public record UpdateEventRequest(
    [Required, MaxLength(300)] string Title,
    string? Description,
    string? ImageUrl,
    EventCategory Category,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    string? Timezone,
    [Range(1, 1000000)] int MaxAttendees,
    bool IsTicketed,
    [Range(0, double.MaxValue)] decimal TicketPrice,
    bool WaitlistEnabled,
    bool IsPublic,
    string? Tags,
    Guid? VenueId,
    string? VenueText,
    EventStatus Status
);

public record EventSummaryResponse(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? ImageUrl,
    EventCategory Category,
    EventStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    int MaxAttendees,
    int RegisteredCount,
    bool IsTicketed,
    decimal TicketPrice,
    bool WaitlistEnabled,
    bool IsPublic,
    string? Tags,
    Guid CompanyId,
    string CompanyName,
    VenueResponse? Venue,
    string? VenueText,
    string OrganizerName,
    DateTime CreatedAt
);

public record EventDetailResponse(
    Guid Id,
    string Title,
    string Slug,
    string? Description,
    string? ImageUrl,
    EventCategory Category,
    EventStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    string? Timezone,
    int MaxAttendees,
    int RegisteredCount,
    int WaitlistedCount,
    bool IsTicketed,
    decimal TicketPrice,
    bool WaitlistEnabled,
    bool IsPublic,
    string? Tags,
    Guid CompanyId,
    string CompanyName,
    VenueResponse? Venue,
    string? VenueText,
    string OrganizerName,
    DateTime CreatedAt
);
