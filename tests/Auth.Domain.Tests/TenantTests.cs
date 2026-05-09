using Auth.Domain.Entities;
using FluentAssertions;

namespace Auth.Domain.Tests;

public class TenantTests
{
    [Fact]
    public void CreateTenant_ShouldSetProperties()
    {
        var tenant = new Tenant("Acme Corp", "acme", "Server=acme-db;...");

        tenant.Name.Should().Be("Acme Corp");
        tenant.Slug.Should().Be("acme");
        tenant.ConnectionString.Should().Be("Server=acme-db;...");
        tenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldChangeNameAndConnectionString()
    {
        var tenant = new Tenant("Old", "old", "old-cs");

        tenant.Update("New Corp", "new-cs");

        tenant.Name.Should().Be("New Corp");
        tenant.ConnectionString.Should().Be("new-cs");
        tenant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var tenant = new Tenant("Corp", "corp");
        tenant.Deactivate();

        tenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var tenant = new Tenant("Corp", "corp");
        tenant.Deactivate();
        tenant.Activate();

        tenant.IsActive.Should().BeTrue();
    }
}
