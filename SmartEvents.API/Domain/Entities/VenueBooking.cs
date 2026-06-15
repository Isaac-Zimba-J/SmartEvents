using SmartEvents.API.Domain.Enums;

namespace SmartEvents.API.Domain.Entities;

public class VenueBooking
{
    public Guid Id { get; set; }
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public VenueBookingStatus Status { get; set; } = VenueBookingStatus.Pending;
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? TransactionRef { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
