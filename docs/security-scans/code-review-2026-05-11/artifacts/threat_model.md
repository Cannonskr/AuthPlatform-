# AuthPlatform Threat Model

## Overview

AuthPlatform is an ASP.NET Core authentication and authorization service. The runtime surface is the `Auth.Api` web API, with controllers for login, refresh-token rotation, logout, user management, tenants, applications, roles, and permissions. The application layer uses MediatR handlers and validators, the infrastructure layer implements JWT, refresh-token, password hashing, caching, and authorization services, and the persistence layer stores users, roles, permissions, tenants, applications, audit logs, and refresh tokens through EF Core/MySQL.

Security-sensitive assets include JWT signing keys, refresh tokens, password hashes, application API keys, tenant data, role/permission assignments, user profile and account state, database connection strings, audit logs, and administrative endpoints that mutate identity or authorization state.

## Threat Model, Trust Boundaries, and Assumptions

External clients cross the HTTP API boundary through `src/Auth.Api/Controllers`. Anonymous users can call login and refresh endpoints, while authenticated users can call protected controllers. Operators control deployment configuration such as JWT keys, database connection strings, CORS origins, and environment settings. Developers control source code, tests, and build artifacts.

Attacker-controlled inputs include request bodies, route IDs, query-string values, authorization bearer tokens, refresh tokens, login credentials, pagination parameters, tenant/application/user/role/permission mutation payloads, and any token presented through the `access_token` query parameter. Operator-controlled inputs include appsettings, secrets, JWT key files or base64 key material, CORS origins, and database credentials.

Important invariants:

- Authentication must only issue tokens after valid credentials and account state checks.
- Refresh tokens must remain unguessable, stored only as hashes, single-use after rotation, and revocable.
- JWT validation must reject forged, expired, wrong-issuer, or unsigned tokens.
- Privileged mutations to users, roles, permissions, tenants, and applications must require appropriate authorization, not merely any authenticated principal.
- Tenant and application boundaries must prevent cross-tenant or cross-application data exposure and privilege changes.
- Secrets and keys must not be committed, logged, returned unnecessarily, or accepted from untrusted users.

## Attack Surface, Mitigations, and Attacker Stories

Primary attack surfaces are `AuthController` for login/refresh/revoke/current-user flows, administrative controllers for identity and authorization data, JWT validation in `ServiceCollectionExtensions`, token generation in `JwtService`, refresh-token rotation in `RefreshTokenService`, password hashing in `PasswordHasherService`, and database access through EF Core contexts and entity configurations.

Existing controls include JWT bearer authentication, RSA signing keys, explicit startup validation for JWT issuer and key configuration, BCrypt password hashing, SHA-256 hashing of refresh tokens before storage, refresh-token rotation, account lockout state in the domain model, EF Core query APIs, production CORS origin configuration, and `[Authorize]` on non-auth controllers.

Realistic attacker stories include credential stuffing against login, refresh-token replay, JWT forgery or validation bypass, IDOR or missing authorization on administrative endpoints, cross-tenant data access, privilege escalation through role/permission mutation APIs, leaked app API keys, unsafe CORS or token transport behavior, and accidental exposure of local/default credentials in deployment configuration.

Out-of-scope or lower-likelihood stories include direct local filesystem access by an attacker, compromise of the deployment secret store outside the application, and test-only code paths unless they are shipped or reachable at runtime.

## Severity Calibration (Critical, High, Medium, Low)

Critical issues would include token forgery, private signing-key disclosure, unauthenticated administrative mutation, broad cross-tenant privilege escalation, or compromise-equivalent role/permission modification.

High issues would include authenticated but unauthorized access to privileged tenant/user/role/permission objects, refresh-token replay that enables account takeover, or returning sensitive API keys beyond the intended one-time creation/rotation flows.

Medium issues would include narrower information disclosure, weak rate-limit enforcement around login, missing tenant scoping for non-critical reads, or configuration choices that materially increase exposure but require additional preconditions.

Low issues would include hardening gaps, audit/logging omissions, non-sensitive metadata exposure, or developer-experience artifacts that do not change runtime security behavior.
