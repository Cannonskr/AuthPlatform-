using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using Auth.Application.Features.Auth.Commands;
using FluentAssertions;
using Moq;

namespace Auth.Application.Tests.Commands;

public class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDelegateToRefreshTokenService()
    {
        var serviceMock = new Mock<IRefreshTokenService>();
        var handler = new RefreshTokenCommandHandler(serviceMock.Object);
        var expectedResult = AuthenticationResult.Succeed("jwt", "rt", DateTime.UtcNow, new UserDto());
        serviceMock.Setup(x => x.RefreshTokenAsync("test-rt")).ReturnsAsync(expectedResult);

        var result = await handler.Handle(new RefreshTokenCommand("test-rt"), CancellationToken.None);

        result.Should().Be(expectedResult);
        serviceMock.Verify(x => x.RefreshTokenAsync("test-rt"), Times.Once);
    }
}
