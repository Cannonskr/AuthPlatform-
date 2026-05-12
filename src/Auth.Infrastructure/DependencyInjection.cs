using Auth.Application.Common.Interfaces;
using Auth.Infrastructure.Authorization;
using Auth.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // JWT Authentication is configured in the API layer (ServiceCollectionExtensions)
        // Only register infrastructure services here

        // Authorization
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        return services;
    }
}
