using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Application.Interfaces;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/venue-bookings")]
[Authorize]
public class VenueBookingsController(SmartEventsDbContext db, IPawaPayService pawaPayService) : ControllerBase
{
    private static readonly Dictionary<PaymentMethod, string> CorrespondentMap = new()
    {
        [PaymentMethod.AirtelMoney] = "AIRTEL_OAPI_ZMB",
        [PaymentMethod.MTNMoMo] = "MTN_MOMO_ZMB"
    };

    [HttpPost]
    public async Task<ActionResult<VenueBookingResponse>> Create(CreateVenueBookingRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (request.StartDate >= request.EndDate)
            return BadRequest(new { message = "Start date must be before end date." });

        if (request.StartDate <= DateTime.UtcNow || request.EndDate <= DateTime.UtcNow)
            return BadRequest(new { message = "Both dates must be in the future." });

        var isMobileMoney = CorrespondentMap.ContainsKey(request.PaymentMethod);
        if (isMobileMoney && string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest(new { message = "Phone number is required for mobile money payments." });

        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == request.VenueId);
        if (venue is null) return NotFound(new { message = "Venue not found." });

        var hasConflict = await db.VenueBookings.AnyAsync(vb =>
            vb.VenueId == request.VenueId &&
            vb.Status == VenueBookingStatus.Confirmed &&
            vb.StartDate < request.EndDate &&
            vb.EndDate > request.StartDate);

        if (hasConflict)
            return Conflict(new { message = "Venue is already booked for these dates." });

        var days = Math.Max(1m, (decimal)(request.EndDate.Date - request.StartDate.Date).TotalDays);
        var totalAmount = (venue.PricePerDay ?? 0m) * days;

        var depositId = isMobileMoney ? Guid.NewGuid().ToString() : null;

        var booking = new VenueBooking
        {
            Id = Guid.NewGuid(),
            VenueId = request.VenueId,
            UserId = userId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = request.Notes,
            Status = isMobileMoney ? VenueBookingStatus.Pending : VenueBookingStatus.Confirmed,
            TotalAmount = totalAmount,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = isMobileMoney ? PaymentStatus.Pending : PaymentStatus.Completed,
            TransactionRef = isMobileMoney ? null : $"VB-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            PawaPayDepositId = depositId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.VenueBookings.Add(booking);
        await db.SaveChangesAsync();

        if (isMobileMoney && depositId is not null)
        {
            try
            {
                await pawaPayService.InitiateDepositAsync(
                    depositId,
                    totalAmount,
                    "ZMW",
                    CorrespondentMap[request.PaymentMethod],
                    request.PhoneNumber!
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"PawaPay venue booking initiation warning: {ex.Message}");
            }
        }

        booking.Venue = venue;
        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, ToResponse(booking));
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<VenueBookingStatusResponse>> GetStatus(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var booking = await db.VenueBookings.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (booking is null) return NotFound(new { message = "Booking not found." });

        if (booking.PaymentStatus is PaymentStatus.Completed or PaymentStatus.Failed)
            return Ok(new VenueBookingStatusResponse(booking.Id, booking.Status, booking.PaymentStatus, booking.TransactionRef));

        if (booking.PawaPayDepositId is not null)
        {
            try
            {
                var remote = await pawaPayService.GetDepositStatusAsync(booking.PawaPayDepositId);

                if (remote.Status == "COMPLETED")
                {
                    booking.PaymentStatus = PaymentStatus.Completed;
                    booking.TransactionRef = booking.PawaPayDepositId;
                    booking.Status = VenueBookingStatus.Confirmed;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                else if (remote.Status == "FAILED")
                {
                    booking.PaymentStatus = PaymentStatus.Failed;
                    booking.Status = VenueBookingStatus.Cancelled;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"PawaPay venue booking status poll warning: {ex.Message}");
            }
        }

        return Ok(new VenueBookingStatusResponse(booking.Id, booking.Status, booking.PaymentStatus, booking.TransactionRef));
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
