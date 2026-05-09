using Auth.Domain.Common;

namespace Auth.Domain.Entities;

public class UserPermission : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid PermissionId { get; private set; }
    public bool IsGranted { get; private set; }
    public Guid? GrantedBy { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;

    private UserPermission() { }

    public UserPermission(Guid userId, Guid permissionId, bool isGranted, Guid? grantedBy = null, DateTime? expiresAt = null)
    {
        UserId = userId;
        PermissionId = permissionId;
        IsGranted = isGranted;
        GrantedBy = grantedBy;
        ExpiresAt = expiresAt;
    }
}
