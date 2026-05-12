using System.Security.Cryptography;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Auth.Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly RefreshTokenSettings _settings;

    public RefreshTokenService(IApplicationDbContext context, IJwtService jwtService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _settings = configuration.GetSection("RefreshTokenSettings").Get<RefreshTokenSettings>()
            ?? new RefreshTokenSettings();
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId, string jwtId,
        string? applicationCode = null, string? deviceInfo = null, string? ipAddress = null)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        // Hash the token before storing
        var tokenHash = ComputeHash(token);

        var refreshToken = new RefreshToken(
            tokenHash,
            jwtId,
            userId,
            DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays),
            applicationCode,
            deviceInfo,
            ipAddress);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        // Return the unhashed token to the client
        return token;
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = ComputeHash(refreshToken);

        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserPermissions)
                    .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(rt => rt.Token == tokenHash);

        if (storedToken is null)
            return AuthenticationResult.Failed("Invalid refresh token.");

        if (!storedToken.IsActive)
            return AuthenticationResult.Failed("Refresh token is expired or has been used.");

        // Mark old token as used (rotation)
        storedToken.MarkAsUsed();

        var user = storedToken.User;
        if (!user.CanLogin())
            return AuthenticationResult.Failed("Account is locked or inactive.");

        // Resolve permissions
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var directPermissions = user.UserPermissions
            .Where(up => up.IsGranted && (up.ExpiresAt is null || up.ExpiresAt > DateTime.UtcNow))
            .Select(up => up.Permission.Name)
            .ToList();

        permissions.AddRange(directPermissions);
        permissions = permissions.Distinct().ToList();

        // Use the stored application code from the original refresh token
        var applicationCode = storedToken.ApplicationCode ?? "unknown";

        // Generate new tokens
        var jwtToken = await _jwtService.GenerateTokenAsync(
            user.Id, user.Email, $"{user.FirstName} {user.LastName}",
            roles, permissions, applicationCode, user.TenantId?.ToString());

        var newRefreshToken = await GenerateRefreshTokenAsync(
            user.Id, jwtToken.TokenId, applicationCode);

        await _context.SaveChangesAsync();

        return AuthenticationResult.Succeed(
            jwtToken.AccessToken,
            newRefreshToken,
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

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = ComputeHash(refreshToken);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == tokenHash);

        if (storedToken is not null)
        {
            storedToken.Revoke();
            await _context.SaveChangesAsync();
        }
    }

    public async Task<RefreshToken?> GetStoredRefreshTokenAsync(string refreshToken)
    {
        var tokenHash = ComputeHash(refreshToken);
        return await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == tokenHash);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync();

        foreach (var token in activeTokens)
            token.Revoke();

        await _context.SaveChangesAsync();
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}

public class RefreshTokenSettings
{
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
