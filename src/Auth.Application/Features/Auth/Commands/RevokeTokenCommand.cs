using Auth.Application.Common.Interfaces;
using MediatR;

namespace Auth.Application.Features.Auth.Commands;

public record RevokeTokenCommand(string RefreshToken) : IRequest;

public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICacheService _cacheService;
    private readonly IJwtService _jwtService;

    public RevokeTokenCommandHandler(
        IRefreshTokenService refreshTokenService,
        ICacheService cacheService,
        IJwtService jwtService)
    {
        _refreshTokenService = refreshTokenService;
        _cacheService = cacheService;
        _jwtService = jwtService;
    }

    public async Task Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken);

        // Also invalidate active access tokens that were paired with this refresh token
        // by attempting to validate and extract the JTI
        try
        {
            // The refresh token itself is being revoked above; the JWT blacklist is handled
            // at the JWT middleware level via OnTokenValidated event
            var storedToken = await _refreshTokenService.GetStoredRefreshTokenAsync(request.RefreshToken);
            if (storedToken?.JwtId is not null)
            {
                // Blacklist the JWT for the remainder of its natural lifetime
                var expirationSeconds = 15 * 60; // Default 15 minutes since JWTs are short-lived
                await _cacheService.SetAsync(
                    $"jti_blacklist:{storedToken.JwtId}",
                    "revoked",
                    TimeSpan.FromSeconds(expirationSeconds),
                    cancellationToken);
            }
        }
        catch
        {
            // JWT blacklisting is best-effort; the refresh token revocation is the primary mechanism
        }
    }
}
