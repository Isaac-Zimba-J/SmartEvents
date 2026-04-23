using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Application.Interfaces;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Infrastructure.Data;
using SmartEvents.API.Infrastructure.Notifications;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(SmartEventsDbContext db, ITokenService tokenService, IEmailService emailService, IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            return Conflict(new { message = "Email already in use." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Account is disabled." });

        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("request-verification")]
    [Authorize]
    public async Task<IActionResult> RequestVerification()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(userId);
        if (user is null) return NotFound();
        if (user.IsEmailVerified) return BadRequest(new { message = "Email is already verified." });

        user.VerificationToken = Guid.NewGuid().ToString("N");
        user.VerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await db.SaveChangesAsync();

        var appUrl = configuration["AppUrl"] ?? "http://localhost:4200";
        var link = $"{appUrl}/auth/verify-email?token={user.VerificationToken}";
        var html = EmailTemplates.VerificationEmail(user.FirstName, link);

        await emailService.SendAsync(user.Email, "Verify your SmartEvents email", html);

        return Ok(new { message = "Verification email sent. Check your inbox." });
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest(new { message = "Invalid token." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
        if (user is null) return BadRequest(new { message = "Invalid or expired verification token." });
        if (user.VerificationTokenExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "Verification token has expired. Please request a new one." });

        user.IsEmailVerified = true;
        user.VerificationToken = null;
        user.VerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Email verified successfully!" });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);
        if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        return Ok(await BuildAuthResponseAsync(user));
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var refreshToken = tokenService.GenerateRefreshToken();
        var refreshDays = int.Parse(configuration["JwtSettings:RefreshExpiryInDays"] ?? "7");
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshDays);
        await db.SaveChangesAsync();

        return BuildAuthResponse(user, refreshToken);
    }

    private AuthResponse BuildAuthResponse(User user, string? refreshToken = null)
    {
        var accessToken = tokenService.GenerateAccessToken(user);
        var expiryMinutes = int.Parse(configuration["JwtSettings:ExpiryInMinutes"] ?? "60");

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken ?? user.RefreshToken ?? string.Empty,
            TokenType: "Bearer",
            ExpiresIn: expiryMinutes * 60,
            User: new UserDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Phone,
                user.Role,
                user.CompanyId
            )
        );
    }
}
