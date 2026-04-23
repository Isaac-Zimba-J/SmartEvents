using SmartEvents.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartEvents.API.Application.DTOs;

public record PaymentCheckoutRequest(
    [Required] Guid EventId,
    PaymentMethod PaymentMethod,
    string? Notes
);

public record PaymentCheckoutResponse(
    Guid PaymentId,
    string TransactionReference,
    decimal Amount,
    PaymentStatus Status,
    PaymentMethod Method,
    RegistrationResponse Registration
);

public record PaymentSummaryResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    PaymentMethod Method,
    string? TransactionReference,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string EventTitle
);
