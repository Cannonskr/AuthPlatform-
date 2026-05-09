# Auth Platform 🔐

Enterprise-grade authentication and authorization platform built with **.NET 9** following **Clean Architecture** principles.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-79%20passed-brightgreen)](docs/TEST_REPORT.md)

---

## Features

- **🔑 JWT Authentication** — RS256 signed tokens with short-lived access tokens and refresh token rotation
- **🛡️ RBAC + Permission-based Authorization** — Role-based access control with fine-grained permission overrides
- **🏢 Multi-Tenant Ready** — Tenant isolation with optional per-tenant database support
- **📦 Clean Architecture** — Domain, Application, Infrastructure, and API layers with dependency inversion
- **⚡ CQRS with MediatR** — Command/Query separation with pipeline behaviors (validation, logging, performance)
- **🔐 BCrypt Password Hashing** — Work-factor 12 for secure credential storage
- **📋 Audit Logging** — Track all authentication and authorization events
- **🐳 Docker Support** — One-command startup with Docker Compose
- **🧪 Comprehensive Tests** — 79 unit tests across Domain, Application, and Infrastructure layers

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Runtime** | .NET 9 |
| **API** | ASP.NET Core Minimal / Controllers |
| **ORM** | Entity Framework Core 9 |
| **Database** | MySQL 8 |
| **CQRS** | MediatR |
| **Validation** | FluentValidation |
| **Mapping** | AutoMapper |
| **Auth** | JWT (RS256), BCrypt |
| **Caching** | In-Memory / Redis-ready |
| **Testing** | xUnit, FluentAssertions, Moq |
| **Container** | Docker & Docker Compose |

## Architecture

```
┌──────────────────────────────────────────────┐
│              API Layer (ASP.NET Core)         │
│  Controllers · Middleware · Filters · Swagger │
├──────────────────────────────────────────────┤
│          Application Layer (CQRS)             │
│  Commands · Queries · Validators · Behaviors  │
├──────────────────────────────────────────────┤
│            Domain Layer (Core)                │
│  Entities · Value Objects · Enums · Events    │
├──────────────────────────────────────────────┤
│       Infrastructure / Persistence Layer      │
│  EF Core · JWT · BCrypt · Caching · AuthZ    │
└──────────────────────────────────────────────┘
```

## Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for local development)

### Run with Docker Compose

```bash
# Start all services (API, MySQL, Nginx)
docker compose up -d

# API available at: https://localhost:5001
# Swagger UI at:    https://localhost:5001/swagger
```

### Run Locally

```bash
# 1. Start MySQL
docker compose up -d mysql

# 2. Run the API
dotnet run --project src/Auth.Api

# 3. API available at: https://localhost:5001
```

### Run Tests

```bash
dotnet test
# Total: 79 tests — all pass ✓
```

## Project Structure

```
auth-platform/
├── src/
│   ├── Auth.Api/              # ASP.NET Core Web API
│   │   ├── Controllers/       # Auth, Users, Roles, Permissions, etc.
│   │   ├── Middleware/        # Exception handling, logging, tenant resolution
│   │   ├── Filters/           # Validation filter
│   │   └── Extensions/        # Service collection, app builder, Swagger
│   ├── Auth.Application/      # CQRS commands, queries, validators
│   │   ├── Common/            # Interfaces, models, behaviors, exceptions
│   │   └── Features/          # Auth, Users, Roles, Permissions
│   ├── Auth.Domain/           # Core entities, value objects, enums
│   └── Auth.Infrastructure/   # EF Core, JWT, BCrypt, caching, auth
├── tests/
│   ├── Auth.Domain.Tests/         # 43 tests
│   ├── Auth.Application.Tests/    # 22 tests
│   └── Auth.Infrastructure.Tests/ # 14 tests
├── docker/
│   ├── mysql/                # MySQL config & initialization
│   └── nginx/                # Nginx reverse proxy config
├── docs/                     # Architecture, deployment, test reports
└── docker-compose.yml        # Orchestrated services
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Authenticate user |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/revoke` | Revoke refresh token |
| GET | `/api/users` | List users (paged) |
| POST | `/api/users` | Create user |
| GET | `/api/users/{id}` | Get user details |
| GET | `/api/roles` | List roles |
| POST | `/api/roles` | Create role |
| GET | `/api/permissions` | List permissions |
| GET | `/api/applications` | List applications |
| POST | `/api/applications` | Register application |
| GET | `/api/tenants` | List tenants |

> Full API documentation available at `/swagger` when the service is running.

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `MYSQL_ROOT_PASSWORD` | MySQL root password | `root` |
| `ConnectionStrings__DefaultConnection` | MySQL connection string | — |
| `JwtSettings__PrivateKey` | RSA private key (base64) | — |
| `JwtSettings__PublicKey` | RSA public key (base64) | — |

## Test Coverage

| Project | Tests | Coverage Area |
|---------|-------|---------------|
| Domain | 43 | Entities, value objects, domain events |
| Application | 22 | Handlers, validators, models, exceptions |
| Infrastructure | 14 | Services (cache, password hashing, current user) |
| **Total** | **79** | **All passing** ✅ |

[View full test report](docs/TEST_REPORT.md)

## Security

- **JWT Signing** — RS256 asymmetric key pair
- **Password Storage** — BCrypt with work factor 12
- **Token Expiration** — Access tokens: 15-30 min, Refresh tokens: 7 days
- **Token Rotation** — New refresh token issued on each refresh
- **Account Lockout** — Configurable failed attempt threshold
- **Rate Limiting** — Per-IP and per-user throttling

## License

This project is licensed under the MIT License.
