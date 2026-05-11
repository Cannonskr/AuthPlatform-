# Validation Report

## Validation Rubric

- [x] The affected code is in a real runtime path.
- [x] The attacker-controlled source is identified.
- [x] The broken control or missing guard is identified at a concrete file/line.
- [x] Counterevidence was searched in nearby and global runtime code.
- [x] The impact crosses a meaningful auth, tenant, credential, or secret boundary.

Dynamic build validation was attempted with `dotnet build AuthPlatform.slnx`, but package restore was blocked by sandboxed network access to `nuget.org` (`NU1301`, socket forbidden). The findings below rely on static code tracing and repository configuration evidence.

## Validation Closure Table

| Ledger row | Candidate | Root control | Entry point/source | Sink/control | Disposition | Counterevidence or proof gap | Survives |
|---|---|---|---|---|---|---|---|
| RW-001 | CAND-001 | `ApplicationDbContextSeed.cs:187` | Anonymous login | Fixed seeded admin password | reportable | `InitializeDatabaseAsync` runs seeding on startup; no environment gate, forced rotation, or random bootstrap secret found. | yes |
| RW-001 | CAND-001 | `ApplicationDbContextSeed.cs:73` | App seed / application API key consumers | Fixed `default-api-key` | reportable | No evidence that API keys are hashed or generated uniquely for the default app. | yes |
| RW-002 | CAND-002 | `PermissionsController.cs:207` | Authenticated JWT | Direct user permission grant | reportable | Permission policy provider exists, but no controller action uses `Permission:*` policies or `AuthorizeAsync`. | yes |
| RW-002 | CAND-002 | `RolesController.cs:166` | Authenticated JWT | Assign role to any user | reportable | Only class-level `[Authorize]`; no role/permission/tenant check before `UserRoles.Add`. | yes |
| RW-002 | CAND-002 | `ApplicationsController.cs:157` | Authenticated JWT | Rotate API key | reportable | Only class-level `[Authorize]`; API key returned to caller. | yes |
| RW-002 | CAND-002 | `TenantsController.cs:109` | Authenticated JWT | Update tenant connection string | reportable | Only class-level `[Authorize]`; no tenant management permission check. | yes |
| RW-003 | CAND-003 | `UsersController.cs:39` | Authenticated JWT | List all users | reportable | `TenantId`, `TenantSlug`, and current-user tenant claim exist, but query has no tenant filter. | yes |
| RW-003 | CAND-003 | `TenantsController.cs:30` | Authenticated JWT | List all tenants | reportable | No tenant scoping or permission check in query. | yes |
| RW-004 | CAND-004 | `LoginCommand.cs:41` | Anonymous login | Failed-login accounting | reportable | Unknown email returns before any throttling/lockout state update; no `AddRateLimiter`/`UseRateLimiter` found. | yes |
| RW-005 | CAND-005 | `ServiceCollectionExtensions.cs:91` | Any HTTP request URL | Query token extraction | reportable | No path restriction to WebSocket/SSE endpoints found. | yes |
| RW-006 | none | EF Core query paths | Request inputs | SQL execution | suppressed | No raw SQL sink found; EF LINQ used. | no |
| RW-007 | none | File reads | Configured key/doc paths | File APIs | suppressed | Paths are operator/static-controlled, not request-controlled. | no |
| RW-008 | none | Runtime code | N/A | RCE/SSRF/parser/template sinks | not_applicable | No reachable sink found in `src/**`. | no |
| RW-009 | none | Dependencies | Package refs | CVE/advisory status | deferred | No advisory seed provided; NuGet network access blocked. | uncertain |

## Notes

- `rg` confirmed permission infrastructure exists but no runtime use of `Permission:` authorization policies, `RequireAuthorization`, `IAuthorizationService.AuthorizeAsync`, or role/permission checks in controllers.
- Tenant-related state appears in middleware, claims, and model properties, but not in data access guards.
- The default credential and API key finding has direct repository evidence and does not require dynamic reproduction to establish reportability.
