# Enterprise Authentication & Authorization Platform - Architecture Document

## Table of Contents
1. [Architecture Overview](#1-architecture-overview)
2. [Project Structure](#2-project-structure)
3. [Database Schema](#3-database-schema)
4. [Authentication Flow](#4-authentication-flow)
5. [Authorization Flow](#5-authorization-flow)
6. [Future Scalability Considerations](#6-future-scalability-considerations)

---

## 1. Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Client Applications                         │
│              (SPA, Mobile, Server-to-Server, etc.)                  │
└───────────────────────────┬─────────────────────────────────────────┘
                            │ HTTPS
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      API Gateway / Load Balancer                    │
│                    (Nginx / Traefik / Azure Gateway)                │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     Auth Platform - REST API (.NET 8)               │
├─────────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐               │
│  │ Auth        │  │ User         │  │ Permission   │               │
│  │ Controller  │  │ Controller   │  │ Controller   │               │
│  └──────┬──────┘  └──────┬───────┘  └──────┬───────┘               │
│         │                │                  │                       │
│  ┌──────┴────────────────┴──────────────────┴───────┐              │
│  │              Application Layer (MediatR)          │              │
│  │         Commands, Queries, Validators, Mappings   │              │
│  └──────────────────────┬───────────────────────────┘              │
│                         │                                          │
│  ┌──────────────────────┴───────────────────────────┐              │
│  │              Domain Layer (Core)                  │              │
│  │     Entities, Value Objects, Interfaces, Enums    │              │
│  └──────────────────────┬───────────────────────────┘              │
│                         │                                          │
│  ┌──────────────────────┴───────────────────────────┐              │
│  │           Infrastructure Layer                    │              │
│  │   EF Core, JWT, BCrypt, Repositories, Caching    │              │
│  └──────────────────────┬───────────────────────────┘              │
│                         │                                          │
│  ┌──────────────────────┴───────────────────────────┐              │
│  │              Persistence Layer                    │              │
│  │              MySQL Database (EF Core)             │              │
│  └───────────────────────────────────────────────────┘              │
└─────────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         MySQL Database                              │
│              (Master-Slave Replication Ready)                       │
└─────────────────────────────────────────────────────────────────────┘
```

### Key Architectural Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Architecture Pattern** | Clean Architecture | Separation of concerns, testability, maintainability |
| **API Pattern** | REST + CQRS (MediatR) | Clear separation of reads/writes, pipeline behaviors |
| **ORM** | Entity Framework Core 8 | Mature, LINQ support, migrations |
| **Database** | MySQL 8 | ACID compliance, JSON support, replication |
| **Authentication** | JWT (RS256) + Refresh Tokens | Stateless, secure, scalable |
| **Authorization** | RBAC + Permission-based | Fine-grained, flexible, auditable |
| **Mapping** | AutoMapper | Convention-based, maintainable |
| **Validation** | FluentValidation | Declarative, composable |
| **Containerization** | Docker + Docker Compose | Consistent environments, easy deployment |
| **API Documentation** | Swagger/OpenAPI | Industry standard, interactive testing |

---

## 2. Project Structure

```
auth-platform/
│
├── src/
│   ├── AuthPlatform.Api/                    # Presentation Layer
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs            # Login, Refresh, Logout
│   │   │   ├── UsersController.cs           # CRUD users
│   │   │   ├── RolesController.cs           # CRUD roles
│   │   │   ├── PermissionsController.cs     # Permission management
│   │   │   ├── ApplicationsController.cs    # Application management
│   │   │   └── TenantsController.cs         # Tenant management (future)
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   ├── RequestLoggingMiddleware.cs
│   │   │   └── TenantResolutionMiddleware.cs
│   │   ├── Filters/
│   │   │   ├── PermissionAuthorizationFilter.cs
│   │   │   └── ValidationFilter.cs
│   │   ├── Extensions/
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   ├── ApplicationBuilderExtensions.cs
│   │   │   └── SwaggerExtensions.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Dockerfile
│   │   └── Properties/launchSettings.json
│   │
│   ├── AuthPlatform.Application/            # Application Layer
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IApplicationDbContext.cs
│   │   │   │   ├── IJwtService.cs
│   │   │   │   ├── IRefreshTokenService.cs
│   │   │   │   ├── IPasswordHasher.cs
│   │   │   │   ├── ICurrentUserService.cs
│   │   │   │   ├── ICacheService.cs
│   │   │   │   └── ITenantService.cs
│   │   │   ├── Models/
│   │   │   │   ├── JwtToken.cs
│   │   │   │   ├── AuthenticationResult.cs
│   │   │   │   └── PagedResult.cs
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── AuthorizationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   └── PerformanceBehavior.cs
│   │   │   ├── Exceptions/
│   │   │   │   ├── NotFoundException.cs
│   │   │   │   ├── UnauthorizedException.cs
│   │   │   │   ├── ForbiddenException.cs
│   │   │   │   └── ValidationException.cs
│   │   │   └── Mappings/
│   │   │       └── MappingProfile.cs
│   │   ├── Features/
│   │   │   ├── Auth/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── LoginCommand.cs
│   │   │   │   │   ├── RefreshTokenCommand.cs
│   │   │   │   │   ├── RevokeTokenCommand.cs
│   │   │   │   │   └── LogoutCommand.cs
│   │   │   │   └── Queries/
│   │   │   │       └── GetCurrentUserQuery.cs
│   │   │   ├── Users/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateUserCommand.cs
│   │   │   │   │   ├── UpdateUserCommand.cs
│   │   │   │   │   └── DeleteUserCommand.cs
│   │   │   │   └── Queries/
│   │   │   │       ├── GetUserByIdQuery.cs
│   │   │   │       └── GetUsersListQuery.cs
│   │   │   ├── Roles/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateRoleCommand.cs
│   │   │   │   │   ├── UpdateRoleCommand.cs
│   │   │   │   │   ├── AssignRoleCommand.cs
│   │   │   │   │   └── RemoveRoleCommand.cs
│   │   │   │   └── Queries/
│   │   │   │       ├── GetRoleByIdQuery.cs
│   │   │   │       └── GetRolesListQuery.cs
│   │   │   ├── Permissions/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── AssignPermissionCommand.cs
│   │   │   │   │   └── RevokePermissionCommand.cs
│   │   │   │   └── Queries/
│   │   │   │       ├── GetUserPermissionsQuery.cs
│   │   │   │       └── GetRolePermissionsQuery.cs
│   │   │   └── Applications/
│   │   │       ├── Commands/
│   │   │       │   ├── RegisterApplicationCommand.cs
│   │   │       │   └── UpdateApplicationCommand.cs
│   │   │       └── Queries/
│   │   │           ├── GetApplicationByIdQuery.cs
│   │   │           └── GetApplicationsListQuery.cs
│   │   ├── DependencyInjection.cs
│   │   └── AuthPlatform.Application.csproj
│   │
│   ├── AuthPlatform.Domain/                 # Domain Layer (Core)
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Role.cs
│   │   │   ├── Permission.cs
│   │   │   ├── RolePermission.cs
│   │   │   ├── UserRole.cs
│   │   │   ├── UserPermission.cs
│   │   │   ├── RefreshToken.cs
│   │   │   ├── Application.cs
│   │   │   └── Tenant.cs
│   │   ├── ValueObjects/
│   │   │   ├── Email.cs
│   │   │   ├── PasswordHash.cs
│   │   │   └── PhoneNumber.cs
│   │   ├── Enums/
│   │   │   ├── UserStatus.cs
│   │   │   ├── TokenType.cs
│   │   │   └── PermissionGroup.cs
│   │   ├── Events/
│   │   │   ├── UserCreatedEvent.cs
│   │   │   ├── UserLoggedInEvent.cs
│   │   │   └── PermissionChangedEvent.cs
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs
│   │   │   ├── BaseAuditableEntity.cs
│   │   │   ├── ValueObject.cs
│   │   │   └── DomainEvent.cs
│   │   └── AuthPlatform.Domain.csproj
│   │
│   └── AuthPlatform.Infrastructure/         # Infrastructure Layer
│       ├── Persistence/
│       │   ├── ApplicationDbContext.cs
│       │   ├── Configurations/
│       │   │   ├── UserConfiguration.cs
│       │   │   ├── RoleConfiguration.cs
│       │   │   ├── PermissionConfiguration.cs
│       │   │   ├── RefreshTokenConfiguration.cs
│       │   │   ├── ApplicationConfiguration.cs
│       │   │   └── TenantConfiguration.cs
│       │   ├── Migrations/
│       │   ├── Repositories/
│       │   │   ├── UserRepository.cs
│       │   │   ├── RoleRepository.cs
│       │   │   └── PermissionRepository.cs
│       │   └── Seed/
│       │       └── ApplicationDbContextSeed.cs
│       ├── Services/
│       │   ├── JwtService.cs
│       │   ├── RefreshTokenService.cs
│       │   ├── PasswordHasherService.cs
│       │   ├── CurrentUserService.cs
│       │   ├── CacheService.cs
│       │   └── TenantService.cs
│       ├── Authentication/
│       │   ├── JwtSettings.cs
│       │   └── JwtTokenValidator.cs
│       ├── Authorization/
│       │   ├── PermissionRequirement.cs
│       │   ├── PermissionAuthorizationHandler.cs
│       │   └── PermissionPolicyProvider.cs
│       ├── DependencyInjection.cs
│       └── AuthPlatform.Infrastructure.csproj
│
├── tests/
│   ├── AuthPlatform.Api.Tests/
│   │   ├── Controllers/
│   │   └── IntegrationTests/
│   ├── AuthPlatform.Application.Tests/
│   │   ├── Features/
│   │   └── Common/
│   └── AuthPlatform.Domain.Tests/
│       └── Entities/
│
├── docker/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   ├── docker-compose.override.yml
│   ├── mysql/
│   │   ├── init.sql
│   │   └── my.cnf
│   └── nginx/
│       └── nginx.conf
│
├── docs/
│   ├── api/
│   │   └── openapi.yaml
│   └── architecture/
│       └── diagrams/
│
├── .gitignore
├── .editorconfig
├── AuthPlatform.sln
├── Directory.Build.props
└── README.md
```

### Layer Responsibilities

| Layer | Responsibility | Dependencies |
|-------|---------------|--------------|
| **Api** | HTTP concerns, routing, middleware, Swagger | Application |
| **Application** | Business logic, CQRS, validation, mapping | Domain |
| **Domain** | Core entities, business rules, enums, events | None |
| **Infrastructure** | External concerns: DB, JWT, caching, email | Application |

---

## 3. Database Schema

### Entity Relationship Diagram (Textual)

```
┌──────────────────┐       ┌──────────────────────┐       ┌──────────────────┐
│    Applications   │       │       Tenants         │       │      Users       │
├──────────────────┤       ├──────────────────────┤       ├──────────────────┤
│ PK: Id (GUID)    │       │ PK: Id (GUID)        │       │ PK: Id (GUID)    │
│ Name             │       │ Name                  │       │ Username         │
│ Code             │       │ Slug                  │       │ Email            │
│ Description      │       │ ConnectionString (opt)│       │ PasswordHash     │
│ IsActive         │       │ IsActive              │       │ FirstName        │
│ ApiKey           │       │ CreatedAt             │       │ LastName         │
│ CreatedAt        │       └──────────┬───────────┘       │ PhoneNumber      │
└────────┬─────────┘                  │                    │ Status (enum)    │
         │                            │                    │ IsLocked         │
         │                            │                    │ LockoutEnd       │
         │                            │                    │ AccessFailedCount│
         │                            │                    │ LastLoginAt      │
         │                            │                    │ CreatedAt        │
         │                            │                    │ UpdatedAt        │
         │                            │                    │ TenantId (FK)    │
         │                            │                    └────────┬─────────┘
         │                            │                             │
         │                            │                             │
         │              ┌─────────────┴─────────────────────────────┘
         │              │
         │    ┌─────────┴──────────────────┐
         │    │     UserTenants (Future)    │
         │    ├────────────────────────────┤
         │    │ PK: Id (GUID)              │
         │    │ FK: UserId                 │
         │    │ FK: TenantId               │
         │    │ IsDefault                  │
         │    └────────────────────────────┘
         │
         │              ┌──────────────────┐
         │              │   RefreshTokens   │
         │              ├──────────────────┤
         │              │ PK: Id (GUID)    │
         │              │ Token            │
         │              │ JwtId            │
         │              │ FK: UserId       │
         │              │ IsRevoked        │
         │              │ IsUsed           │
         │              │ ExpiresAt        │
         │              │ CreatedAt        │
         │              │ RevokedAt        │
         │              │ DeviceInfo       │
         │              │ IpAddress        │
         │              └────────┬─────────┘
         │                       │
         │                       │
┌────────┴───────────────────────┴──────────────────────────────────────┐
│                                                                       │
│  ┌──────────────┐    ┌──────────────────┐    ┌──────────────────┐     │
│  │    Roles      │    │  RolePermissions  │    │   Permissions    │     │
│  ├──────────────┤    ├──────────────────┤    ├──────────────────┤     │
│  │ PK: Id (GUID)│    │ PK: Id (GUID)    │    │ PK: Id (GUID)    │     │
│  │ Name         │◄───┤ FK: RoleId       │───►│ Name             │     │
│  │ NormalizedName│   │ FK: PermissionId │    │ DisplayName      │     │
│  │ Description  │    │ CreatedAt        │    │ Description      │     │
│  │ IsSystem     │    └──────────────────┘    │ Group            │     │
│  │ ApplicationId│                            │ IsSystem         │     │
│  │ CreatedAt    │                            │ CreatedAt        │     │
│  └──────┬───────┘                            └────────┬─────────┘     │
│         │                                              │              │
│         │                                              │              │
│  ┌──────┴──────────┐          ┌──────────────────┐     │              │
│  │   UserRoles      │          │  UserPermissions  │     │              │
│  ├────────────────┤          ├──────────────────┤     │              │
│  │ PK: Id (GUID)  │          │ PK: Id (GUID)    │     │              │
│  │ FK: UserId     │───┐      │ FK: UserId       │─────┘              │
│  │ FK: RoleId     │   │      │ FK: PermissionId │                    │
│  │ AssignedAt     │   │      │ IsGranted        │                    │
│  │ AssignedBy     │   │      │ GrantedAt        │                    │
│  └────────────────┘   │      │ GrantedBy        │                    │
│                        │      │ ExpiresAt (opt)  │                    │
│                        │      └──────────────────┘                    │
│                        │                                              │
│                        │                                              │
│                        │                                              │
│                        │                                              │
│                        ▼                                              │
│              ┌──────────────────┐                                     │
│              │   AuditLogs      │                                     │
│              ├──────────────────┤                                     │
│              │ PK: Id (GUID)    │                                     │
│              │ UserId           │                                     │
│              │ Action           │                                     │
│              │ EntityType       │                                     │
│              │ EntityId         │                                     │
│              │ OldValues (JSON) │                                     │
│              │ NewValues (JSON) │                                     │
│              │ IpAddress        │                                     │
│              │ UserAgent        │                                     │
│              │ Timestamp        │                                     │
│              └──────────────────┘                                     │
└───────────────────────────────────────────────────────────────────────┘
```

### Table Definitions

#### 1. `Applications`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | CHAR(36) | PK | UUID |
| Name | VARCHAR(100) | NOT NULL | Application display name |
| Code | VARCHAR(50) | NOT NULL, UNIQUE | Application code (e.g., "admin-portal") |
| Description | VARCHAR(500) | NULL | Description |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | Active status |
| ApiKey | VARCHAR(255) | NOT NULL, UNIQUE | API key for server-to-server auth |
| CreatedAt | DATETIME | NOT NULL | Creation timestamp |
| UpdatedAt | DATETIME | NULL | Last update timestamp |

#### 2. `Tenants`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | CHAR(36) | PK | UUID |
| Name | VARCHAR(200) | NOT NULL | Tenant name |
| Slug | VARCHAR(100) | NOT NULL, UNIQUE | URL-friendly identifier |
| ConnectionString | VARCHAR(500) | NULL | Optional isolated DB connection |
| IsActive | BOOLEAN | NOT NULL, DEFAULT TRUE | Active status |
| CreatedAt | DATETIME | NOT NULL | Creation timestamp |
| UpdatedAt | DATETIME | NULL | Last update timestamp |

#### 3. `Users`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | CHAR(36) | PK | UUID |
| Username | VARCHAR(50) | NOT NULL, UNIQUE | Login username |
| Email | VARCHAR(255) | NOT NULL, UNIQUE | Email address |
| PasswordHash | VARCHAR(500) | NOT NULL | BCrypt hash |
| FirstName | VARCHAR(100) | NOT NULL | First name |
| LastName | VARCHAR(100) | NOT NULL | Last name |
| PhoneNumber | VARCHAR(20) | NULL | Phone number |
| Status | TINYINT | NOT NULL, DEFAULT 1 | 0=Inactive, 1=Active, 2=Suspended |
| IsLocked | BOOLEAN | NOT NULL, DEFAULT FALSE | Account lock status |
| LockoutEnd | DATETIME | NULL | Lockout expiration |
| AccessFailedCount | INT | NOT NULL, DEFAULT 0 | Failed login attempts |
| LastLoginAt | DATETIME | NULL | Last successful login |
| TenantId | CHAR(36) | NULL, FK→Tenants | Tenant association |
| CreatedAt | DATETIME | NOT NULL | Creation timestamp |
| UpdatedAt | DATETIME | NULL | Last update timestamp |

#### 4. `Roles`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | CHAR(36) | PK | UUID |
| Name | VARCHAR(100) | NOT NULL | Role name (e.g., "Admin") |
| NormalizedName | VARCHAR(100) | NOT NULL | Uppercased name for lookup |
| Description | VARCHAR(500) | NULL | Description |
| IsSystem | BOOLEAN | NOT NULL, DEFAULT FALSE | System-protected role |
| ApplicationId | CHAR(36) | NOT NULL, FK→Applications | Scoped to application |
| CreatedAt | DATETIME | NOT NULL | Creation timestamp |
| UpdatedAt | DATETIME | NULL | Last update timestamp |

**Unique Constraint:** (NormalizedName, ApplicationId)

#### 5. `Permissions`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | CHAR(36) | PK | UUID |
| Name | VARCHAR(150) | NOT NULL, UNIQUE | Permission key (e.g., "users.create") |
| DisplayName | VARCHAR(200) | NOT NULL | Human-readable name |
| Description | VARCHAR(500) | NULL | Description |
| Group | VARCHAR(100) | NOT NULL | Grouping (e.g., "Users", "Roles") |
| IsSystem | BOOLEAN | NOT NULL, DEFAULT FALSE | System-protected |
| CreatedAt | DATETIME | NOT NULL | Creation timestamp |

#### 6. `RolePermissions`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | CHAR(36) | PK | UUID |
| RoleId | CHAR(36) | NOT NULL, FK→Roles | Role reference |
| PermissionId | CHAR(36) | NOT NULL, FK→Permissions | Permission reference |
| CreatedAt | DATETIME | NOT NULL | Assignment timestamp |

**Unique Constraint:** (RoleId, PermissionId)

#### 7. `UserRoles`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | CHAR(36) | PK | UUID |
| UserId | CHAR(36) | NOT NULL, FK→Users | User reference |
| RoleId | CHAR(36) | NOT NULL, FK→Roles | Role reference |
| AssignedAt | DATETIME | NOT NULL | Assignment timestamp |
| AssignedBy | CHAR(36) | NULL, FK→Users | Who assigned |

**Unique Constraint:** (UserId, RoleId)

#### 8. `UserPermissions` (Direct permission overrides)
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | CHAR(36) | PK | UUID |
| UserId | CHAR(36) | NOT NULL, FK→Users | User reference |
| PermissionId | CHAR(36) | NOT NULL, FK→Permissions | Permission reference |
| IsGranted | BOOLEAN | NOT NULL | true=grant, false=deny (override) |
| GrantedAt | DATETIME | NOT NULL | Assignment timestamp |
| GrantedBy | CHAR(36) | NULL, FK→Users | Who granted |
| ExpiresAt | DATETIME | NULL | Optional expiration |

**Unique Constraint:** (UserId, PermissionId)

#### 9. `RefreshTokens`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | CHAR(36) | PK | UUID |
| Token | VARCHAR(500) | NOT NULL, UNIQUE | Hashed refresh token |
| JwtId | VARCHAR(100) | NOT NULL | Associated JWT ID (jti) |
| UserId | CHAR(36) | NOT NULL, FK→Users | User reference |
| IsRevoked | BOOLEAN | NOT NULL, DEFAULT FALSE | Revoked flag |
| IsUsed | BOOLEAN | NOT NULL, DEFAULT FALSE | Used flag (rotation) |
| ExpiresAt | DATETIME | NOT NULL | Expiration |
| CreatedAt | DATETIME | NOT NULL | Creation timestamp |
| RevokedAt | DATETIME | NULL | Revocation timestamp |
| DeviceInfo | VARCHAR(500) | NULL | Device/user-agent info |
| IpAddress | VARCHAR(45) | NULL | Client IP |

#### 10. `AuditLogs`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | CHAR(36) | PK | UUID |
| UserId | CHAR(36) | NULL, FK→Users | Who performed action |
| Action | VARCHAR(100) | NOT NULL | Action type (e.g., "USER_LOGIN") |
| EntityType | VARCHAR(100) | NOT NULL | Entity name |
| EntityId | VARCHAR(36) | NOT NULL | Entity ID |
| OldValues | JSON | NULL | Previous state |
| NewValues | JSON | NULL | New state |
| IpAddress | VARCHAR(45) | NULL | Client IP |
| UserAgent | VARCHAR(500) | NULL | User agent |
| Timestamp | DATETIME | NOT NULL | When it happened |

### Indexing Strategy

```sql
-- Users
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_TenantId ON Users(TenantId);
CREATE INDEX IX_Users_Status ON Users(Status);

-- Roles
CREATE INDEX IX_Roles_ApplicationId ON Roles(ApplicationId);
CREATE INDEX IX_Roles_NormalizedName_ApplicationId ON Roles(NormalizedName, ApplicationId);

-- Permissions
CREATE INDEX IX_Permissions_Group ON Permissions(`Group`);

-- RefreshTokens
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
CREATE INDEX IX_RefreshTokens_ExpiresAt ON RefreshTokens(ExpiresAt);

-- AuditLogs
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_Action ON AuditLogs(Action);
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
```

---

## 4. Authentication Flow

### 4.1 Login Flow

```
Client                          Auth Platform                        Database
  │                                  │                                  │
  │  POST /api/auth/login            │                                  │
  │  {email, password, appCode}      │                                  │
  │─────────────────────────────────►│                                  │
  │                                  │                                  │
  │                          1. Validate Request                       │
  │                             (FluentValidation)                     │
  │                                  │                                  │
  │                          2. Find User by Email                     │
  │                                  │─────────────────────────────────►│
  │                                  │◄────────────────────────────────│
  │                                  │                                  │
  │                          3. Verify Password (BCrypt)               │
  │                                  │                                  │
  │                          4. Check:                                 │
  │                             - Is account active?                   │
  │                             - Is account locked?                   │
  │                             - Does user belong to app?             │
  │                                  │                                  │
  │                          5. Generate JWT (RS256)                   │
  │                             Claims:                                │
  │                             - sub: userId                          │
  │                             - email: user@email.com                │
  │                             - jti: unique token id                 │
  │                             - app: applicationCode                 │
  │                             - roles: ["Admin", "Editor"]           │
  │                             - permissions: ["users.read", ...]     │
  │                             - tenant: tenantId (optional)          │
  │                             - iat, exp, iss, aud                   │
  │                                  │                                  │
  │                          6. Generate Refresh Token                 │
  │                             (Cryptographically random, hashed)     │
  │                                  │                                  │
  │                          7. Store Refresh Token                    │
  │                                  │─────────────────────────────────►│
  │                                  │                                  │
  │                          8. Update LastLoginAt                     │
  │                                  │─────────────────────────────────►│
  │                                  │                                  │
  │                          9. Log Audit Event                        │
  │                                  │─────────────────────────────────►│
  │                                  │                                  │
  │  {accessToken, refreshToken,     │                                  │
  │   expiresIn, user}               │                                  │
  │◄─────────────────────────────────│                                  │
```

### 4.2 Token Refresh Flow

```
Client                          Auth Platform                        Database
  │                                  │                                  │
  │  POST /api/auth/refresh          │                                  │
  │  {refreshToken}                  │                                  │
  │─────────────────────────────────►│                                  │
  │                                  │                                  │
  │                          1. Validate Request                       │
  │                                  │                                  │
  │                          2. Hash incoming token                    │
  │                          3. Find matching refresh token            │
  │                                  │─────────────────────────────────►│
  │                                  │◄────────────────────────────────│
  │                                  │                                  │
  │                          4. Validate:                              │
  │                             - Token exists?                        │
  │                             - Is it revoked?                       │
  │                             - Is it used? (replay detection)       │
  │                             - Is it expired?                       │
  │                                  │                                  │
  │                          5. Mark old token as used (rotation)      │
  │                                  │─────────────────────────────────►│
  │                                  │                                  │
  │                          6. Generate new JWT                       │
  │                          7. Generate new refresh token             │
  │                          8. Store new refresh token                │
  │                                  │─────────────────────────────────►│
  │                                  │                                  │
  │  {accessToken, refreshToken,     │                                  │
  │   expiresIn}                     │                                  │
  │◄─────────────────────────────────│                                  │
```

### 4.3 Token Revocation / Logout

```
Client                          Auth Platform                        Database
  │                                  │                                  │
  │  POST /api/auth/logout           │                                  │
  │  {refreshToken}                  │                                  │
  │  Authorization: Bearer <jwt>     │                                  │
  │─────────────────────────────────►│                                  │
  │                                  │                                  │
  │                          1. Validate JWT                           │
  │                          2. Find & revoke refresh token            │
  │                                  │─────────────────────────────────►│
  │                                  │                                  │
  │                          3. Add JWT jti to blacklist               │
  │                             (Redis cache until JWT expiry)         │
  │                                  │                                  │
  │  204 No Content                  │                                  │
  │◄─────────────────────────────────│                                  │
```

### 4.4 JWT Token Structure

```json
{
  "alg": "RS256",
  "typ": "JWT",
  "kid": "key-id-2024"
}
.
{
  "sub": "a1b2c3d4-...",           // User ID
  "email": "user@example.com",      // Email
  "name": "John Doe",               // Full name
  "jti": "unique-token-id",         // Token ID (for refresh binding)
  "app": "admin-portal",            // Application code
  "roles": ["Admin", "Editor"],     // Role names
  "permissions": [                  // All resolved permissions
    "users.read",
    "users.create",
    "users.update",
    "roles.read"
  ],
  "tenant": "tenant-slug",          // Tenant (optional)
  "iat": 1700000000,                // Issued at
  "exp": 1700003600,                // Expiration (1 hour)
  "iss": "auth-platform",           // Issuer
  "aud": "admin-portal"             // Audience (application)
}
```

### 4.5 Security Measures

| Measure | Implementation |
|---------|---------------|
| **Password Storage** | BCrypt with work factor 12 |
| **JWT Signing** | RS256 (asymmetric) - RSA key pair |
| **Access Token Lifetime** | 15-30 minutes (short-lived) |
| **Refresh Token Lifetime** | 7 days (configurable) |
| **Refresh Token Rotation** | New token issued on each refresh |
| **Replay Detection** | Old refresh token marked as used |
| **Token Revocation** | JWT blacklist in Redis |
| **Rate Limiting** | Per IP, per user, per endpoint |
| **Account Lockout** | After N failed attempts (configurable) |
| **Password Policy** | Min length, complexity, history |
| **Audit Logging** | All auth events logged |

---

## 5. Authorization Flow

### 5.1 Authorization Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Authorization Pipeline                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────┐    ┌──────────────┐    ┌────────────────────────┐    │
│  │ Request   │───►│ JWT          │───►│ Permission-based       │    │
│  │           │    │ Authentication│    │ Authorization          │    │
│  └──────────┘   