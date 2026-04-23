using SmartEvents.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartEvents.API.Application.DTOs;

public record CreateRegistrationRequest(
    [Required] Guid EventId,
    string? Notes
);

public record RegistrationResponse(
    Guid Id,
    RegistrationStatus Status,
    int WaitlistPosition,
    string? Notes,
    DateTime RegisteredAt,
    DateTime? CheckedInAt,
    Guid EventId,
    string EventTitle,
    Guid UserId,
    string UserName,
    TicketResponse? Ticket
);

public record TicketResponse(
    Guid Id,
    string TicketNumber,
    string QrCode,
    bool IsUsed,
    DateTime IssuedAt
);

public record CheckInRequest(
    [Required] string TicketNumber
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
