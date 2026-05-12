using Auth.Domain.Common;

namespace Auth.Domain.Entities;

public class Tenant : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? ConnectionString { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public ICollection<User> Users { get; private set; } = new List<User>();

    private Tenant() { }

    public Tenant(string name, string slug, string? connectionString = null)
    {
        Name = name;
        Slug = slug;
        ConnectionString = connectionString;
        IsActive = true;
    }

    public void Update(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
