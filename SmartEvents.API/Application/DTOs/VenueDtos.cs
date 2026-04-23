using SmartEvents.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartEvents.API.Application.DTOs;

public record CreateVenueRequest(
    [Required, MaxLength(200)] string Name,
    string? Description,
    [Required] string Address,
    [Required] string City,
    [Required] string Country,
    double? Latitude,
    double? Longitude,
    [Range(1, 1000000)] int Capacity,
    VenueType Type,
    string? ImageUrl,
    string? Amenities,
    decimal? PricePerDay
);

public record UpdateVenueRequest(
    [Required, MaxLength(200)] string Name,
    string? Description,
    [Required] string Address,
    [Required] string City,
    [Required] string Country,
    double? Latitude,
    double? Longitude,
    [Range(1, 1000000)] int Capacity,
    VenueType Type,
    string? ImageUrl,
    string? Amenities,
    decimal? PricePerDay,
    bool IsAvailable
);

public record VenueResponse(
    Guid Id,
    string Name,
    string? Description,
    string Address,
    string City,
    string Country,
    double? Latitude,
    double? Longitude,
    int Capacity,
    VenueType Type,
    string? ImageUrl,
    string? Amenities,
    decimal? PricePerDay,
    bool IsAvailable,
    Guid CompanyId,
    string CompanyName,
    DateTime CreatedAt
);
