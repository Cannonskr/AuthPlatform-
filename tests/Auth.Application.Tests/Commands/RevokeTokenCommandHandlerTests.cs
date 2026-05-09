using Auth.Application.Common.Interfaces;
using Auth.Application.Features.Auth.Commands;
using Moq;

namespace Auth.Application.Tests.Commands;

public class RevokeTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDelegateToRefreshTokenService()
    {
        var serviceMock = new Mock<IRefreshTokenService>();
        var handler = new RevokeTokenCommandHandler(serviceMock.Object);

        await handler.Handle(new RevokeTokenCommand("test-rt"), CancellationToken.None);

        serviceMock.Verify(x => x.RevokeRefreshTokenAsync("test-rt"), Times.Once);
    }
}
