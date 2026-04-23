using SmartEvents.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartEvents.API.Application.DTOs;

public record RegisterRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    string? Phone
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    UserDto User
);

public record RefreshRequest(
    [Required] string RefreshToken
);

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    UserRole Role,
    Guid? CompanyId
);
