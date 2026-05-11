# Attack Path Analysis Report

## CAND-001: Fixed seeded admin credentials and API key

- Scope: in-scope runtime startup and authentication flow.
- Entry point: anonymous `POST /api/auth/login`.
- Boundary crossed: unauthenticated internet/client actor to administrative identity.
- Attack path:
  1. Application startup calls `InitializeDatabaseAsync`, which runs migrations and seed data.
  2. Empty database receives `admin@authplatform.com` with password `Admin@123`, `viewer@authplatform.com` with password `Viewer@123`, and `default-api-key`.
  3. Attacker authenticates with the known admin credential if the environment has not rotated seeded data.
  4. Attacker receives a valid JWT with admin roles/permissions and can call protected administrative endpoints.
- Counterevidence: none found for environment gating, one-time random bootstrap, forced password reset, or secret injection.
- Severity: critical for fresh or reset deployments; high confidence.
- Policy decision: report.

## CAND-002: Privileged controllers do not enforce permission policies

- Scope: in-scope API controllers and authorization infrastructure.
- Entry point: any authenticated JWT.
- Boundary crossed: normal authenticated identity to privileged identity/authorization control-plane actions.
- Attack path:
  1. Attacker obtains any valid JWT, including a low-privilege seeded viewer token.
  2. The controller class-level `[Authorize]` admits the request.
  3. Mutating actions directly add roles, grant permissions, rotate API keys, create users, update tenants, or delete objects.
  4. No action-level permission policy or explicit authorization check blocks the request.
- Counterevidence: permission policy provider and handler exist, but code search found no controller usage of those policies.
- Severity: critical where any low-privileged account exists; high confidence.
- Policy decision: report.

## CAND-003: Tenant/object isolation is not enforced in data access

- Scope: multi-tenant advertised product surface and user/tenant/application data access.
- Entry point: authenticated requests with route IDs, pagination, and tenant header/subdomain.
- Boundary crossed: one authenticated tenant/user context to other tenant/user/application objects.
- Attack path:
  1. Attacker authenticates.
  2. Attacker calls list/read/update endpoints with arbitrary IDs or pages.
  3. Controllers query global DbSets without filtering by current user tenant or resolved tenant slug.
  4. Attacker reads or mutates objects outside their tenant/application boundary.
- Counterevidence: tenant claims and middleware exist, but no enforcement was found in controller queries or global filters.
- Severity: high; confidence high for exposure, medium for deployment-specific tenant model details.
- Policy decision: report.

## CAND-004: Login throttling is not enforced at the API boundary

- Scope: anonymous login endpoint.
- Entry point: repeated anonymous login requests.
- Boundary crossed: unauthenticated actor targets account credentials.
- Attack path:
  1. Attacker sends repeated login attempts.
  2. Unknown emails return before failed-attempt accounting.
  3. Known-user wrong-password attempts increment user lockout, but no IP/user rate limiter middleware is registered.
  4. Attacker can password-spray many usernames without the documented per-IP/per-user throttling.
- Counterevidence: appsettings and README mention rate limiting, but runtime registration/search did not find rate-limiting middleware.
- Severity: medium; confidence high.
- Policy decision: report.

## CAND-005: JWT bearer token accepted through query parameter globally

- Scope: JWT authentication setup.
- Entry point: any HTTP request URL containing `access_token`.
- Boundary crossed: bearer token moves from auth header into URL/logging surfaces.
- Attack path:
  1. Client or attacker-crafted flow places a JWT in `?access_token=...`.
  2. JWT bearer handler accepts that value for every endpoint.
  3. URLs can be stored in proxy logs, server logs, browser history, analytics, and referrers.
  4. Anyone with log access or leaked URL can replay the bearer token until expiry.
- Counterevidence: no restriction to WebSocket/SSE paths found.
- Severity: medium; confidence medium-high.
- Policy decision: report.
