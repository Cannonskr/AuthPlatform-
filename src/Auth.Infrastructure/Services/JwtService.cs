using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Auth.Application.Common.Interfaces;
using Auth.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtService> _logger;

    private const string ClaimTypeApp = "app";
    private const string ClaimTypeTenant = "tenant";
    private const string ClaimTypePermission = "permission";

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings not configured");
    }

    public Task<JwtToken> GenerateTokenAsync(Guid userId, string email, string fullName,
        IList<string> roles, IList<string> permissions, string applicationCode, string? tenantId)
    {
        var tokenId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, fullName),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new(ClaimTypeApp, applicationCode),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(tenantId))
            claims.Add(new Claim(ClaimTypeTenant, tenantId));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in permissions)
            claims.Add(new Claim(ClaimTypePermission, permission));

        var rsaKey = GetPrivateKey();
        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsaKey), SecurityAlgorithms.RsaSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: applicationCode,
            claims: claims,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        var tokenHandler = new JwtSecurityTokenHandler();
        var accessToken = tokenHandler.WriteToken(token);

        return Task.FromResult(new JwtToken
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            TokenId = tokenId
        });
    }

    public Task<Guid?> ValidateTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var rsaKey = GetPublicKey();

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsaKey),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
            return Task.FromResult<Guid?>(userIdClaim != null ? Guid.Parse(userIdClaim.Value) : null);
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: token expired.");
            return Task.FromResult<Guid?>(null);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: invalid token.");
            return Task.FromResult<Guid?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed with unexpected error.");
            return Task.FromResult<Guid?>(null);
        }
    }

    private RSA GetPrivateKey()
    {
        var rsa = RSA.Create();
        var privateKeyPath = _jwtSettings.PrivateKeyPath;
        if (!string.IsNullOrEmpty(privateKeyPath) && File.Exists(privateKeyPath))
        {
            rsa.ImportFromPem(File.ReadAllText(privateKeyPath));
            return rsa;
        }

        var privateKeyB64 = _jwtSettings.PrivateKey;
        if (!string.IsNullOrEmpty(privateKeyB64))
        {
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyB64), out _);
            return rsa;
        }

        throw new InvalidOperationException(
            "JWT private key not found. Configure JwtSettings:PrivateKeyPath or JwtSettings:PrivateKey.");
    }

    private RSA GetPublicKey()
    {
        var rsa = RSA.Create();
        var publicKeyPath = _jwtSettings.PublicKeyPath;
        if (!string.IsNullOrEmpty(publicKeyPath) && File.Exists(publicKeyPath))
        {
            rsa.ImportFromPem(File.ReadAllText(publicKeyPath));
            return rsa;
        }

        var publicKeyB64 = _jwtSettings.PublicKey;
        if (!string.IsNullOrEmpty(publicKeyB64))
        {
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyB64), out _);
            return rsa;
        }

        throw new InvalidOperationException(
            "JWT public key not found. Configure JwtSettings:PublicKeyPath or JwtSettings:PublicKey.");
    }
}

public class JwtSettings
{
    public string Issuer { get; set; } = "auth-platform";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKeyPath { get; set; } = string.Empty;
    public string PublicKeyPath { get; set; } = string.Empty;
}
