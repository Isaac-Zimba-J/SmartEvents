using System.ComponentModel.DataAnnotations;

namespace SmartEvents.API.Application.DTOs;

public record CreateCompanyRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(500)] string? Description,
    string? Website,
    string? Phone,
    [EmailAddress] string? Email,
    string? Address,
    string? LogoUrl
);

public record UpdateCompanyRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(500)] string? Description,
    string? Website,
    string? Phone,
    [EmailAddress] string? Email,
    string? Address,
    string? LogoUrl
);

public record CompanyResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    string? Website,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive,
    DateTime CreatedAt
);
