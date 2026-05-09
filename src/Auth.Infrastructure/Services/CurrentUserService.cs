using System.Security.Claims;
using Auth.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Auth.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var subClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub");
            return subClaim is not null && Guid.TryParse(subClaim.Value, out var userId) ? userId : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;

    public string? FullName => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
        ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();

    public IList<string> Roles => _httpContextAccessor.HttpContext?.User?
        .FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

    public IList<string> Permissions => _httpContextAccessor.HttpContext?.User?
        .FindAll("permission").Select(c => c.Value).ToList() ?? new List<string>();

    public string? ApplicationCode => _httpContextAccessor.HttpContext?.User?
        .FindFirst("app")?.Value;

    public string? TenantId => _httpContextAccessor.HttpContext?.User?
        .FindFirst("tenant")?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
