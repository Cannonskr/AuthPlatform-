# Finding Discovery Report

## Scope

Repository-wide source-code security review of `src/**` for AuthPlatform. Tests, docs, generated cache files, and local developer artifacts were excluded from primary runtime scope unless they supported deployment or threat-model evidence.

## Discovery Summary

Five candidate finding families were promoted for validation:

1. Fixed seeded credentials and API key are installed during application startup.
2. Privileged user/role/permission/application/tenant endpoints require only authentication, not permission policies.
3. Tenant and object isolation controls exist in data model/middleware but are not enforced in controller queries.
4. Login throttling is documented/configured but not wired, and lockout only applies to existing-user wrong-password attempts.
5. JWT bearer tokens are accepted from query strings globally.

High-impact families searched and suppressed:

- SQL injection: no raw SQL sinks found.
- RCE/process execution: no process/eval sinks found.
- SSRF: no attacker-controlled outbound HTTP sink found.
- Path traversal/file read: only operator-controlled key/doc paths found.
- XML/deserialization/SSTI: no relevant runtime sink found.

## Candidate Details

### CAND-001: Fixed seeded admin credentials and application API key

- Affected locations: `src/Auth.Api/Extensions/ApplicationBuilderExtensions.cs:26`, `src/Auth.Persistence/Seed/ApplicationDbContextSeed.cs:73`, `src/Auth.Persistence/Seed/ApplicationDbContextSeed.cs:187`, `src/Auth.Persistence/Seed/ApplicationDbContextSeed.cs:194`
- Attacker-controlled source: anonymous login requests to `/api/auth/login`
- Broken control: production startup seed creates fixed known credentials and API key whenever the database is empty
- Impact: default admin login and known application API key after first deployment or reset
- CWE: CWE-798

### CAND-002: Privileged controllers do not enforce permission policies

- Affected locations: `src/Auth.Api/Controllers/UsersController.cs:12`, `src/Auth.Api/Controllers/RolesController.cs:12`, `src/Auth.Api/Controllers/PermissionsController.cs:11`, `src/Auth.Api/Controllers/ApplicationsController.cs:11`, `src/Auth.Api/Controllers/TenantsController.cs:11`
- Supporting locations: `src/Auth.Infrastructure/Authorization/PermissionPolicyProvider.cs:17`, `src/Auth.Infrastructure/Authorization/PermissionAuthorizationHandler.cs:10`
- Attacker-controlled source: any valid authenticated JWT
- Broken control: controllers require `[Authorize]` only; no `Permission:*` policy, `AuthorizeAsync`, or role/permission check is applied to privileged actions
- Impact: any authenticated user can perform privileged mutations such as assigning roles, granting permissions, rotating API keys, creating users, and updating tenants
- CWE: CWE-862, CWE-285

### CAND-003: Tenant/object isolation is not enforced in data access

- Affected locations: `src/Auth.Api/Middleware/TenantResolutionMiddleware.cs:30`, `src/Auth.Infrastructure/Services/CurrentUserService.cs:44`, `src/Auth.Api/Controllers/UsersController.cs:39`, `src/Auth.Api/Controllers/UsersController.cs:76`, `src/Auth.Api/Controllers/TenantsController.cs:30`, `src/Auth.Api/Controllers/ApplicationsController.cs:30`
- Attacker-controlled source: authenticated requests selecting route IDs, pages, or tenant header/subdomain
- Broken control: tenant context exists but controller queries do not filter by current tenant or compare object tenant/application ownership
- Impact: cross-tenant/user/application/role data exposure and unauthorized mutation paths
- CWE: CWE-639, CWE-862

### CAND-004: Login throttling is not enforced at the API boundary

- Affected locations: `src/Auth.Api/Controllers/AuthController.cs:26`, `src/Auth.Application/Features/Auth/Commands/LoginCommand.cs:41`, `src/Auth.Application/Features/Auth/Commands/LoginCommand.cs:46`, `src/Auth.Api/appsettings.json:31`
- Attacker-controlled source: anonymous login attempts
- Broken control: rate-limit configuration exists, but no rate-limiter middleware/service is registered; lockout increments only after a known user is found and password verification fails
- Impact: credential stuffing and password spraying are materially easier than documented security posture implies
- CWE: CWE-307

### CAND-005: JWT bearer token accepted through query parameter globally

- Affected location: `src/Auth.Api/Extensions/ServiceCollectionExtensions.cs:91`
- Attacker-controlled source: any HTTP request with `?access_token=...`
- Broken control: bearer-token transport falls back to query string for every path
- Impact: token leakage through URL logs, browser history, reverse proxies, analytics, and referrers
- CWE: CWE-598
