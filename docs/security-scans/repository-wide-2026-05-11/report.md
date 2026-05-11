# Repository-Wide Security Code Review Report

## Summary

- Date: 2026-05-11
- Repository: `AuthPlatform`
- Branch: `code-review`
- Scope: full project source review of `src/**`
- Workflow: threat model, runtime inventory, repository-wide discovery, validation, attack-path analysis
- Result: 5 reportable findings

Build validation was attempted with `dotnet build AuthPlatform.slnx`, but NuGet restore was blocked by sandbox network restrictions (`NU1301`, socket forbidden). Findings below are validated by static source tracing and repository evidence.

## Findings

## Finding: Fixed seeded admin credentials and default API key

- Priority: P0
- Severity: critical
- Confidence: high
- CWE: CWE-798 Use of Hard-coded Credentials
- Affected lines: `src/Auth.Api/Extensions/ApplicationBuilderExtensions.cs:26-27`, `src/Auth.Persistence/Seed/ApplicationDbContextSeed.cs:68-73`, `src/Auth.Persistence/Seed/ApplicationDbContextSeed.cs:181-194`

### Summary

The application automatically runs database seeding during startup and inserts fixed credentials into an empty database: `admin@authplatform.com` / `Admin@123`, `viewer@authplatform.com` / `Viewer@123`, and an application API key of `default-api-key`. A fresh or reset deployment can therefore expose a known administrator login.

### Validation

`InitializeDatabaseAsync` calls `ApplicationDbContextSeed.SeedAsync` at startup. The seed data is not gated by environment, external secret injection, random bootstrap generation, or forced rotation. The seeded admin role receives all permissions.

### Reachability Analysis

An unauthenticated attacker can use the normal login endpoint. If the database is empty or reseeded, the known admin credential produces a valid JWT and crosses directly into the identity control plane.

### Attack Path

1. Deploy or reset the service with an empty database.
2. Startup seeds the fixed admin user and default API key.
3. Attacker logs in as `admin@authplatform.com` using the known password.
4. Attacker receives admin permissions and can access administrative APIs.

### Severity Analysis

This is critical because it can provide immediate administrative compromise in realistic fresh deployment states. The strongest counterevidence would be a production-only bootstrap secret or forced password rotation; none was found.

### Remediation

Remove fixed production seed credentials. Generate one-time bootstrap secrets from operator-provided environment variables or an installation flow, require immediate rotation, and fail startup in production if secure bootstrap values are absent.

## Finding: Privileged endpoints require only authentication, not permissions

- Priority: P0
- Severity: critical
- Confidence: high
- CWE: CWE-862 Missing Authorization, CWE-285 Improper Authorization
- Affected lines: `src/Auth.Api/Controllers/UsersController.cs:12`, `src/Auth.Api/Controllers/RolesController.cs:12`, `src/Auth.Api/Controllers/PermissionsController.cs:11`, `src/Auth.Api/Controllers/ApplicationsController.cs:11`, `src/Auth.Api/Controllers/TenantsController.cs:11`, `src/Auth.Api/Controllers/PermissionsController.cs:146-167`, `src/Auth.Api/Controllers/PermissionsController.cs:207-231`, `src/Auth.Api/Controllers/RolesController.cs:166-187`, `src/Auth.Api/Controllers/ApplicationsController.cs:157-172`, `src/Auth.Api/Controllers/TenantsController.cs:109-119`

### Summary

The project defines permission-based authorization infrastructure, but privileged controllers only use class-level `[Authorize]`. Any authenticated user can reach high-impact actions such as granting permissions directly to users, assigning roles, rotating application API keys, creating/deleting users, and updating tenant connection strings.

### Validation

Code search found `PermissionPolicyProvider` and `PermissionAuthorizationHandler`, but no controller use of `Permission:*` policies, `RequireAuthorization`, `IAuthorizationService.AuthorizeAsync`, `User.IsInRole`, or direct permission checks. The mutating actions proceed directly from an authenticated request to EF Core writes.

### Reachability Analysis

The affected endpoints are normal authenticated HTTP API routes. A low-privilege account, including the seeded viewer account, satisfies `[Authorize]` and can invoke the same mutation code paths.

### Attack Path

1. Attacker authenticates as any user.
2. Attacker calls `POST /api/permissions/users/{userId}/{permissionId}` or `POST /api/roles/{roleId}/users/{userId}`.
3. The action writes a new `UserPermission` or `UserRole` without checking whether the caller has `permissions.assign` or role-management rights.
4. Attacker escalates privileges or modifies identity control-plane state.

### Severity Analysis

This is critical because it allows direct privilege escalation and authorization-state compromise by any authenticated user. It is not merely a missing defense-in-depth check; the code already models fine-grained permissions and then does not enforce them on the endpoints that modify those permissions.

### Remediation

Apply action-level policies such as `[Authorize(Policy = "Permission:permissions.assign")]`, `[Authorize(Policy = "Permission:roles.update")]`, `[Authorize(Policy = "Permission:applications.manage")]`, and `[Authorize(Policy = "Permission:tenants.manage")]`. Add negative authorization tests for viewer/low-privilege users on every privileged mutation.

## Finding: Tenant and object isolation are not enforced

- Priority: P1
- Severity: high
- Confidence: high
- CWE: CWE-639 Authorization Bypass Through User-Controlled Key, CWE-862 Missing Authorization
- Affected lines: `src/Auth.Api/Middleware/TenantResolutionMiddleware.cs:15-30`, `src/Auth.Infrastructure/Services/CurrentUserService.cs:44-45`, `src/Auth.Api/Controllers/UsersController.cs:39-57`, `src/Auth.Api/Controllers/UsersController.cs:76-92`, `src/Auth.Api/Controllers/TenantsController.cs:30-43`, `src/Auth.Api/Controllers/ApplicationsController.cs:30-44`

### Summary

The codebase is tenant-aware: users have `TenantId`, JWTs can contain a tenant claim, and middleware resolves `X-Tenant` or subdomain into `TenantSlug`. The controllers do not use that tenant context when reading or mutating users, tenants, applications, roles, or permissions. Authenticated callers can enumerate global data or select arbitrary object IDs.

### Validation

The user list query starts from `_context.Users` without a tenant filter, `GetUser` uses only the route ID, and tenant/application list queries read global DbSets. No `HasQueryFilter`, tenant service implementation, or controller-level tenant comparison was found.

### Reachability Analysis

Authenticated HTTP requests can supply arbitrary route IDs and pagination values. The current user's tenant claim and resolved tenant slug are not consulted before returning data.

### Attack Path

1. Attacker authenticates as a user from one tenant or application context.
2. Attacker calls list endpoints or guesses GUIDs for other users/tenants/applications.
3. The controller queries global tables without tenant scoping.
4. Attacker reads or mutates objects outside their boundary.

### Severity Analysis

High severity is appropriate because the repository advertises multi-tenant readiness and stores tenant identity state, but the data-access controls do not enforce that boundary. Impact includes cross-tenant PII exposure and unauthorized control-plane changes when combined with privileged endpoint gaps.

### Remediation

Implement a tenant service backed by validated JWT tenant claims and resolved tenant slug. Add global query filters or explicit `Where` predicates for tenant-owned records, and verify target object tenant/application ownership before reads and mutations.

## Finding: Login throttling is documented but not enforced

- Priority: P2
- Severity: medium
- Confidence: high
- CWE: CWE-307 Improper Restriction of Excessive Authentication Attempts
- Affected lines: `src/Auth.Api/Controllers/AuthController.cs:26-38`, `src/Auth.Application/Features/Auth/Commands/LoginCommand.cs:41-48`, `src/Auth.Api/appsettings.json:31-34`

### Summary

The configuration and README describe rate limiting, but no rate-limiter middleware or service is wired into the application. Login lockout only increments after an existing user is found and password verification fails. Unknown-user attempts return immediately without any throttle or lockout state update.

### Validation

Search found `RateLimiting` only in configuration and README content. No `AddRateLimiter`, `UseRateLimiter`, custom throttling service, or cache-backed attempt tracking was found in runtime code.

### Reachability Analysis

The login endpoint is anonymous by design. Attackers can repeatedly submit credential guesses and password-spray username lists. Known accounts may eventually lock, but broad IP/user-agent/client throttling is absent.

### Attack Path

1. Attacker sends repeated `POST /api/auth/login` requests.
2. Unknown emails return without attempt accounting.
3. Known-account wrong passwords increment lockout only on that user.
4. Password spraying remains feasible across many accounts and IPs.

### Severity Analysis

Medium severity: this does not directly bypass authentication, but it materially weakens the authentication boundary and contradicts the documented security posture.

### Remediation

Add ASP.NET Core rate limiting or a dedicated login-attempt service keyed by IP, normalized email, tenant/application, and device signal. Apply it before password verification and cover unknown-user attempts with indistinguishable throttling behavior.

## Finding: JWT bearer tokens are accepted in query strings for all endpoints

- Priority: P2
- Severity: medium
- Confidence: medium-high
- CWE: CWE-598 Use of GET Request Method With Sensitive Query Strings
- Affected lines: `src/Auth.Api/Extensions/ServiceCollectionExtensions.cs:87-95`

### Summary

The JWT bearer handler reads `access_token` from the query string for every request path. Query-string tokens are commonly captured in logs, browser history, reverse proxies, analytics tools, and referrers. This should normally be restricted to endpoints that require it, such as WebSocket or server-sent event connections.

### Validation

The `OnMessageReceived` hook assigns `context.Token` whenever `context.Request.Query["access_token"]` is non-empty. No path restriction or transport-specific condition was found.

### Reachability Analysis

Any client can place a bearer token in a URL. Once accepted, the URL becomes equivalent to an Authorization header for the lifetime of the token and may be replayed by anyone who obtains it from logs.

### Attack Path

1. A valid token is placed in a URL as `?access_token=...`.
2. Middleware accepts it globally.
3. The URL is stored or leaked through infrastructure logs/history/referrer paths.
4. Another party replays the token before it expires.

### Severity Analysis

Medium severity because exploitation depends on token leakage through logging or URL handling, but the implementation expands token exposure across all API paths.

### Remediation

Remove query-string token support unless required. If WebSocket/SSE needs it, restrict extraction to specific paths and add log redaction for `access_token`.

## Coverage Closure

- SQL injection: suppressed. Runtime data access uses EF LINQ and no raw SQL sinks were found.
- RCE/process execution: not applicable. No attacker-controlled process/eval sink found.
- SSRF/network callback abuse: not applicable. No attacker-controlled outbound HTTP sink found.
- Path traversal/file read: suppressed. File reads are operator-controlled JWT key paths or static XML doc paths.
- XML/deserialization/SSTI: not applicable. No relevant runtime parser/template/deserializer sink found.
- Dependency advisory audit: deferred. No CVE/GHSA/package seed was provided, and package restore/build was blocked by sandboxed network access.

## Artifacts

- Threat model: `docs/security-scans/repository-wide-2026-05-11/artifacts/threat_model.md`
- Runtime inventory: `docs/security-scans/repository-wide-2026-05-11/artifacts/runtime_inventory.md`
- File checklist: `docs/security-scans/repository-wide-2026-05-11/artifacts/exhaustive-file-checklist.md`
- Coverage ledger: `docs/security-scans/repository-wide-2026-05-11/artifacts/repository_coverage_ledger.md`
- Discovery report: `docs/security-scans/repository-wide-2026-05-11/artifacts/finding_discovery_report.md`
- Validation report: `docs/security-scans/repository-wide-2026-05-11/artifacts/validation_report.md`
- Attack-path analysis: `docs/security-scans/repository-wide-2026-05-11/artifacts/attack_path_analysis_report.md`
