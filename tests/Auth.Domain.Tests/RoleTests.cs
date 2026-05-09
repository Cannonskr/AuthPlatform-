using Auth.Domain.Entities;
using FluentAssertions;

namespace Auth.Domain.Tests;

public class RoleTests
{
    [Fact]
    public void CreateRole_ShouldSetProperties()
    {
        var appId = Guid.NewGuid();
        var role = new Role("Admin", "Administrator role", appId, isSystem: true);

        role.Name.Should().Be("Admin");
        role.NormalizedName.Should().Be("ADMIN");
        role.Description.Should().Be("Administrator role");
        role.ApplicationId.Should().Be(appId);
        role.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void CreateRole_WithId_ShouldSetGivenId()
    {
        var id = Guid.NewGuid();
        var role = new Role(id, "Viewer", "Read-only", Guid.NewGuid());

        role.Id.Should().Be(id);
    }

    [Fact]
    public void Update_ShouldChangeNameAndDescription()
    {
        var role = new Role("Admin", "Old desc", Guid.NewGuid());

        role.Update("SuperAdmin", "New description");

        role.Name.Should().Be("SuperAdmin");
        role.NormalizedName.Should().Be("SUPERADMIN");
        role.Description.Should().Be("New description");
        role.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
