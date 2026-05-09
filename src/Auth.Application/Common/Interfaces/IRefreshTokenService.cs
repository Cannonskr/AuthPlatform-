using Auth.Application.Common.Models;

namespace Auth.Application.Common.Interfaces;

public interface IRefreshTokenService
{
    Task<string> GenerateRefreshTokenAsync(Guid userId, string jwtId, string? applicationCode = null, string? deviceInfo = null, string? ipAddress = null);
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(Guid userId);
}
