using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/venue-bookings")]
[Authorize]
public class VenueBookingsController(SmartEventsDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<VenueBookingResponse>> Create(CreateVenueBookingRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (request.StartDate >= request.EndDate)
            return BadRequest(new { message = "Start date must be before end date." });

        if (request.StartDate <= DateTime.UtcNow || request.EndDate <= DateTime.UtcNow)
            return BadRequest(new { message = "Both dates must be in the future." });

        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == request.VenueId);
        if (venue is null) return NotFound(new { message = "Venue not found." });

        var hasConflict = await db.VenueBookings.AnyAsync(vb =>
            vb.VenueId == request.VenueId &&
            vb.Status == VenueBookingStatus.Confirmed &&
            vb.StartDate < request.EndDate &&
            vb.EndDate > request.StartDate);

        if (hasConflict)
            return Conflict(new { message = "Venue is already booked for these dates." });

        var startDate = request.StartDate;
        var endDate = request.EndDate;
        var days = Math.Max(1m, (decimal)(endDate.Date - startDate.Date).TotalDays);
        var totalAmount = (venue.PricePerDay ?? 0m) * days;

        var booking = new VenueBooking
        {
            Id = Guid.NewGuid(),
            VenueId = request.VenueId,
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate,
            Notes = request.Notes,
            Status = VenueBookingStatus.Confirmed,
            TotalAmount = totalAmount,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Completed,
            TransactionRef = $"VB-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.VenueBookings.Add(booking);
        await db.SaveChangesAsync();

        // Reload with venue navigation property for response
        booking.Venue = venue;

        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, ToResponse(booking));
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<VenueBookingResponse>>> GetMyBookings()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var bookings = await db.VenueBookings
            .Include(vb => vb.Venue)
            .Where(vb => vb.UserId == userId)
            .OrderByDescending(vb => vb.StartDate)
            .ToListAsync();

        return Ok(bookings.Select(ToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueBookingResponse>> GetById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var booking = await db.VenueBookings
            .Include(vb => vb.Venue)
            .FirstOrDefaultAsync(vb => vb.Id == id);

        if (booking is null) return NotFound(new { message = "Booking not found." });
        if (booking.UserId != userId) return Forbid();

        return Ok(ToResponse(booking));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var booking = await db.VenueBookings.FirstOrDefaultAsync(vb => vb.Id == id);

        if (booking is null) return NotFound(new { message = "Booking not found." });
        if (booking.UserId != userId) return Forbid();

        booking.Status = VenueBookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    private static VenueBookingResponse ToResponse(VenueBooking vb) => new(
        vb.Id, vb.VenueId,
        vb.Venue?.Name ?? "", vb.Venue?.Address ?? "", vb.Venue?.City ?? "",
        vb.StartDate, vb.EndDate, vb.Notes, vb.Status,
        vb.TotalAmount, vb.PaymentMethod, vb.PaymentStatus,
        vb.TransactionRef, vb.CreatedAt
    );
}
