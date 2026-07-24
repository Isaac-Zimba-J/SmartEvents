using System.ComponentModel.DataAnnotations;
using SmartEvents.API.Domain.Enums;

namespace SmartEvents.API.Application.DTOs;

public record CreateVenueBookingRequest(
    [Required] Guid VenueId,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    string? Notes,
    [Required] PaymentMethod PaymentMethod,
    string? PhoneNumber
);

public record VenueBookingResponse(
    Guid Id,
    Guid VenueId,
    string VenueName,
    string VenueAddress,
    string VenueCity,
    DateTime StartDate,
    DateTime EndDate,
    string? Notes,
    VenueBookingStatus Status,
    decimal TotalAmount,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    string? TransactionRef,
    DateTime CreatedAt
);

public record VenueBookingStatusResponse(
    Guid BookingId,
    VenueBookingStatus Status,
    PaymentStatus PaymentStatus,
    string? TransactionRef
);
