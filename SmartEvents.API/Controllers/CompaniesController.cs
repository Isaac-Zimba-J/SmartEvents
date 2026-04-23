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

    private static CompanyResponse ToResponse(Company c) => new(
        c.Id, c.Name, c.Slug, c.Description, c.LogoUrl,
        c.Website, c.Phone, c.Email, c.Address, c.IsActive, c.CreatedAt
    );
}
