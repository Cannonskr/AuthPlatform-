using Auth.Domain.Common;
using Auth.Domain.Enums;

namespace Auth.Domain.Entities;

public class User : BaseAuditableEntity
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public bool IsLocked { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public int AccessFailedCount { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public Guid? TenantId { get; private set; }

    // Navigation properties
    public Tenant? Tenant { get; private set; }
    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public ICollection<UserPermission> UserPermissions { get; private set; } = new List<UserPermission>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private User() { }

    public User(string username, string email, string passwordHash, string firstName, string lastName, Guid? tenantId = null)
        : this(default, username, email, passwordHash, firstName, lastName, tenantId)
    {
    }

    public User(Guid id, string username, string email, string passwordHash, string firstName, string lastName, Guid? tenantId = null)
        : base(id)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        TenantId = tenantId;
        Status = UserStatus.Active;
    }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEmail(string email)
    {
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Note: VerifyPassword has been intentionally removed to avoid bypassing BCrypt verification.
    /// Use IPasswordHasher.Verify() from the Infrastructure layer instead.
    /// </summary>

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        AccessFailedCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailedLoginAttempt(int maxAttempts, int lockoutMinutes)
    {
        AccessFailedCount++;
        if (AccessFailedCount >= maxAttempts)
        {
            IsLocked = true;
            LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        }
    }

    public void Unlock()
    {
        IsLocked = false;
        LockoutEnd = null;
        AccessFailedCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanLogin()
    {
        return Status == UserStatus.Active
            && !IsLocked
            && (LockoutEnd is null || LockoutEnd < DateTime.UtcNow);
    }
}
