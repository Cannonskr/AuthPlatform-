using Auth.Application.Common.Interfaces;
using Auth.Application.Features.Auth.Commands;
using Moq;

namespace Auth.Application.Tests.Commands;

public class RevokeTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDelegateToRefreshTokenService()
    {
        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        var cacheServiceMock = new Mock<ICacheService>();
        var jwtServiceMock = new Mock<IJwtService>();
        var handler = new RevokeTokenCommandHandler(refreshTokenServiceMock.Object, cacheServiceMock.Object, jwtServiceMock.Object);

        await handler.Handle(new RevokeTokenCommand("test-rt"), CancellationToken.None);

        refreshTokenServiceMock.Verify(x => x.RevokeRefreshTokenAsync("test-rt"), Times.Once);
    }
}
