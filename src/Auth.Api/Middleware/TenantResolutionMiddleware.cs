namespace Auth.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Resolve tenant from header or subdomain
        var tenantSlug = context.Request.Headers["X-Tenant"].FirstOrDefault();

        if (string.IsNullOrEmpty(tenantSlug))
        {
            // Try to resolve from subdomain (e.g., tenant1.authplatform.local)
            var host = context.Request.Host.Host;
            var parts = host?.Split('.');
            if (parts is { Length: > 2 })
            {
                tenantSlug = parts[0];
            }
        }

        if (!string.IsNullOrEmpty(tenantSlug))
        {
            context.Items["TenantSlug"] = tenantSlug;
        }

        await _next(context);
    }
}
