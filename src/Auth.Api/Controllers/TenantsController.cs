using Auth.Application.Common.Interfaces;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(IApplicationDbContext context, ILogger<TenantsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of tenants.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "Permission:tenants.read")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenants([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _context.Tenants.AsQueryable();
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.IsActive,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize
        });
    }

    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:tenants.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant is null)
            return NotFound(new { error = $"Tenant with ID {id} not found." });

        return Ok(new
        {
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.IsActive,
            tenant.CreatedAt
        });
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Permission:tenants.manage")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        var existing = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == request.Slug);

        if (existing is not null)
            return BadRequest(new { error = "A tenant with this slug already exists." });

        var tenant = new Tenant(request.Name, request.Slug);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant created: {TenantId} ({Slug})", tenant.Id, tenant.Slug);

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, new
        {
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.IsActive,
            tenant.CreatedAt
        });
    }

    /// <summary>
    /// Updates a tenant.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:tenants.manage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant is null)
            return NotFound(new { error = $"Tenant with ID {id} not found." });

        tenant.Update(request.Name, request.ConnectionString);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Deactivates a tenant.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:tenants.manage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTenant(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant is null)
            return NotFound(new { error = $"Tenant with ID {id} not found." });

        tenant.Deactivate();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant deactivated: {TenantId}", id);

        return NoContent();
    }
}

public record CreateTenantRequest(string Name, string Slug);
public record UpdateTenantRequest(string Name, string? ConnectionString);
