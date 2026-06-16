using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEvents.API.Application.DTOs;
using SmartEvents.API.Application.Helpers;
using SmartEvents.API.Domain.Entities;
using SmartEvents.API.Domain.Enums;
using SmartEvents.API.Infrastructure.Data;

namespace SmartEvents.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController(SmartEventsDbContext db) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CompanyResponse>>> GetAll()
    {
        var companies = await db.Companies
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => ToResponse(c))
            .ToListAsync();

        return Ok(companies);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<CompanyResponse>> GetById(Guid id)
    {
        var company = await db.Companies.FindAsync(id);
        if (company is null) return NotFound();
        return Ok(ToResponse(company));
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.SuperAdmin))]
    public async Task<ActionResult<CompanyResponse>> Create(CreateCompanyRequest request)
    {
        var slug = SlugHelper.Generate(request.Name);
        if (await db.Companies.AnyAsync(c => c.Slug == slug))
            slug = SlugHelper.GenerateUnique(request.Name, Guid.NewGuid().ToString("N")[..6]);

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            Website = request.Website,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            LogoUrl = request.LogoUrl
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = company.Id }, ToResponse(company));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<ActionResult<CompanyResponse>> Update(Guid id, UpdateCompanyRequest request)
    {
        var company = await db.Companies.FindAsync(id);
        if (company is null) return NotFound();

        company.Name = request.Name;
        company.Description = request.Description;
        company.Website = request.Website;
        company.Phone = request.Phone;
        company.Email = request.Email;
        company.Address = request.Address;
        company.LogoUrl = request.LogoUrl;
        company.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToResponse(company));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.SuperAdmin))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var company = await db.Companies.FindAsync(id);
        if (company is null) return NotFound();

        company.IsActive = false;
        company.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    // ── Member Management ──────────────────────────────────────────────────

    [HttpGet("{id:guid}/members")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<ActionResult<IEnumerable<CompanyMemberResponse>>> GetMembers(Guid id)
    {
        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var caller = await db.Users.FindAsync(callerId);
        if (caller is null) return Unauthorized();

        if (caller.Role == UserRole.CompanyAdmin && caller.CompanyId != id)
            return Forbid();

        var company = await db.Companies.FindAsync(id);
        if (company is null) return NotFound(new { message = "Company not found." });

        var members = await db.Users
            .Where(u => u.CompanyId == id)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();

        return Ok(members.Select(ToMemberResponse));
    }

    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<ActionResult<CompanyMemberResponse>> AddMember(Guid id, AddCompanyMemberRequest request)
    {
        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var caller = await db.Users.FindAsync(callerId);
        if (caller is null) return Unauthorized();

        if (caller.Role == UserRole.CompanyAdmin && caller.CompanyId != id)
            return Forbid();

        var company = await db.Companies.FindAsync(id);
        if (company is null) return NotFound(new { message = "Company not found." });

        if (request.Role == UserRole.SuperAdmin)
            return BadRequest(new { message = "Cannot assign the SuperAdmin role to a company member." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (user is null) return NotFound(new { message = "User not found." });

        if (user.CompanyId is not null)
            return Conflict(new { message = "User is already a member of a company." });

        user.CompanyId = id;
        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToMemberResponse(user));
    }

    [HttpPut("{id:guid}/members/{userId:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<ActionResult<CompanyMemberResponse>> UpdateMemberRole(Guid id, Guid userId, UpdateMemberRoleRequest request)
    {
        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var caller = await db.Users.FindAsync(callerId);
        if (caller is null) return Unauthorized();

        if (caller.Role == UserRole.CompanyAdmin && caller.CompanyId != id)
            return Forbid();

        var company = await db.Companies.FindAsync(id);
        if (company is null) return NotFound(new { message = "Company not found." });

        if (request.Role == UserRole.SuperAdmin)
            return BadRequest(new { message = "Cannot assign the SuperAdmin role to a company member." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == id);
        if (user is null) return NotFound(new { message = "Member not found in this company." });

        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToMemberResponse(user));
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.SuperAdmin)},{nameof(UserRole.CompanyAdmin)}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var caller = await db.Users.FindAsync(callerId);
        if (caller is null) return Unauthorized();

        if (caller.Role == UserRole.CompanyAdmin && caller.CompanyId != id)
            return Forbid();

        var company = await db.Companies.FindAsync(id);
        if (company is null) return NotFound(new { message = "Company not found." });

        if (userId == callerId)
            return BadRequest(new { message = "You cannot remove yourself from the company." });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.CompanyId == id);
        if (user is null) return NotFound(new { message = "Member not found in this company." });

        user.CompanyId = null;
        user.Role = UserRole.Attendee;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static CompanyResponse ToResponse(Company c) => new(
        c.Id, c.Name, c.Slug, c.Description, c.LogoUrl,
        c.Website, c.Phone, c.Email, c.Address, c.IsActive, c.CreatedAt
    );

    private static CompanyMemberResponse ToMemberResponse(User u) => new(
        u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.CreatedAt
    );
}
