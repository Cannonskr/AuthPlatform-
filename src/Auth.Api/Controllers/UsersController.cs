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
public class UsersController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantService _tenantService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser,
        ITenantService tenantService,
        ILogger<UsersController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of users scoped to the current tenant.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "Permission:users.read")]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .AsQueryable();

        // Apply tenant isolation if multi-tenant is enabled
        if (_tenantService.IsMultiTenantEnabled && Guid.TryParse(_tenantService.CurrentTenantId, out var tenantId))
        {
            query = query.Where(u => u.TenantId == tenantId);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync();

        return Ok(new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Gets a user by ID, scoped to the current tenant.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:users.read")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .Where(u => u.Id == id);

        // Apply tenant isolation if multi-tenant is enabled
        if (_tenantService.IsMultiTenantEnabled && Guid.TryParse(_tenantService.CurrentTenantId, out var tenantId))
        {
            query = query.Where(u => u.TenantId == tenantId);
        }

        var user = await query.FirstOrDefaultAsync();

        if (user is null)
            return NotFound(new { error = $"User with ID {id} not found." });

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        });
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Permission:users.create")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Username);

        if (existingUser is not null)
            return BadRequest(new { error = "A user with this email or username already exists." });

        var passwordHash = _passwordHasher.Hash(request.Password);

        // Assign TenantId from the current tenant context
        Guid? tenantId = null;
        if (_tenantService.IsMultiTenantEnabled && Guid.TryParse(_tenantService.CurrentTenantId, out var parsedTenantId))
        {
            tenantId = parsedTenantId;
        }

        var user = new User(
            request.Username,
            request.Email,
            passwordHash,
            request.FirstName,
            request.LastName,
            tenantId);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created: {UserId} ({Email}), Tenant: {TenantId}", user.Id, user.Email, tenantId);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:users.update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var query = _context.Users.Where(u => u.Id == id);

        // Apply tenant isolation if multi-tenant is enabled
        if (_tenantService.IsMultiTenantEnabled && Guid.TryParse(_tenantService.CurrentTenantId, out var tenantId))
        {
            query = query.Where(u => u.TenantId == tenantId);
        }

        var user = await query.FirstOrDefaultAsync();
        if (user is null)
            return NotFound(new { error = $"User with ID {id} not found." });

        user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Deletes a user.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Permission:users.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var query = _context.Users.Where(u => u.Id == id);

        // Apply tenant isolation if multi-tenant is enabled
        if (_tenantService.IsMultiTenantEnabled && Guid.TryParse(_tenantService.CurrentTenantId, out var tenantId))
        {
            query = query.Where(u => u.TenantId == tenantId);
        }

        var user = await query.FirstOrDefaultAsync();
        if (user is null)
            return NotFound(new { error = $"User with ID {id} not found." });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User deleted: {UserId}", id);

        return NoContent();
    }
}

public record CreateUserRequest(string Username, string Email, string Password, string FirstName, string LastName);
public record UpdateUserRequest(string FirstName, string LastName, string? PhoneNumber);
