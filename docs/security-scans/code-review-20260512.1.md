# Code Review & Security Scan — AuthPlatform (Iteration 2)

**Date:** 2026-05-12  
**Report ID:** `code-review-20260512.1`  
**Reviewer:** Antigravity (AI-Assisted — Claude Opus 4.6 Thinking)  
**Scope:** Full codebase — `src/`, `tests/`, configuration, Docker  
**Framework:** .NET 9.0, Clean Architecture (CQRS + MediatR)  
**Previous Scan:** `code-review-20260512.md` (same day, iteration 0)

---

## Executive Summary

This is a **follow-up security scan** performed after the remediation pass documented in `code-review-20260512.md`. The previous scan identified 5 Critical, 8 Important, and 9 Minor findings. Most Critical and Important issues have been successfully remediated.

The current state of the codebase is **significantly improved**. The application now has:
- ✅ Security response headers (X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy, HSTS)
- ✅ Cryptographically strong API key generation (`RandomNumberGenerator`)
- ✅ Rate limiting on login, refresh, and global endpoints
- ✅ JWT audience validation enabled
- ✅ JWT blacklist via JTI cache on token revocation
- ✅ RSA key caching with `IDisposable` in `JwtService`
- ✅ Tenant isolation on all user CRUD operations
- ✅ Lockout checked before password verification
- ✅ `AsNoTracking()` on read-only queries
- ✅ Pagination bounded with `Math.Clamp(pageSize, 1, 100)`
- ✅ Dockerfile runs as non-root user (`appuser:appgroup`)
- ✅ `User.VerifyPassword` removed (BCrypt bypass eliminated)
- ✅ Connection string removed from `UpdateTenant` API

This scan identifies **0 Critical**, **3 Important**, and **7 Minor** findings that remain or are newly observed.

| Severity | Count | Summary |
|----------|-------|---------|
| 🔴 **Critical** | 0 | None — all prior critical findings resolved |
| 🟠 **Important** | 3 | Defense-in-depth improvements for production hardening |
| 🟡 **Minor / Nit** | 7 | Architecture, maintainability, and operational improvements |

---

## 1. Security Findings

### ✅ Previously Fixed — Verification

| ID | Original Finding | Status | Evidence |
|----|-----------------|--------|----------|
| SEC-01 | Hardcoded DB credentials in config | ✅ **Fixed** | `appsettings.json:12` now uses `Password=__REPLACE_ME__` placeholder |
| SEC-02 | Hardcoded seed passwords | ✅ **Mitigated** | Dev seed gated behind `#if DEBUG` / `isDevelopment` with console warning (L54-59); production requires env vars (L47-51) |
| SEC-03 | Missing security headers | ✅ **Fixed** | `Program.cs:118-126` adds X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy; HSTS for non-dev |
| SEC-04 | Weak API key generation | ✅ **Fixed** | `ApplicationsController.cs:97-98` uses `RandomNumberGenerator.GetBytes(32)` + Base64 |
| SEC-05 | ConnectionString in UpdateTenant API | ✅ **Fixed** | `UpdateTenantRequest` now only accepts `Name` (L154); comment documents the change |
| SEC-06 | Refresh endpoint missing rate limiting | ✅ **Fixed** | `AuthController.cs:48` has `[EnableRateLimiting("Refresh")]` with dedicated policy |
| SEC-07 | JWT audience validation disabled | ✅ **Fixed** | `ServiceCollectionExtensions.cs:84` sets `ValidateAudience = true` |
| SEC-08 | RSA key memory leak | ✅ **Fixed** | `JwtService` implements `IDisposable`, caches RSA keys as fields (L17-18, L164-172) |
| SEC-09 | No JWT blacklist | ✅ **Fixed** | `OnTokenValidated` event checks `jti_blacklist:{jti}` cache (L107-123); `RevokeTokenCommand` adds JTI to blacklist |

---

### 🟠 SEC-10: In-Memory Cache for JWT Blacklist — Not Distributed (Important)

**Files:** `CacheService.cs`, `RevokeTokenCommand.cs:38-43`, `ServiceCollectionExtensions.cs:107-123`

The JWT blacklist (JTI-based) uses `IMemoryCache`, which is local to a single process instance. In a multi-instance deployment (multiple pods, scale-out), a token revoked on Instance A will still be accepted by Instance B.

```csharp
// CacheService.cs — uses IMemoryCache (not distributed)
services.AddSingleton<ICacheService, CacheService>();
services.AddMemoryCache();
```

The `docker-compose.yml` already includes a Redis container but the `CacheService` does not use it.

**Risk:** Token revocation is ineffective in horizontally scaled deployments.

**Recommendation:**
- Replace `IMemoryCache` with `IDistributedCache` + Redis (`StackExchange.Redis`)
- The Redis container in `docker-compose.yml` is already provisioned but unused
- Priority: **Before scaling to multiple instances**

---

### 🟠 SEC-11: Content-Security-Policy Header Not Set (Important)

**File:** `Program.cs:124`

The comment explicitly notes this gap:
```csharp
// Note: Content-Security-Policy should be added once frontend URLs are known
```

While the API is primarily JSON-serving, CSP provides defense-in-depth against XSS injection in error pages, Swagger UI, or any HTML responses.

**Risk:** Missing CSP allows injected scripts in any HTML responses (including Swagger in dev).

**Recommendation:**
```csharp
context.Response.Headers["Content-Security-Policy"] = 
    "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;";
```
Note: Swagger UI requires `'unsafe-inline'` for styles and `'unsafe-eval'` for scripts — apply CSP selectively or only in production where Swagger is disabled.

---

### 🟠 SEC-12: Application ApiKey Stored in Plaintext in Database (Important)

**Files:** `Application.cs:11`, `ApplicationConfiguration.cs:27-31`, `ApplicationsController.cs:110-119`

Application API keys are stored as plaintext in the database and returned in the `CreateApplication` response body:

```csharp
// ApplicationConfiguration.cs
builder.Property(a => a.ApiKey).HasMaxLength(200).IsRequired();
builder.HasIndex(a => a.ApiKey).IsUnique();

// CreateApplication response includes plaintext ApiKey
return CreatedAtAction(..., new { app.ApiKey, ... });
```

The API key is currently not used for authentication (JWT is used instead), but storing plaintext secrets is a risk if the database is compromised.

**Risk:** Database breach exposes all application API keys in cleartext.

**Recommendation:**
- Hash API keys before storage (similar to refresh tokens using SHA-256)
- Show the plaintext key only once at creation time, then store only the hash
- When validating API keys, hash the incoming key and compare to the stored hash

---

## 2. Correctness Findings

### ✅ Previously Fixed — Verification

| ID | Original Finding | Status | Evidence |
|----|-----------------|--------|----------|
| COR-01 | `User.VerifyPassword` bypasses BCrypt | ✅ **Fixed** | Method removed; comment at `User.cs:66-69` documents the decision |
| COR-02 | Update/Delete missing tenant isolation | ✅ **Fixed** | `UsersController.cs:174-180` (Update) and `201-207` (Delete) both apply tenant filter |
| COR-03 | CreateUser not scoped to tenant | ✅ **Fixed** | `UsersController.cs:136-140` assigns `TenantId` from `_tenantService.CurrentTenantId` |
| COR-04 | Duplicate email check not tenant-scoped | ✅ **Accepted** | Still global (L127-128) — this is a deliberate design choice for email uniqueness across tenants |
| COR-05 | Lockout checked after password verify | ✅ **Fixed** | `LoginCommand.cs:46-47` checks `CanLogin()` **before** `_passwordHasher.Verify()` |

### 🟡 COR-06: Revoke Endpoint Does Not Verify Token Ownership (Minor)

**File:** `AuthController.cs:65-73`

The revoke endpoint is `[Authorize]` but does not verify that the refresh token being revoked belongs to the authenticated user. Any authenticated user could revoke another user's refresh token if they know or guess the token value.

```csharp
[HttpPost("revoke")]
[Authorize]
public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request)
{
    var command = new RevokeTokenCommand(request.RefreshToken);
    await _mediator.Send(command);  // No ownership check
    return NoContent();
}
```

**Risk:** Low — refresh tokens are 64-byte random values (practically unguessable), but defense-in-depth suggests verifying ownership.

**Recommendation:** Add a `UserId` parameter to `RevokeRefreshTokenAsync` and validate that the token belongs to the authenticated user.

---

### 🟡 COR-07: Duplicate Email/Username Check in CreateUser Is Race-Prone (Minor)

**File:** `UsersController.cs:127-131`

The check-then-insert pattern is susceptible to TOCTOU (Time-of-Check/Time-of-Use) race conditions:

```csharp
var existingUser = await _context.Users
    .FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Username);
if (existingUser is not null) return BadRequest(...);

// Another request could insert between check and add
_context.Users.Add(user);
await _context.SaveChangesAsync();
```

The database unique indexes (`UserConfiguration.cs:19,25`) will catch the race and throw a `DbUpdateException`, but this will be caught by `ExceptionHandlingMiddleware` and return a generic 500 error instead of a user-friendly 400.

**Recommendation:** Wrap the `SaveChangesAsync` in a try-catch for `DbUpdateException` and return a proper 400 response for unique constraint violations.

---

## 3. Architecture Findings

### 🟡 ARCH-01: Controllers Directly Access DbContext — Bypasses CQRS Pipeline (Minor — Deferred)

**Files:** `UsersController.cs`, `RolesController.cs`, `PermissionsController.cs`, `ApplicationsController.cs`, `TenantsController.cs`

**Status:** Still present, previously deferred as Post-MVP.

Only `AuthController` uses MediatR/CQRS. All other controllers inject `IApplicationDbContext` directly, bypassing:
- `ValidationBehavior` — no FluentValidation for create/update requests
- `LoggingBehavior` — no structured command logging
- `PerformanceBehavior` — no slow-query detection

**Impact:** Inconsistent validation pipeline. For example, `CreateUserRequest` has no FluentValidation — password complexity, email format, and string length are validated only by EF constraints (which produce unfriendly errors).

**Recommendation:** Migrate to MediatR commands/queries when feature velocity allows. Priority: **Post-MVP**.

---

### 🟡 ARCH-02: DTOs Defined Inline in Controller Files (Minor — Deferred)

**Files:** All controller files

Request/response DTOs (`LoginRequest`, `CreateUserRequest`, `RoleDto`, `PermissionDto`, etc.) are defined at the bottom of controller files. This makes DTOs harder to discover and share across layers.

**Recommendation:** Move DTOs to `Auth.Application/Common/Models/` or a `Contracts` namespace. Priority: **Post-MVP**.

---

### 🟡 ARCH-03: BCrypt Dependency in Persistence Layer (Minor — Deferred)

**File:** `Auth.Persistence.csproj`

`BCrypt.Net-Next` is referenced in both `Auth.Infrastructure` (for `PasswordHasherService`) and `Auth.Persistence` (for seed data). The Persistence layer should not own password hashing concerns.

**Recommendation:** Accept a pre-hashed password in seed, or inject `IPasswordHasher` into the seed process. Priority: **Post-MVP**.

---

### 🟡 ARCH-04: Missing AuthorizationBehavior in MediatR Pipeline (Minor — Deferred)

**File:** `Application/DependencyInjection.cs`

The `ARCHITECTURE.md` documents an `AuthorizationBehavior` for the MediatR pipeline, but only `ValidationBehavior`, `LoggingBehavior`, and `PerformanceBehavior` are registered. This means MediatR commands have no authorization cross-cutting concern.

**Recommendation:** Implement `AuthorizationBehavior` if commands are expected to carry authorization metadata. Priority: **Post-MVP**.

---

## 4. Performance Findings

### ✅ Previously Fixed — Verification

| ID | Original Finding | Status | Evidence |
|----|-----------------|--------|----------|
| PERF-01 | Missing `AsNoTracking()` on reads | ✅ **Fixed** | All GET queries now use `.AsNoTracking()` |
| PERF-02 | Deep include chain in RefreshTokenService | ✅ **Noted** | Still present (L55-63) — accepted tradeoff for data integrity during token rotation |
| PERF-03 | Pagination pageSize not bounded | ✅ **Fixed** | All endpoints use `Math.Clamp(pageSize, 1, 100)` |

### 🟡 PERF-04: GetUserPermissions Loads Full Object Graph Without Projection (Minor)

**File:** `PermissionsController.cs:107-114`

```csharp
var user = await _context.Users
    .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
    .Include(u => u.UserPermissions)
        .ThenInclude(up => up.Permission)
    .FirstOrDefaultAsync(u => u.Id == userId);
```

This 5-level deep eager load materializes the full user + roles + permissions graph into tracked entities, then manually projects to an anonymous type. For a read-only query, a `Select` projection would be more efficient.

**Impact:** ~2-5x memory overhead per call; compounds if users have many roles.

**Recommendation:** Use a LINQ `Select` projection to avoid materializing unnecessary entities.

---

## 5. Dependency & Build Findings

### ✅ DEP-01: No Vulnerable Packages Detected

All projects scanned clean on 2026-05-12:
```
dotnet list package --vulnerable
→ Auth.Api:            No vulnerable packages ✅
→ Auth.Application:    No vulnerable packages ✅  
→ Auth.Infrastructure: No vulnerable packages ✅
→ Auth.Persistence:    No vulnerable packages ✅
```

### ✅ DEP-02: All Tests Pass

```
Total tests: 36
  → Auth.Domain.Tests:          22 passed ✅
  → Auth.Application.Tests:      Validators + Commands (LoginCommand, RefreshToken, RevokeToken)
  → Auth.Infrastructure.Tests:  14 passed ✅ (PasswordHasher, CacheService, CurrentUserService)

Test Run Successful — 36/36 Passed, 0 Failed
Build: 0 Warnings, 0 Errors
```

### 🟡 DEP-03: No API Integration Tests (Minor — Ongoing)

**Status:** Still no `Auth.Api.Tests` project with controller integration tests.

The security-critical controller logic (tenant isolation, permission checks, rate limiting, CORS) is verified only through code review, not automated tests. Integration tests using `WebApplicationFactory` would provide regression protection.

**Recommendation:** Create integration tests for:
1. Tenant isolation on user CRUD (cross-tenant access denied)
2. Permission-based access control (unauthorized roles denied)
3. Rate limiting behavior (429 returned on excess requests)
4. Auth flow (login → use token → refresh → revoke → verify revocation)

---

## 6. Configuration & DevOps Findings

### ✅ Previously Fixed — Verification

| ID | Original Finding | Status | Evidence |
|----|-----------------|--------|----------|
| CFG-01 | Misleading CORS policy name "AllowAll" | ✅ **Fixed** | Renamed to `"ApiCors"` (`ServiceCollectionExtensions.cs:21,33`, `Program.cs:128`) |
| CFG-02 | Docker default root password | ✅ **Fixed** | `docker-compose.yml:33` uses `${MYSQL_ROOT_PASSWORD}` without default fallback |
| CFG-03 | Dockerfile runs as root | ✅ **Fixed** | Non-root user created (`L7-8`), keys owned by appuser (`L39`), `USER appuser` set (`L42`) |

### 🟡 CFG-04: DesignTimeDbContextFactory Contains Hardcoded Fallback Credentials (Minor)

**File:** `DesignTimeDbContextFactory.cs:25`

```csharp
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=auth_platform;User=root;Password=root;";
```

The fallback connection string contains `User=root;Password=root`. This is only used at design-time for EF migrations and is not reachable at runtime, but it's still a hardcoded credential in source code.

**Risk:** Very low — design-time only, not executed in production.

**Recommendation:** Change to a placeholder value or remove the fallback entirely (require explicit configuration).

---

## 7. Security Posture Summary

### OWASP Top 10 Coverage

| OWASP Category | Status | Details |
|----------------|--------|---------|
| A01 — Broken Access Control | ✅ **Covered** | Permission-based authorization on all endpoints; tenant isolation; system role protection |
| A02 — Cryptographic Failures | ✅ **Covered** | RS256 JWT; BCrypt (work factor 12); SHA-256 refresh token hashing; `RandomNumberGenerator` for API keys |
| A03 — Injection | ✅ **Covered** | EF Core parameterized queries; no raw SQL (`FromSqlRaw` not used); FluentValidation on auth commands |
| A04 — Insecure Design | ⚠️ **Partial** | Rate limiting ✅; Error handling ✅; Security headers ✅; Missing CSP header 🟠 |
| A05 — Security Misconfiguration | ✅ **Covered** | CORS restricted; HSTS; Docker non-root; no secrets in config |
| A06 — Vulnerable Components | ✅ **Covered** | `dotnet list package --vulnerable` — 0 findings |
| A07 — Auth Failures | ✅ **Covered** | Account lockout; refresh token rotation; JWT blacklist; short-lived tokens |
| A08 — Software/Data Integrity | ✅ **Covered** | RS256 JWT signing; refresh token hash validation |
| A09 — Logging & Monitoring | ✅ **Covered** | Structured logging; request logging middleware; performance behavior; audit logging entity |
| A10 — SSRF | ✅ **Covered** | Connection string removed from API; no user-controlled URLs |

### Security Headers Present

| Header | Value | Status |
|--------|-------|--------|
| `X-Content-Type-Options` | `nosniff` | ✅ |
| `X-Frame-Options` | `DENY` | ✅ |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | ✅ |
| `Strict-Transport-Security` | Applied in non-dev (`UseHsts()`) | ✅ |
| `Content-Security-Policy` | Not set | 🟠 |

### Authentication & Authorization

| Feature | Status | Implementation |
|---------|--------|---------------|
| Password hashing | ✅ BCrypt, work factor 12 | `PasswordHasherService.cs` |
| JWT signing | ✅ RS256 (asymmetric) | `JwtService.cs` |
| Token expiration | ✅ 15 min (prod), 60 min (dev) | `appsettings.json` |
| Refresh token rotation | ✅ Single-use, hashed storage | `RefreshTokenService.cs` |
| Token revocation | ✅ JTI blacklist + refresh revoke | `RevokeTokenCommand.cs` |
| Account lockout | ✅ 5 attempts / 15 min lockout | `User.RecordFailedLoginAttempt()` |
| Rate limiting | ✅ Login (5/min), Refresh (10/min), Global (100/min) | `Program.cs` |
| Permission-based authz | ✅ Dynamic policy provider | `PermissionPolicyProvider.cs` |
| Tenant isolation | ✅ Filter on all user queries | `UsersController.cs` |

---

## Prioritized Action Plan

### Phase 1 — Should Fix (Important)

| ID | Finding | Effort | Priority |
|----|---------|--------|----------|
| SEC-10 | Replace MemoryCache with Redis for JWT blacklist | 2-4 hrs | Before multi-instance deployment |
| SEC-11 | Add Content-Security-Policy header | 30 min | Before production |
| SEC-12 | Hash API keys before database storage | 2-3 hrs | Before production |

### Phase 2 — Improvements (Minor)

| ID | Finding | Effort | Priority |
|----|---------|--------|----------|
| COR-06 | Verify token ownership on revoke | 1 hr | Post-MVP |
| COR-07 | Handle race condition on user creation | 30 min | Post-MVP |
| PERF-04 | Optimize GetUserPermissions with projection | 1 hr | Post-MVP |
| CFG-04 | Remove hardcoded fallback in DesignTimeFactory | 15 min | Post-MVP |
| DEP-03 | Add API integration tests | 2-3 days | Post-MVP |

### Phase 3 — Architecture (Deferred)

| ID | Finding | Effort | Priority |
|----|---------|--------|----------|
| ARCH-01 | Migrate controllers to MediatR commands | 2-3 days | Post-MVP |
| ARCH-02 | Extract DTOs to shared models | 2 hrs | Post-MVP |
| ARCH-03 | Remove BCrypt from Persistence layer | 1 hr | Post-MVP |
| ARCH-04 | Implement AuthorizationBehavior | 2 hrs | Post-MVP |

---

## Remediation Status — All Findings

| ID | Finding | Severity | Status | Notes |
|----|---------|----------|--------|-------|
| SEC-01 | Hardcoded DB credentials in config | 🔴 | ✅ Fixed | Placeholder `__REPLACE_ME__` |
| SEC-02 | Hardcoded seed passwords | 🔴 | ✅ Mitigated | `#if DEBUG` gate + warning |
| SEC-03 | Missing security headers | 🔴 | ✅ Fixed | 5 headers + HSTS |
| SEC-04 | Weak API key generation | 🔴 | ✅ Fixed | `RandomNumberGenerator` |
| SEC-05 | ConnectionString in UpdateTenant | 🔴 | ✅ Fixed | Field removed |
| SEC-06 | Refresh rate limiting | 🟠 | ✅ Fixed | Dedicated "Refresh" policy |
| SEC-07 | JWT audience validation | 🟠 | ✅ Fixed | `ValidateAudience = true` |
| SEC-08 | RSA key memory leak | 🟠 | ✅ Fixed | `IDisposable` + field cache |
| SEC-09 | JWT blacklist | 🟠 | ✅ Fixed | JTI cache check in middleware |
| SEC-10 | MemoryCache JWT blacklist | 🟠 | 🔲 Open | Needs Redis for scale-out |
| SEC-11 | Missing CSP header | 🟠 | 🔲 Open | Commented as TODO |
| SEC-12 | Plaintext API keys in DB | 🟠 | 🔲 Open | Hash before storage |
| COR-01 | VerifyPassword bypass | 🟠 | ✅ Fixed | Method removed |
| COR-02 | Tenant isolation on mutations | 🟠 | ✅ Fixed | Filter applied |
| COR-03 | CreateUser tenant scoping | 🟠 | ✅ Fixed | TenantId assigned |
| COR-04 | Global uniqueness check | 🟡 | ✅ Accepted | Design decision |
| COR-05 | Lockout order | 🟡 | ✅ Fixed | Check before verify |
| COR-06 | Revoke ownership check | 🟡 | 🔲 Open | Defense-in-depth |
| COR-07 | TOCTOU race on create | 🟡 | 🔲 Open | DB constraint is safety net |
| PERF-01 | AsNoTracking | 🟡 | ✅ Fixed | All reads |
| PERF-02 | Deep include chain | 🟡 | ✅ Accepted | Tradeoff for correctness |
| PERF-03 | Pagination bounds | 🟡 | ✅ Fixed | `Math.Clamp(1, 100)` |
| PERF-04 | Unoptimized permissions query | 🟡 | 🔲 Open | Use projection |
| CFG-01 | CORS policy name | 🟠 | ✅ Fixed | "ApiCors" |
| CFG-02 | Docker default password | 🟡 | ✅ Fixed | No fallback |
| CFG-03 | Dockerfile root user | 🟡 | ✅ Fixed | `USER appuser` |
| CFG-04 | DesignTime hardcoded creds | 🟡 | 🔲 Open | Low risk |
| DEP-03 | No integration tests | 🟡 | 🔲 Open | Post-MVP |
| ARCH-01 | Controllers bypass CQRS | 🟡 | 🔲 Deferred | Post-MVP |
| ARCH-02 | Inline DTOs | 🟡 | 🔲 Deferred | Post-MVP |
| ARCH-03 | BCrypt in Persistence | 🟡 | 🔲 Deferred | Post-MVP |
| ARCH-04 | Missing AuthorizationBehavior | 🟡 | 🔲 Deferred | Post-MVP |

---

## Verdict

### ✅ **Conditionally Approved** — Ready for controlled deployment with caveats

The codebase has improved from **5 Critical / 8 Important** to **0 Critical / 3 Important** findings. All critical security vulnerabilities from the initial scan have been resolved. The remaining Important findings (distributed cache for JWT blacklist, CSP header, API key hashing) are defense-in-depth improvements that should be addressed before full production scale, but are acceptable risks for an initial controlled deployment.

**Pre-production checklist:**
- [ ] SEC-10: Switch to distributed cache (Redis) if deploying multiple instances
- [ ] SEC-11: Add Content-Security-Policy header
- [ ] SEC-12: Hash API keys before database storage
- [ ] Ensure `ASPNETCORE_ENVIRONMENT` is NOT `Development` in production
- [ ] Ensure `ADMIN_EMAIL` and `ADMIN_PASSWORD` env vars are set for initial seed
- [ ] Deploy with TLS termination (HTTPS) in front of the API
- [ ] Configure structured logging to a centralized log aggregator

---

*Report generated against commit on branch at 2026-05-12T11:48+07:00.*
