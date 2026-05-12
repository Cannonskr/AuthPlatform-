using System.Security.Cryptography;
using Auth.Application.Common.Interfaces;
using Auth.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddControllers();

        // Add CORS - restrict in production
        services.AddCors(options =>
        {
            if (environment.IsDevelopment())
            {
                options.AddPolicy("ApiCors", builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            }
            else
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? throw new InvalidOperationException("Cors:AllowedOrigins must be configured in production.");

                options.AddPolicy("ApiCors", builder =>
                {
                    builder.WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            }
        });

        services.AddHealthChecks();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var issuer = jwtSettings["Issuer"] ?? "auth-platform";
        var publicKeyPath = jwtSettings["PublicKeyPath"];

        RSA? rsa = null;

        if (!string.IsNullOrEmpty(publicKeyPath) && File.Exists(publicKeyPath))
        {
            rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(publicKeyPath));
        }
        else
        {
            var publicKeyB64 = jwtSettings["PublicKey"];
            if (!string.IsNullOrEmpty(publicKeyB64))
            {
                rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyB64), out _);
            }
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = rsa is not null,
                IssuerSigningKey = rsa is not null ? new RsaSecurityKey(rsa.ExportParameters(false)) : null,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"] ?? issuer,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                // Only extract tokens from query string for SignalR/WebSocket endpoints
                // which cannot use Authorization headers. All other endpoints must use
                // the Authorization header.
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken)
                        && context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },

                // Check JWT blacklist (JTI) on every authenticated request
                OnTokenValidated = async context =>
                {
                    var jti = context.Principal?.FindFirst("jti")?.Value;
                    if (!string.IsNullOrEmpty(jti))
                    {
                        var cacheService = context.HttpContext.RequestServices.GetService<ICacheService>();
                        if (cacheService != null)
                        {
                            var isBlacklisted = await cacheService.GetAsync<string>($"jti_blacklist:{jti}");
                            if (isBlacklisted != null)
                            {
                                context.Fail("Token has been revoked.");
                                return;
                            }
                        }
                    }
                }
            };
        });

        services.AddAuthorization();

        return services;
    }
}
