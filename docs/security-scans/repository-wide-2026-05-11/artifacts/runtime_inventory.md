# Runtime Inventory

## Scope

- Scan type: repository-wide security code review
- Repository: `AuthPlatform`
- Commit observed: `2193a3f`
- Branch observed: `code-review`
- In-scope runtime files: `src/**`
- Excluded from primary runtime pass: `tests/**`, `docs/**`, generated cache files, and local developer artifacts.

## Product Surfaces

- ASP.NET Core API: `src/Auth.Api/Program.cs`
- Public anonymous endpoints: `POST /api/auth/login`, `POST /api/auth/refresh`
- Authenticated endpoints: `/api/auth/revoke`, `/api/auth/me`, `/api/users`, `/api/roles`, `/api/permissions`, `/api/applications`, `/api/tenants`
- Health endpoint: `/health`
- Swagger UI: development-only branch in `Program.cs`
- Docker deployment surface: `docker-compose.yml`, `src/Auth.Api/Dockerfile`, `docker/nginx/nginx.conf`

## Security Controls

- JWT authentication configured in `src/Auth.Api/Extensions/ServiceCollectionExtensions.cs`
- Permission policy provider and handler in `src/Auth.Infrastructure/Authorization/*`
- JWT issue/validate helpers in `src/Auth.Infrastructure/Services/JwtService.cs`
- Refresh-token rotation in `src/Auth.Infrastructure/Services/RefreshTokenService.cs`
- Password hashing in `src/Auth.Infrastructure/Services/PasswordHasherService.cs`
- Tenant resolution middleware in `src/Auth.Api/Middleware/TenantResolutionMiddleware.cs`
- EF Core persistence in `src/Auth.Persistence/ApplicationDbContext.cs`
- Seed data in `src/Auth.Persistence/Seed/ApplicationDbContextSeed.cs`

## High-Risk Data And State

- User credentials and password hashes
- JWT signing keys
- Access tokens and refresh tokens
- Role and permission assignments
- Tenant objects and connection strings
- Application API keys
- Audit logs and authentication events

## Entry Points Reviewed

- `AuthController`: login, refresh, revoke, current user
- `UsersController`: list, read, create, update, delete users
- `RolesController`: list, read, create, update, delete roles, assign/remove user roles
- `PermissionsController`: list permissions, role/user permission reads, assign/remove role permissions, assign/remove user permission overrides
- `ApplicationsController`: list, read, create, update, deactivate, rotate API key
- `TenantsController`: list, read, create, update, deactivate tenants

## Sink And Control Inventory

- Authorization boundaries: `[Authorize]`, permission policy provider, permission claims, role claims
- Tenant boundaries: `TenantId`, `X-Tenant`, `TenantSlug`, `CurrentUserService.TenantId`
- Database sinks: EF Core LINQ and `FindAsync` queries, `Add`, `Remove`, `SaveChangesAsync`
- Secret handling: JWT private/public key path/base64 config, application API keys, database connection strings
- Token transport: Authorization header plus `access_token` query parameter support
- Rate limiting: documented and configured values, no runtime middleware found
- File APIs: key loading from configured paths; no attacker-controlled file path sink found
- Network/process/deserialization/XML/template sinks: no attacker-reachable sink found in runtime code
