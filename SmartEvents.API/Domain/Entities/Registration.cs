using SmartEvents.API.Domain.Enums;

namespace SmartEvents.API.Domain.Entities;

public class Registration
{
    public Guid Id { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;
    public int WaitlistPosition { get; set; } = 0;
    public string? Notes { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? CheckedInAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Ticket? Ticket { get; set; }
    public Payment? Payment { get; set; }
}
