namespace SmartEvents.API.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public bool IsUsed { get; set; } = false;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }

    public Guid RegistrationId { get; set; }
    public Registration Registration { get; set; } = null!;

    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
}
