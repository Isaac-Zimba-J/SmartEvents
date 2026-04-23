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
[Route("api/[controller]")]
public class VenuesController(SmartEventsDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<VenueResponse>>> GetAll(
        [FromQuery] string? city = null,
        [FromQuery] string? country = null,
        [FromQuery] VenueType? type = null,
        [FromQuery] int? minCapacity = null,
        [FromQuery] Guid? companyId = null)
    {
        var query = db.Venues
            .Include(v => v.Company)
            .Where(v => v.IsAvailable);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(v => v.City.ToLower().Contains(city.ToLower()));

        if (!string.IsNullOrWhiteSpace(country))
            query = query.Where(v => v.Country.ToLower().Contains(country.ToLower()));

        if (type.HasValue)
            query = query.Where(v => v.Type == type.Value);

        if (minCapacity.HasValue)
            query = query.Where(v => v.Capacity >= minCapacity.Value);

        if (companyId.HasValue)
            query = query.Where(v => v.CompanyId == companyId.Value);

        var venues = await query.OrderBy(v => v.Name).ToListAsync();
        return Ok(venues.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<VenueResponse>> GetById(Guid id)
    {
        var venue = await db.Venues.Include(v => v.Company).FirstOrDefaultAsync(v => v.Id == id);
        if (venue is null) return NotFound();
        return Ok(ToResponse(venue));
    }

    [HttpGet("company/{companyId:guid}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<VenueResponse>>> GetByCompany(Guid companyId)
    {
        var venues = await db.Venues
            .Include(v => v.Company)
            .Where(v => v.CompanyId == companyId)
            .OrderBy(v => v.Name)
            .ToListAsync();

        return Ok(venues.Select(ToResponse));
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<ActionResult<VenueResponse>> Create(CreateVenueRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(userId);
        if (user?.CompanyId is null)
            return BadRequest(new { message = "User must belong to a company to add venues." });

        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Capacity = request.Capacity,
            Type = request.Type,
            ImageUrl = request.ImageUrl,
            Amenities = request.Amenities,
            PricePerDay = request.PricePerDay,
            CompanyId = user.CompanyId.Value
        };

        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        await db.Entry(venue).Reference(v => v.Company).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = venue.Id }, ToResponse(venue));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<ActionResult<VenueResponse>> Update(Guid id, UpdateVenueRequest request)
    {
        var venue = await db.Venues.Include(v => v.Company).FirstOrDefaultAsync(v => v.Id == id);
        if (venue is null) return NotFound();

        venue.Name = request.Name;
        venue.Description = request.Description;
        venue.Address = request.Address;
        venue.City = request.City;
        venue.Country = request.Country;
        venue.Latitude = request.Latitude;
        venue.Longitude = request.Longitude;
        venue.Capacity = request.Capacity;
        venue.Type = request.Type;
        venue.ImageUrl = request.ImageUrl;
        venue.Amenities = request.Amenities;
        venue.PricePerDay = request.PricePerDay;
        venue.IsAvailable = request.IsAvailable;
        venue.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToResponse(venue));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var venue = await db.Venues.FindAsync(id);
        if (venue is null) return NotFound();
        venue.IsAvailable = false;
        venue.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static VenueResponse ToResponse(Venue v) => new(
        v.Id, v.Name, v.Description, v.Address, v.City, v.Country,
        v.Latitude, v.Longitude, v.Capacity, v.Type, v.ImageUrl,
        v.Amenities, v.PricePerDay, v.IsAvailable, v.CompanyId,
        v.Company?.Name ?? string.Empty, v.CreatedAt
    );
}
