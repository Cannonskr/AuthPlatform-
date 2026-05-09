using Auth.Domain.Entities;
using FluentAssertions;

namespace Auth.Domain.Tests;

public class ApplicationTests
{
    [Fact]
    public void CreateApplication_ShouldSetProperties()
    {
        var app = new Application("Admin Portal", "admin-portal", "Main admin app", "api-key-123");

        app.Name.Should().Be("Admin Portal");
        app.Code.Should().Be("admin-portal");
        app.Description.Should().Be("Main admin app");
        app.ApiKey.Should().Be("api-key-123");
        app.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreateApplication_WithId_ShouldSetGivenId()
    {
        var id = Guid.NewGuid();
        var app = new Application(id, "Test", "test", null, "key");

        app.Id.Should().Be(id);
    }

    [Fact]
    public void Update_ShouldChangeNameAndDescription()
    {
        var app = new Application("Old", "code", "Old desc", "key");

        app.Update("New Name", "New desc");

        app.Name.Should().Be("New Name");
        app.Description.Should().Be("New desc");
        app.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var app = new Application("App", "code", null, "key");
        app.Deactivate();

        app.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var app = new Application("App", "code", null, "key");
        app.Deactivate();
        app.Activate();

        app.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RotateApiKey_ShouldChangeKey()
    {
        var app = new Application("App", "code", null, "old-key");

        app.RotateApiKey("new-key");

        app.ApiKey.Should().Be("new-key");
    }
}
