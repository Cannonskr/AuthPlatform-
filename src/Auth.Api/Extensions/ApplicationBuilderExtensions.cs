using Auth.Api.Middleware;
using Auth.Persistence;
using Auth.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApiMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<TenantResolutionMiddleware>();

        return app;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            await context.Database.MigrateAsync();

            var isDevelopment = app.Environment.IsDevelopment();
            var bootstrapAdminEmail = app.Configuration["ADMIN_EMAIL"];
            var bootstrapAdminPassword = app.Configuration["ADMIN_PASSWORD"];
            var bootstrapApiKey = app.Configuration["ADMIN_API_KEY"];

            await ApplicationDbContextSeed.SeedAsync(
                context,
                isDevelopment,
                bootstrapAdminEmail,
                bootstrapAdminPassword,
                bootstrapApiKey);
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing the database.");
        }
    }
}
