using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IApplicationDbContext context, ILogger<RolesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of roles.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "Permission:roles.read")]
    [ProducesResponseType(typeof(PagedResult<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Roles.AsNoTracking();
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                ApplicationId = r.ApplicationId,
                PermissionCount = r.RolePermissions.Count
            })
            .ToListAsync();

        return Ok(new PagedResult<RoleDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Gets a role by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:roles.read")]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(Guid id)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role is null)
            return NotFound(new { error = $"Role with ID {id} not found." });

        return Ok(new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            ApplicationId = role.ApplicationId,
            Permissions = role.RolePermissions.Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                DisplayName = rp.Permission.DisplayName,
                Group = rp.Permission.Group
            }).ToList()
        });
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Permission:roles.create")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var app = await _context.Applications.FindAsync(request.ApplicationId);
        if (app is null)
            return BadRequest(new { error = "Application not found." });

        var role = new Role(request.Name, request.Description, request.ApplicationId);

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Role created: {RoleId} ({Name})", role.Id, role.Name);

        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            ApplicationId = role.ApplicationId
        });
    }

    /// <summary>
    /// Updates a role.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:roles.update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role is null)
            return NotFound(new { error = $"Role with ID {id} not found." });

        if (role.IsSystem)
            return BadRequest(new { error = "System roles cannot be modified." });

        role.Update(request.Name, request.Description);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Deletes a role.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:roles.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role is null)
            return NotFound(new { error = $"Role with ID {id} not found." });

        if (role.IsSystem)
            return BadRequest(new { error = "System roles cannot be deleted." });

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Role deleted: {RoleId}", id);

        return NoContent();
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    [HttpPost("{roleId:guid}/users/{userId:guid}")]
    [Authorize(Policy = "Permission:roles.update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRoleToUser(Guid roleId, Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            return BadRequest(new { error = "User not found." });

        var role = await _context.Roles.FindAsync(roleId);
        if (role is null)
            return BadRequest(new { error = "Role not found." });

        var existing = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (existing is not null)
            return BadRequest(new { error = "User already has this role assigned." });

        var userRole = new UserRole(userId, roleId);
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Role {RoleId} assigned to user {UserId}", roleId, userId);

        return NoContent();
    }

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    [HttpDelete("{roleId:guid}/users/{userId:guid}")]
    [Authorize(Policy = "Permission:roles.update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRoleFromUser(Guid roleId, Guid userId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole is null)
            return NotFound(new { error = "User does not have this role." });

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Role {RoleId} removed from user {UserId}", roleId, userId);

        return NoContent();
    }
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public Guid ApplicationId { get; set; }
    public int PermissionCount { get; set; }
}

public class RoleDetailDto : RoleDto
{
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Group { get; set; } = string.Empty;
}

public record CreateRoleRequest(string Name, string Description, Guid ApplicationId);
public record UpdateRoleRequest(string Name, string Description);
