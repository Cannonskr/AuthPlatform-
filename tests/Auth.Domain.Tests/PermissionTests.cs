using Auth.Domain.Entities;
using FluentAssertions;

namespace Auth.Domain.Tests;

public class PermissionTests
{
    [Fact]
    public void CreatePermission_ShouldSetProperties()
    {
        var permission = new Permission("users.read", "Read Users", "Can read users", "Users", isSystem: true);

        permission.Name.Should().Be("users.read");
        permission.DisplayName.Should().Be("Read Users");
        permission.Description.Should().Be("Can read users");
        permission.Group.Should().Be("Users");
        permission.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void CreatePermission_WithId_ShouldSetGivenId()
    {
        var id = Guid.NewGuid();
        var permission = new Permission(id, "test", "Test", null, "System");

        permission.Id.Should().Be(id);
    }

    [Fact]
    public void Update_ShouldChangeDisplayFields()
    {
        var permission = new Permission("users.read", "Old Name", "Old desc", "Users");

        permission.Update("New Name", "New desc", "System");

        permission.DisplayName.Should().Be("New Name");
        permission.Description.Should().Be("New desc");
        permission.Group.Should().Be("System");
    }
}
