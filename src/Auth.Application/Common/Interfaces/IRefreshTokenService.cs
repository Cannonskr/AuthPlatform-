using Auth.Application.Common.Models;
using Auth.Domain.Entities;

namespace Auth.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    Task<string> GenerateRefreshTokenAsync(Guid userId, string jwtId, string? applicationCode = null, string? deviceInfo = null, string? ipAddress = null);
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(Guid userId);
    Task<RefreshToken?> GetStoredRefreshTokenAsync(string refreshToken);
}
