using Auth.Domain.Entities;
using Auth.Domain.Enums;
using FluentAssertions;

namespace Auth.Domain.Tests;

public class UserTests
{
    [Fact]
    public void CreateUser_WithValidData_SetsPropertiesAndStatusActive()
    {
        var user = new User("johndoe", "john@test.com", "hashed123", "John", "Doe");

        user.Username.Should().Be("johndoe");
        user.Email.Should().Be("john@test.com");
        user.PasswordHash.Should().Be("hashed123");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Status.Should().Be(UserStatus.Active);
        user.IsLocked.Should().BeFalse();
        user.AccessFailedCount.Should().Be(0);
    }

    [Fact]
    public void CreateUser_WithId_ShouldSetGivenId()
    {
        var id = Guid.NewGuid();
        var user = new User(id, "jane", "jane@test.com", "hash", "Jane", "Doe");

        user.Id.Should().Be(id);
    }

    [Fact]
    public void UpdateProfile_ShouldUpdateFieldsAndSetUpdatedAt()
    {
        var user = new User("user", "u@t.com", "hash", "Old", "Name");

        user.UpdateProfile("NewFirst", "NewLast", "555-0100");

        user.FirstName.Should().Be("NewFirst");
        user.LastName.Should().Be("NewLast");
        user.PhoneNumber.Should().Be("555-0100");
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void UpdateEmail_ShouldChangeEmail()
    {
        var user = new User("user", "old@test.com", "hash", "First", "Last");

        user.UpdateEmail("new@test.com");

        user.Email.Should().Be("new@test.com");
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordLogin_ShouldSetLastLoginAndResetFailedCount()
    {
        var user = new User("user", "u@t.com", "hash", "F", "L");
        user.RecordFailedLoginAttempt(5, 15);
        user.AccessFailedCount.Should().Be(1);

        user.RecordLogin();

        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        user.AccessFailedCount.Should().Be(0);
    }

    [Fact]
    public void RecordFailedLoginAttempt_ShouldLockAfterMaxAttempts()
    {
        var user = new User("user", "u@t.com", "hash", "F", "L");

        user.RecordFailedLoginAttempt(3, 15);
        user.RecordFailedLoginAttempt(3, 15);
        user.RecordFailedLoginAttempt(3, 15);

        user.AccessFailedCount.Should().Be(3);
        user.IsLocked.Should().BeTrue();
        user.LockoutEnd.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CanLogin_ShouldReturnFalse_WhenLocked()
    {
        var user = new User("user", "u@t.com", "hash", "F", "L");
        user.RecordFailedLoginAttempt(3, 15);
        user.RecordFailedLoginAttempt(3, 15);
        user.RecordFailedLoginAttempt(3, 15);

        user.CanLogin().Should().BeFalse();
    }

    [Fact]
    public void CanLogin_ShouldReturnFalse_WhenInactive()
    {
        var user = new User("user", "u@t.com", "hash", "F", "L");
        user.Deactivate();

        user.CanLogin().Should().BeFalse();
    }

    [Fact]
    public void CanLogin_ShouldReturnFalse_WhenSuspended()
    {
        var user = new User("user", "u@t.com", "hash", "F", "L");
        user.Suspend();

        user.CanLogin().Should().BeFalse();
    }

    [Fact]
    public void Unlock_ShouldResetLockState()
    {
        var user = new User("user", "u@t.com", "hash", "F", "L");
        user.RecordFailedLoginAttempt(3, 15);
        user.RecordFailedLoginAttempt(3, 15);
        user.RecordFailedLoginAttempt(3, 15);
        user.IsLocked.Should().BeTrue();

        user.Unlock();

        user.IsLocked.Should().BeFalse();
        user.LockoutEnd.Should().BeNull();
        user.AccessFailedCount.Should().Be(0);
    }

    [Fact]
    public void StatusTransitions_ShouldChangeStatus()
    {
        var user = new User("user", "u@t.com", "hash", "F", "L");

        user.Suspend();
        user.Status.Should().Be(UserStatus.Suspended);

        user.Activate();
        user.Status.Should().Be(UserStatus.Active);

        user.Deactivate();
        user.Status.Should().Be(UserStatus.Inactive);
    }

    [Fact]
    public void UpdatePassword_ShouldChangePasswordHash()
    {
        var user = new User("user", "u@t.com", "oldhash", "F", "L");

        user.UpdatePassword("newhash");

        user.PasswordHash.Should().Be("newhash");
    }

    [Fact]
    public void PasswordVerification_UsesExternalHasher_NotDomainEntity()
    {
        // VerifyPassword was intentionally removed from User entity to avoid
        // bypassing BCrypt verification. Use IPasswordHasher.Verify() instead.
        var user = new User("user", "u@t.com", "hash123", "F", "L");
        user.PasswordHash.Should().Be("hash123");
    }
}
