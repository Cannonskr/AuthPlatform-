using Auth.Domain.Common;

namespace Auth.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; private set; } = string.Empty;
    public string JwtId { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public bool IsRevoked { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ApplicationCode { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsUsed && !IsExpired;

    private RefreshToken() { }

    public RefreshToken(string token, string jwtId, Guid userId, DateTime expiresAt,
        string? applicationCode = null, string? deviceInfo = null, string? ipAddress = null)
    {
        Token = token;
        JwtId = jwtId;
        UserId = userId;
        ExpiresAt = expiresAt;
        ApplicationCode = applicationCode;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }
}
