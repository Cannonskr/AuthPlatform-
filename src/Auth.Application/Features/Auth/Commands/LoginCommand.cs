using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using Auth.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password, string ApplicationCode) : IRequest<AuthenticationResult>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthenticationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthenticationResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
            return AuthenticationResult.Failed("Invalid email or password.");

        // Check lockout BEFORE verifying password to avoid unnecessary BCrypt computation
        // and prevent extending lockout timer on already-locked accounts
        if (!user.CanLogin())
            return AuthenticationResult.Failed("Account is locked or inactive.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLoginAttempt(5, 15);
            await _context.SaveChangesAsync(cancellationToken);
            return AuthenticationResult.Failed("Invalid email or password.");
        }

        user.RecordLogin();

        // Resolve roles and permissions
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        // Apply direct user permission overrides
        var directPermissions = user.UserPermissions
            .Where(up => up.IsGranted && (up.ExpiresAt is null || up.ExpiresAt > DateTime.UtcNow))
            .Select(up => up.Permission.Name)
            .ToList();

        permissions.AddRange(directPermissions);
        permissions = permissions.Distinct().ToList();

        var jwtToken = await _jwtService.GenerateTokenAsync(
            user.Id, user.Email, $"{user.FirstName} {user.LastName}",
            roles, permissions, request.ApplicationCode,
            user.TenantId?.ToString());

        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
            user.Id, jwtToken.TokenId, request.ApplicationCode);

        await _context.SaveChangesAsync(cancellationToken);

        return AuthenticationResult.Succeed(
            jwtToken.AccessToken,
            refreshToken,
            jwtToken.ExpiresAt,
            new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                Permissions = permissions
            });
    }
}
