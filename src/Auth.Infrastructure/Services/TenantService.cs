using Auth.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Auth.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUserService;

    public TenantService(IHttpContextAccessor httpContextAccessor, ICurrentUserService currentUserService)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentUserService = currentUserService;
    }

    public string? CurrentTenantId => _currentUserService.TenantId;

    public string? CurrentTenantSlug
    {
        get
        {
            // First try from resolved middleware (X-Tenant header or subdomain)
            var slug = _httpContextAccessor.HttpContext?.Items["TenantSlug"] as string;
            if (!string.IsNullOrEmpty(slug))
                return slug;

            // Fall back to JWT tenant claim
            return _currentUserService.TenantId;
        }
    }

    public bool IsMultiTenantEnabled => true;
}
