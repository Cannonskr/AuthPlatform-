using Auth.Domain.Common;

namespace Auth.Domain.Events;

public class UserLoggedInEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string? IpAddress { get; }

    public UserLoggedInEvent(Guid userId, string email, string? ipAddress = null)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
    }
}
