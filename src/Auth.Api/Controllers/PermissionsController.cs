using Auth.Application.Common.Interfaces;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(IApplicationDbContext context, ILogger<PermissionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available permissions, optionally filtered by group.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions([FromQuery] string? group = null)
    {
        var query = _context.Permissions.AsQueryable();

        if (!string.IsNullOrEmpty(group))
        {
            query = query.Where(p => p.Group == group);
        }

        var permissions = await query
            .OrderBy(p => p.Group)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.DisplayName,
                p.Description,
                p.Group,
                p.IsSystem
            })
            .ToListAsync();

        return Ok(permissions);
    }

    /// <summary>
    /// Gets permission groups.
    /// </summary>
    [HttpGet("groups")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionGroups()
    {
        var groups = await _context.Permissions
            .Select(p => p.Group)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

        return Ok(groups);
    }

    /// <summary>
    /// Gets all permissions assigned to a specific role.
    /// </summary>
    [HttpGet("roles/{roleId:guid}")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePermissions(Guid roleId)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role is null)
            return NotFound(new { error = "Role not found." });

        var permissions = await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => new
            {
                rp.Permission.Id,
                rp.Permission.Name,
                rp.Permission.DisplayName,
                rp.Permission.Group
            })
            .ToListAsync();

        return Ok(permissions);
    }

    /// <summary>
    /// Gets all permissions for a specific user (direct + inherited from roles).
    /// </summary>
    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserPermissions(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return NotFound(new { error = "User not found." });

        var rolePermissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission)
            .Distinct()
            .ToList();

        var directPermissions = user.UserPermissions
            .Where(up => up.ExpiresAt is null || up.ExpiresAt > DateTime.UtcNow)
            .ToList();

        return Ok(new
        {
            RolePermissions = rolePermissions.Select(p => new
            {
                p.Id, p.Name, p.DisplayName, p.Group
            }),
            DirectPermissions = directPermissions.Select(up => new
            {
                up.Permission.Id,
                up.Permission.Name,
                up.Permission.DisplayName,
                up.Permission.Group,
                up.IsGranted,
                up.ExpiresAt
            })
        });
    }

    /// <summary>
    /// Assigns a permission to a role.
    /// </summary>
    [HttpPost("roles/{roleId:guid}/{permissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignPermissionToRole(Guid roleId, Guid permissionId)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role is null)
            return BadRequest(new { error = "Role not found." });

        var permission = await _context.Permissions.FindAsync(permissionId);
        if (permission is null)
            return BadRequest(new { error = "Permission not found." });

        var existing = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (existing is not null)
            return BadRequest(new { error = "Permission already assigned to this role." });

        var rolePermission = new RolePermission(roleId, permissionId);
        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Permission {PermissionId} assigned to role {RoleId}", permissionId, roleId);

        return NoContent();
    }

    /// <summary>
    /// Removes a permission from a role.
    /// </summary>
    [HttpDelete("roles/{roleId:guid}/{permissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemovePermissionFromRole(Guid roleId, Guid permissionId)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role is null)
            return BadRequest(new { error = "Role not found." });

        if (role.IsSystem)
            return BadRequest(new { error = "Cannot modify system role permissions." });

        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (rolePermission is null)
            return NotFound(new { error = "Permission not assigned to this role." });

        _context.RolePermissions.Remove(rolePermission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Permission {PermissionId} removed from role {RoleId}", permissionId, roleId);

        return NoContent();
    }

    /// <summary>
    /// Assigns a permission directly to a user (grant or deny override).
    /// </summary>
    [HttpPost("users/{userId:guid}/{permissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignPermissionToUser(Guid userId, Guid permissionId,
        [FromQuery] bool isGranted = true, [FromQuery] DateTime? expiresAt = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            return BadRequest(new { error = "User not found." });

        var permission = await _context.Permissions.FindAsync(permissionId);
        if (permission is null)
            return BadRequest(new { error = "Permission not found." });

        var existing = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (existing is not null)
        {
            _context.UserPermissions.Remove(existing);
        }

        var userPermission = new UserPermission(userId, permissionId, isGranted, null, expiresAt);
        _context.UserPermissions.Add(userPermission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Permission override {PermissionId} set for user {UserId} (granted={IsGranted})",
            permissionId, userId, isGranted);

        return NoContent();
    }

    /// <summary>
    /// Removes a direct permission override from a user.
    /// </summary>
    [HttpDelete("users/{userId:guid}/{permissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermissionFromUser(Guid userId, Guid permissionId)
    {
        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (userPermission is null)
            return NotFound(new { error = "Permission override not found for this user." });

        _context.UserPermissions.Remove(userPermission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Permission override removed for user {UserId}: {PermissionId}", userId, permissionId);

        return NoContent();
    }
}
