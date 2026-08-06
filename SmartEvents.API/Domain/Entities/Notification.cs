using SmartEvents.API.Domain.Enums;

namespace SmartEvents.API.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public NotificationEvent Event { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsSent { get; set; } = false;
    public bool IsRead { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
