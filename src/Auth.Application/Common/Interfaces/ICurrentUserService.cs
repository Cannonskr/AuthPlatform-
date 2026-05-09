namespace Auth.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    string? IpAddress { get; }
    IList<string> Roles { get; }
    IList<string> Permissions { get; }
    string? ApplicationCode { get; }
    string? TenantId { get; }
    bool IsAuthenticated { get; }
}
