using Auth.Domain.Common;

namespace Auth.Domain.Entities;

public class Role : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public Guid ApplicationId { get; private set; }

    // Navigation properties
    public Application Application { get; private set; } = null!;
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private Role() { }

    public Role(string name, string description, Guid applicationId, bool isSystem = false)
        : this(default, name, description, applicationId, isSystem)
    {
    }

    public Role(Guid id, string name, string description, Guid applicationId, bool isSystem = false)
        : base(id)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        ApplicationId = applicationId;
        IsSystem = isSystem;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
