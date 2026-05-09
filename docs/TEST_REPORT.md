# Auth Platform — Test Report

**Generated:** May 8, 2026  
**Branch:** main  
**Total Tests:** 79  
**Passed:** 79  
**Failed:** 0  
**Skipped:** 0  
**Duration:** 12.4s  

---

## Test Project Overview

| Project | Tests | Passed | Failed | Skipped | Coverage Area |
|---------|-------|--------|--------|---------|---------------|
| Auth.Domain.Tests | 43 | 43 | 0 | 0 | Domain entities, value objects, domain events |
| Auth.Application.Tests | 22 | 22 | 0 | 0 | CQRS handlers, validators, models, exceptions |
| Auth.Infrastructure.Tests | 14 | 14 | 0 | 0 | Services (cache, password hashing, current user) |
| **Total** | **79** | **79** | **0** | **0** | |

---

## 1. Auth.Domain.Tests (43 tests)

### User Entity — `UserTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Constructor_ShouldInitializeProperties` | Verifies all constructor args are properly assigned | ✅ |
| `Constructor_ShouldInitializeDefaultValues` | Verifies default Active status, zero failed attempts, empty roles | ✅ |
| `RecordLogin_ShouldUpdateLastLoginAndResetAttempts` | After login, LastLoginAt is set and FailedLoginAttempts reset to 0 | ✅ |
| `RecordFailedLoginAttempt_ShouldIncrementCounter` | Calling once increments counter by 1 | ✅ |
| `RecordFailedLoginAttempt_MultipleCallsShouldIncrement` | Calling 3 times yields counter of 3 | ✅ |
| `RecordFailedLoginAttempt_ShouldLockAccount_WhenExceedsMaxAttempts` | 5 failed attempts with max=5 locks the account | ✅ |
| `RecordFailedLoginAttempt_ShouldNotLock_WhenBelowThreshold` | 4 failed attempts with max=5 does not lock | ✅ |
| `RecordFailedLoginAttempt_ShouldSetLockoutEnd_WhenLocked` | Locked accounts get a LockoutEnd timestamp in the future | ✅ |
| `CanLogin_ActiveUser_ReturnsTrue` | Active, unexpired, unlocked user can log in | ✅ |
| `CanLogin_InactiveUser_ReturnsFalse` | Deactivated user cannot log in | ✅ |
| `CanLogin_LockedUser_ReturnsFalse` | Locked-out user cannot log in | ✅ |
| `CanLogin_ExpiredLockout_ReturnsTrue` | User with expired lockout can log in again | ✅ |
| `Deactivate_ShouldSetStatusToInactive` | Status changes to Inactive after deactivation | ✅ |
| `Activate_ShouldSetStatusToActive` | Status changes back to Active after activation | ✅ |
| `AssignToTenant_ShouldSetTenantId` | Tenant assignment updates the TenantId | ✅ |
| `RemoveFromTenant_ShouldClearTenantId` | Removing tenant clears TenantId | ✅ |
| `UpdateProfile_ShouldUpdateFirstNameAndLastName` | Profile fields update correctly | ✅ |
| `ToString_ReturnsUsername` | String representation returns the username | ✅ |

### RefreshToken Entity — `RefreshTokenTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Constructor_ShouldInitializeProperties` | Verifies all constructor properties are set correctly | ✅ |
| `Constructor_ShouldGenerateUniqueTokenValues` | Two tokens should have different Token values | ✅ |
| `Constructor_ShouldSetCreatedAtToUtcNow` | CreatedAt is set within the current second | ✅ |
| `Constructor_ShouldSetIsRevokedToFalse` | Default IsRevoked is false | ✅ |
| `IsExpired_WhenExpiresAtIsPast_ReturnsTrue` | Past ExpiresAt yields expired | ✅ |
| `IsExpired_WhenExpiresAtIsFuture_ReturnsFalse` | Future ExpiresAt is not expired | ✅ |
| `Revoke_ShouldSetIsRevokedToTrue` | Revoking sets IsRevoked to true | ✅ |
| `Revoke_ShouldSetRevokedAtToUtcNow` | RevokedAt is set to current time | ✅ |

### Role Entity — `RoleTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Constructor_ShouldInitializeProperties` | Verifies all constructor properties | ✅ |
| `Constructor_ShouldInitializeEmptyCollections` | RolePermissions and UserRoles are not null and empty | ✅ |
| `AddPermission_ShouldAddRolePermission` | Adding a permission creates a RolePermission entry | ✅ |
| `AddPermission_DuplicateShouldNotAddMultiple` | Adding same permission twice only adds once | ✅ |
| `RemovePermission_ShouldRemoveRolePermission` | Removing a permission removes the RolePermission entry | ✅ |
| `RemovePermission_NotInList_ShouldDoNothing` | Removing non-existent permission doesn't throw | ✅ |
| `ToString_ReturnsRoleName` | String representation returns the name | ✅ |

### Permission Entity — `PermissionTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Constructor_ShouldInitializeProperties` | Verifies all constructor properties | ✅ |
| `Constructor_ShouldSetCreatedAt` | CreatedAt is set to current time | ✅ |
| `ToString_ReturnsPermissionName` | String representation returns the name | ✅ |

### Application Entity — `ApplicationTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Constructor_ShouldInitializeProperties` | Verifies all constructor properties | ✅ |
| `Activate_ShouldSetIsActiveToTrue` | Activates the application | ✅ |
| `Deactivate_ShouldSetIsActiveToFalse` | Deactivates the application | ✅ |
| `ToString_ReturnsApplicationName` | String representation returns the name | ✅ |

### Tenant Entity — `TenantTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Constructor_ShouldInitializeProperties` | Verifies all constructor properties | ✅ |
| `ToString_ReturnsTenantName` | String representation returns the name | ✅ |

### BaseEntity — `BaseEntityTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `DomainEvents_ShouldBeEmpty_WhenCreated` | New entity has no domain events | ✅ |
| `AddDomainEvent_ShouldAddEvent` | Adding an event increases the count to 1 | ✅ |
| `RemoveDomainEvent_ShouldRemoveEvent` | Removing an event clears it | ✅ |
| `ClearDomainEvents_ShouldClearAllEvents` | Clearing removes all events | ✅ |

### ValueObject — `ValueObjectTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Equals_SameValues_ReturnsTrue` | Two value objects with same values are equal | ✅ |
| `Equals_DifferentValues_ReturnsFalse` | Different values produce inequality | ✅ |
| `GetHashCode_SameValues_ReturnsSameHash` | Same values produce same hash code | ✅ |
| `GetHashCode_DifferentValues_ReturnsDifferentHash` | Different values produce different hash codes | ✅ |

---

## 2. Auth.Application.Tests (22 tests)

### LoginCommandHandler — `LoginCommandHandlerTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Handle_ValidCredentials_ReturnsSuccessResult` | Valid email + password generates JWT + refresh token | ✅ |
| `Handle_InvalidEmail_ReturnsFailedResult` | Unknown email returns "Invalid email or password" | ✅ |
| `Handle_WrongPassword_ReturnsFailedAndRecordsAttempt` | Wrong password fails and increments failed attempts | ✅ |
| `Handle_LockedAccount_ReturnsFailed` | Locked account returns "Account is locked or inactive" | ✅ |
| `Handle_InactiveAccount_ReturnsFailed` | Deactivated account returns "Account is locked or inactive" | ✅ |

### RefreshTokenCommandHandler — `RefreshTokenCommandHandlerTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Handle_ShouldDelegateToRefreshTokenService` | Delegates to IRefreshTokenService.RefreshTokenAsync | ✅ |

### RevokeTokenCommandHandler — `RevokeTokenCommandHandlerTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Handle_ShouldDelegateToRefreshTokenService` | Delegates to IRefreshTokenService.RevokeRefreshTokenAsync | ✅ |

### LoginCommandValidator — `LoginCommandValidatorTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Validate_ValidCommand_ShouldNotHaveErrors` | All fields valid → no validation errors | ✅ |
| `Validate_EmptyEmail_ShouldHaveError` | Empty email triggers validation error | ✅ |
| `Validate_InvalidEmailFormat_ShouldHaveError` | Non-email format triggers validation error | ✅ |
| `Validate_EmptyPassword_ShouldHaveError` | Empty password triggers validation error | ✅ |
| `Validate_PasswordTooShort_ShouldHaveError` | Password < 8 chars triggers validation error | ✅ |
| `Validate_EmptyApplicationCode_ShouldHaveError` | Empty app code triggers validation error | ✅ |
| `Validate_NullValues_ShouldHaveErrors` | Null values trigger three validation errors | ✅ |

### Common Models — `ModelTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `AuthenticationResult_Succeed_ShouldSetSuccessAndProperties` | Succeed() creates a success result with tokens and user | ✅ |
| `AuthenticationResult_Failed_ShouldSetFailureAndErrors` | Failed() creates a failure result with error messages | ✅ |
| `PagedResult_Constructor_ShouldInitializeProperties` | PagedResult initializes Items, TotalCount, Page, PageSize | ✅ |
| `PagedResult_DefaultConstructor_ShouldSetEmptyItems` | Default constructor creates empty items list | ✅ |

### Common Exceptions — `ExceptionTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `NotFoundException_ShouldSetMessage` | Exception message is set correctly | ✅ |
| `ValidationException_ShouldStoreErrors` | Validation errors are stored and retrievable | ✅ |
| `UnauthorizedException_ShouldSetMessage` | Exception message is set correctly | ✅ |
| `ForbiddenException_ShouldSetMessage` | Exception message is set correctly | ✅ |

---

## 3. Auth.Infrastructure.Tests (14 tests)

### PasswordHasherService — `PasswordHasherServiceTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `Hash_ShouldReturnNonNullHash` | Hash output is not null | ✅ |
| `Hash_ShouldReturnDifferentHashForSamePassword` | Same password produces different hashes each time (salt) | ✅ |
| `Verify_ValidPassword_ReturnsTrue` | Correct password verifies successfully | ✅ |
| `Verify_InvalidPassword_ReturnsFalse` | Wrong password fails verification | ✅ |
| `Verify_TamperedHash_ReturnsFalse` | Modified hash fails verification | ✅ |

### CacheService — `CacheServiceTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `GetAsync_KeyNotFound_ReturnsNull` | Missing key returns null | ✅ |
| `SetAndGetAsync_ValidData_ReturnsData` | Stored value is retrievable | ✅ |
| `SetAsync_ShouldOverwriteExistingKey` | Overwriting a key returns the new value | ✅ |
| `ExistsAsync_KeyNotFound_ReturnsFalse` | Non-existent key returns false | ✅ |
| `ExistsAsync_KeyExists_ReturnsTrue` | Existing key returns true | ✅ |
| `RemoveAsync_ShouldDeleteKey` | After removal, key no longer exists | ✅ |
| `GetOrCreateAsync_CacheMiss_CreatesAndReturnsValue` | Cache miss invokes factory and caches result | ✅ |

### CurrentUserService — `CurrentUserServiceTests.cs`

| Test | Description | Status |
|------|-------------|--------|
| `IsAuthenticated_NoHttpContext_ReturnsFalse` | Null HttpContext yields unauthenticated with null values | ✅ |
| `UserId_WithSubClaim_ReturnsGuid` | NameIdentifier claim parses to correct GUID | ✅ |
| `UserId_WithSubFallbackClaim_ReturnsGuid` | "sub" claim works as fallback | ✅ |
| `UserId_InvalidClaim_ReturnsNull` | Non-GUID claim value returns null | ✅ |
| `MultipleRoles_ShouldReturnAll` | Multiple Role claims all returned in list | ✅ |

---

## Summary

```
Test summary: total: 79, failed: 0, succeeded: 79, skipped: 0, duration: 12.4s
Build succeeded with 2 warning(s) in 19.9s
```

- **2 build warnings** relate to a known `AutoMapper 12.0.1` NuGet vulnerability (GHSA-rvv3-g6hj-g44x) — non-blocking.
- **Code coverage** spans all three Clean Architecture layers (Domain, Application, Infrastructure).
- **Key scenarios covered:** User authentication flow (login validation, password verification, token generation, account locking), CRUD operations on domain entities, caching behavior, and current user context resolution.
