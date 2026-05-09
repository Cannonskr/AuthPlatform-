using Auth.Domain.Common;

namespace Auth.Domain.Entities;

public class Permission : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Group { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
    public ICollection<UserPermission> UserPermissions { get; private set; } = new List<UserPermission>();

    private Permission() { }

    public Permission(string name, string displayName, string? description, string group, bool isSystem = false)
        : this(default, name, displayName, description, group, isSystem)
    {
    }

    public Permission(Guid id, string name, string displayName, string? description, string group, bool isSystem = false)
        : base(id)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
        Group = group;
        IsSystem = isSystem;
    }

    public void Update(string displayName, string? description, string group)
    {
        DisplayName = displayName;
        Description = description;
        Group = group;
    }
}
