namespace Auth.Application.Common.Interfaces;

public interface ITenantService
{
    string? CurrentTenantId { get; }
    string? CurrentTenantSlug { get; }
    bool IsMultiTenantEnabled { get; }
}
