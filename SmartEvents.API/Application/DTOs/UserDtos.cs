using System.ComponentModel.DataAnnotations;

namespace SmartEvents.API.Application.DTOs;

public record UpdateProfileRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Phone] string? Phone,
    string? AvatarUrl
);

public record ProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? AvatarUrl,
    string Role,
    bool IsEmailVerified,
    Guid? CompanyId,
    string? CompanyName,
    DateTime CreatedAt
);
