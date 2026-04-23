using SmartEvents.API.Domain.Enums;

namespace SmartEvents.API.Domain.Entities;

public class Venue
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int Capacity { get; set; }
    public VenueType Type { get; set; } = VenueType.Indoor;
    public string? ImageUrl { get; set; }
    public string? Amenities { get; set; }
    public decimal? PricePerDay { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public ICollection<Event> Events { get; set; } = [];
}
