using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;

namespace SmartEvents.API.Controllers;

public record NotificationResponse(
    Guid Id,
    NotificationType Type,
    NotificationEvent Event,
    string Subject,
    string Body,
    bool IsSent,
    DateTime CreatedAt,
    DateTime? SentAt
);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(SmartEventsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetMy()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var notifications = await db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationResponse(
                n.Id, n.Type, n.Event, n.Subject, n.Body,
                n.IsSent, n.CreatedAt, n.SentAt))
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("count")]
    public async Task<ActionResult<object>> GetCount()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var count = await db.Notifications.CountAsync(n => n.UserId == userId);
        return Ok(new { count });
    }
}
