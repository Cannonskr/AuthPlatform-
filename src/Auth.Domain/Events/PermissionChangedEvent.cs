using Auth.Domain.Common;

namespace Auth.Domain.Events;

public class PermissionChangedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string PermissionName { get; }
    public bool IsGranted { get; }

    public PermissionChangedEvent(Guid userId, string permissionName, bool isGranted)
    {
        UserId = userId;
        PermissionName = permissionName;
        IsGranted = isGranted;
    }
}
