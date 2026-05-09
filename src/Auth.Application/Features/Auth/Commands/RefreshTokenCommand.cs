using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using MediatR;

namespace Auth.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthenticationResult>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthenticationResult>
{
    private readonly IRefreshTokenService _refreshTokenService;

    public RefreshTokenCommandHandler(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthenticationResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return await _refreshTokenService.RefreshTokenAsync(request.RefreshToken);
    }
}
