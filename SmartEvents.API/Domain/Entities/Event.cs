using SmartEvents.API.Domain.Enums;

namespace SmartEvents.API.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public EventCategory Category { get; set; } = EventCategory.Other;
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Timezone { get; set; }
    public int MaxAttendees { get; set; }
    public bool IsTicketed { get; set; } = false;
    public decimal TicketPrice { get; set; } = 0;
    public bool WaitlistEnabled { get; set; } = true;
    public bool IsPublic { get; set; } = true;
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }

    public Guid OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public ICollection<Registration> Registrations { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}
