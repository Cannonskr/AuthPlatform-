using Auth.Application.Common.Interfaces;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(IApplicationDbContext context, ILogger<ApplicationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of applications.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _context.Applications.AsQueryable();
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Code,
                a.Description,
                a.IsActive,
                a.CreatedAt
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
    /// Gets an application by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app is null)
            return NotFound(new { error = $"Application with ID {id} not found." });

        return Ok(new
        {
            app.Id,
            app.Name,
            app.Code,
            app.Description,
            app.IsActive,
            app.CreatedAt
        });
    }

    /// <summary>
    /// Creates a new application.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest request)
    {
        var existing = await _context.Applications
            .FirstOrDefaultAsync(a => a.Code == request.Code);

        if (existing is not null)
            return BadRequest(new { error = "An application with this code already exists." });

        var apiKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var app = new Domain.Entities.Application(
            request.Name,
            request.Code,
            request.Description,
            apiKey);

        _context.Applications.Add(app);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Application created: {AppId} ({Code})", app.Id, app.Code);

        return CreatedAtAction(nameof(GetApplication), new { id = app.Id }, new
        {
            app.Id,
            app.Name,
            app.Code,
            app.Description,
            app.ApiKey,
            app.IsActive,
            app.CreatedAt
        });
    }

    /// <summary>
    /// Updates an application.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApplication(Guid id, [FromBody] UpdateApplicationRequest request)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app is null)
            return NotFound(new { error = $"Application with ID {id} not found." });

        app.Update(request.Name, request.Description);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Deactivates an application.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateApplication(Guid id)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app is null)
            return NotFound(new { error = $"Application with ID {id} not found." });

        app.Deactivate();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Application deactivated: {AppId}", id);

        return NoContent();
    }

    /// <summary>
    /// Rotates the API key for an application.
    /// </summary>
    [HttpPost("{id:guid}/rotate-key")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateApiKey(Guid id)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app is null)
            return NotFound(new { error = $"Application with ID {id} not found." });

        var newApiKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        app.RotateApiKey(newApiKey);
        await _context.SaveChangesAsync();

        _logger.LogInformation("API key rotated for application: {AppId}", id);

        return Ok(new { apiKey = newApiKey });
    }
}

public record CreateApplicationRequest(string Name, string Code, string? Description);
public record UpdateApplicationRequest(string Name, string? Description);
