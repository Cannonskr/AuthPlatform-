# Auth Platform — Deployment Guide

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Quick Start (Development)](#2-quick-start-development)
3. [Environment Configuration](#3-environment-configuration)
4. [Docker Deployment](#4-docker-deployment)
5. [Production Deployment](#5-production-deployment)
6. [JWT Key Management](#6-jwt-key-management)
7. [Database Migrations & Seed](#7-database-migrations--seed)
8. [Nginx Configuration](#8-nginx-configuration)
9. [SSL/TLS Setup](#9-ssltls-setup)
10. [Monitoring & Health Checks](#10-monitoring--health-checks)
11. [Backup & Restore](#11-backup--restore)
12. [Scaling](#12-scaling)
13. [Troubleshooting](#13-troubleshooting)

---

## 1. Prerequisites

### Required Software

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | Build and run |
| [Docker](https://docs.docker.com/get-docker/) | 24+ | Containerization |
| [Docker Compose](https://docs.docker.com/compose/install/) | v2+ | Multi-container orchestration |
| [MySQL](https://dev.mysql.com/downloads/mysql/) | 8.0+ | Database (optional for local dev) |
| [Redis](https://redis.io/download/) | 7+ | Caching (optional for local dev) |
| [OpenSSL](https://www.openssl.org/) | 3.0+ | JWT key generation |

### Verify Installation

```bash
dotnet --version            # Should output 10.x
docker --version            # Should output 24.x+
docker compose version      # Should output v2.x+
openssl version             # Should output 3.x
```

---

## 2. Quick Start (Development)

### 2.1 Clone & Build

```bash
git clone <repository-url> auth-platform
cd auth-platform

# Restore and build
dotnet restore
dotnet build --no-restore
```

### 2.2 Generate Development JWT Keys

```bash
# Generate RSA key pair for JWT signing
mkdir -p keys
openssl genpkey -algorithm RSA -out keys/private.pem -pkeyopt rsa_keygen_bits:4096
openssl pkey -in keys/private.pem -pubout -out keys/public.pem

# (Optional) Encode as base64 for environment variable usage
base64 -i keys/private.pem | tr -d '\n' > keys/private-base64.txt
base64 -i keys/public.pem | tr -d '\n' > keys/public-base64.txt
```

### 2.3 Run with Docker Compose (Recommended)

```bash
# Start all services
docker compose up -d

# View logs
docker compose logs -f auth-api

# Check status
docker compose ps
```

This starts:
- **auth-api** — The .NET API (port 5000 HTTP, 5001 HTTPS)
- **mysql** — MySQL 8.0 database (port 3306)
- **redis** — Redis 7 for caching (port 6379)
- **nginx** — Reverse proxy (port 80, 443)

### 2.4 Run Locally (Without Docker)

```bash
# Ensure MySQL and Redis are running locally

# Update connection string in appsettings.Development.json if needed
#   Default: Server=localhost;Port=3306;Database=auth_platform_dev;User=root;Password=root;

# Run database migrations
dotnet ef database update --project src/Auth.Persistence --startup-project src/Auth.Api

# Run the API
dotnet run --project src/Auth.Api
```

The API will be available at:
- **HTTP:** `http://localhost:5000`
- **Swagger UI:** `http://localhost:5000/swagger`
- **Health Check:** `http://localhost:5000/health`

### 2.5 Seed Data

Database migrations and seed data are applied automatically on startup via `ApplicationBuilderExtensions.InitializeDatabaseAsync()`.

Default seed credentials:

| Username | Password | Role |
|----------|----------|------|
| `admin` | `Admin@123` | Admin (full access) |
| `viewer` | `Viewer@123` | Viewer (read-only) |

---

## 3. Environment Configuration

### 3.1 Configuration Sources

The application loads configuration in the following order (later sources override earlier ones):

1. `appsettings.json` (base)
2. `appsettings.{Environment}.json` (environment-specific)
3. Environment variables
4. Docker secrets (production)

### 3.2 All Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | `Development`, `Staging`, `Production` |
| `ASPNETCORE_URLS` | `http://+:80;https://+:443` | Listening URLs |
| `ConnectionStrings__DefaultConnection` | *(see appsettings.json)* | MySQL connection string |
| `JwtSettings__Issuer` | `auth-platform` | JWT issuer |
| `JwtSettings__AccessTokenExpirationMinutes` | `15` | Access token TTL |
| `JwtSettings__PrivateKeyPath` | — | Path to RSA private key PEM file |
| `JwtSettings__PublicKeyPath` | — | Path to RSA public key PEM file |
| `JwtSettings__PrivateKey` | — | Base64-encoded RSA private key (alternative to file) |
| `JwtSettings__PublicKey` | — | Base64-encoded RSA public key (alternative to file) |
| `RefreshTokenSettings__RefreshTokenExpirationDays` | `7` | Refresh token TTL |
| `Cors__AllowedOrigins__0` | *(see appsettings.json)* | Allowed CORS origins |
| `RateLimiting__LoginMaxAttempts` | `5` | Max failed login attempts before lockout |
| `RateLimiting__LoginWindowMinutes` | `15` | Lockout duration (minutes) |
| `MYSQL_ROOT_PASSWORD` | `root` | MySQL root password |
| `SSL_PASSWORD` | `password` | SSL certificate password |
| `REDIS_CONNECTION_STRING` | `localhost:6379` | Redis connection string |

> **Note:** Use double underscores (`__`) for nested configuration keys in environment variables (e.g., `JwtSettings__Issuer`).

### 3.3 Docker Compose Environment Variables

Create a `.env` file in the project root:

```bash
# .env
MYSQL_ROOT_PASSWORD=YourStrongPassword123!
SSL_PASSWORD=YourSslPassword
```

The `.env` file is automatically loaded by Docker Compose.

---

## 4. Docker Deployment

### 4.1 Building the Docker Image

```bash
# Build the API image
docker build -t auth-platform-api:latest -f src/Auth.Api/Dockerfile .

# Build with a specific tag
docker build -t auth-platform-api:v1.0.0 -f src/Auth.Api/Dockerfile .
```

### 4.2 Docker Compose Profiles

```bash
# Full stack (default)
docker compose up -d

# API + Database only (no nginx, no redis)
docker compose up -d auth-api mysql

# Database only
docker compose up -d mysql

# Production stack (overrides)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### 4.3 Custom Docker Compose Override

Create `docker-compose.override.yml` for local customizations:

```yaml
version: '3.8'
services:
  auth-api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - JwtSettings__PrivateKeyPath=/app/keys/private.pem
      - JwtSettings__PublicKeyPath=/app/keys/public.pem
    volumes:
      - ./keys:/app/keys:ro
```

### 4.4 Stopping & Cleaning

```bash
# Stop all services
docker compose down

# Stop and remove volumes (destroys database data)
docker compose down -v

# Rebuild and restart
docker compose up -d --build
```

---

## 5. Production Deployment

### 5.1 Production Checklist

- [ ] **JWT Keys**: Generate production RSA keys (4096-bit minimum)
- [ ] **Passwords**: Change all default passwords
- [ ] **SSL/TLS**: Configure HTTPS with a valid certificate
- [ ] **Database**: Use a managed MySQL instance or configure replication
- [ ] **Redis**: Enable authentication and TLS for Redis
- [ ] **CORS**: Restrict `AllowedOrigins` to known client domains
- [ ] **Rate Limiting**: Configure appropriate rate limits
- [ ] **Logging**: Set up centralized logging (ELK, Seq, etc.)
- [ ] **Backups**: Configure automated database backups
- [ ] **Monitoring**: Set up health checks and alerting

### 5.2 Server Requirements

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU | 2 cores | 4+ cores |
| RAM | 4 GB | 8+ GB |
| Disk | 20 GB SSD | 50+ GB SSD |
| OS | Ubuntu 22.04+ / RHEL 9+ | Same |

### 5.3 Environment-Specific Settings

**`appsettings.Staging.json`:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "JwtSettings": {
    "AccessTokenExpirationMinutes": 30
  },
  "Cors": {
    "AllowedOrigins": [
      "https://staging.admin.authplatform.com",
      "https://staging.app.authplatform.com"
    ]
  }
}
```

**Production Configuration** (via environment variables or Docker secrets):

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="Server=prod-db.example.com;Port=3306;Database=auth_platform;User=app_user;Password=<strong-password>;SslMode=Required;"
JwtSettings__Issuer=auth-platform
JwtSettings__AccessTokenExpirationMinutes=15
```

### 5.4 Docker Compose Production Override

Create `docker-compose.prod.yml`:

```yaml
version: '3.8'
services:
  auth-api:
    restart: always
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 1G
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    secrets:
      - jwt_private_key
      - jwt_public_key

  mysql:
    restart: always
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 4G
    volumes:
      - mysql_data:/var/lib/mysql
      - ./docker/mysql/my.cnf:/etc/mysql/conf.d/my.cnf:ro

  redis:
    restart: always
    environment:
      - REDIS_PASSWORD=<redis-password>

secrets:
  jwt_private_key:
    file: ./secrets/jwt_private.pem
  jwt_public_key:
    file: ./secrets/jwt_public.pem
```

---

## 6. JWT Key Management

### 6.1 Key Generation

For **development**, use a single RSA key pair (both private and public):

```bash
# Generate 4096-bit RSA private key
openssl genpkey -algorithm RSA -out keys/private.pem -pkeyopt rsa_keygen_bits:4096

# Extract public key
openssl pkey -in keys/private.pem -pubout -out keys/public.pem

# Secure permissions
chmod 600 keys/private.pem
chmod 644 keys/public.pem
```

### 6.2 Key Rotation

> **Recommendation**: Rotate JWT signing keys every 90 days.

```bash
# 1. Generate new key pair
openssl genpkey -algorithm RSA -out keys/private-v2.pem -pkeyopt rsa_keygen_bits:4096
openssl pkey -in keys/private-v2.pem -pubout -out keys/public-v2.pem

# 2. Deploy new key alongside existing keys
# 3. Update application configuration to reference new key
# 4. Verify tokens issued with old key are rejected
# 5. Remove old key files
```

### 6.3 Docker Secrets (Production)

Mount JWT keys as Docker secrets instead of environment variables:

```yaml
# docker-compose.yml
services:
  auth-api:
    secrets:
      - jwt_private_key
      - jwt_public_key

secrets:
  jwt_private_key:
    file: ./secrets/jwt_private.pem
  jwt_public_key:
    file: ./secrets/jwt_public.pem
```

In `appsettings.Production.json`, reference the secret files:

```json
{
  "JwtSettings": {
    "PrivateKeyPath": "/run/secrets/jwt_private_key",
    "PublicKeyPath": "/run/secrets/jwt_public_key"
  }
}
```

---

## 7. Database Migrations & Seed

### 7.1 Running Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
    --project src/Auth.Persistence \
    --startup-project src/Auth.Api

# Apply migrations
dotnet ef database update \
    --project src/Auth.Persistence \
    --startup-project src/Auth.Api

# Generate SQL script
dotnet ef migrations script \
    --project src/Auth.Persistence \
    --startup-project src/Auth.Api \
    -o migrations.sql

# Remove last migration (if not applied)
dotnet ef migrations remove \
    --project src/Auth.Persistence \
    --startup-project src/Auth.Api
```

### 7.2 Automatic Migration on Startup

In production, migrations are applied automatically via `InitializeDatabaseAsync()` in `Program.cs`. To disable this behavior:

```csharp
// Program.cs - Remove or comment out the migration call
// await app.InitializeDatabaseAsync();
```

And use a separate deployment step:

```bash
dotnet ef database update \
    --project src/Auth.Persistence \
    --startup-project src/Auth.Api \
    --connection "Server=...;Database=...;User=...;Password=...;"
```

### 7.3 Seed Data

Seed data is applied automatically on first run. The seeder checks for existing data before inserting:

- **Idempotent**: Safe to run multiple times
- **Transactional**: All seed operations roll back on failure
- **Deterministic GUIDs**: Stable IDs for testing and reference

To re-seed (e.g., after schema changes):

```sql
-- Manually truncate seed tables (development only!)
TRUNCATE TABLE UserRoles;
TRUNCATE TABLE RolePermissions;
TRUNCATE TABLE Users;
TRUNCATE TABLE Roles;
TRUNCATE TABLE Permissions;
TRUNCATE TABLE Applications;
```

Then restart the application.

### 7.4 Database Backup

```bash
# Using docker exec
docker exec auth-platform-mysql mysqldump \
    -u root -p${MYSQL_ROOT_PASSWORD} \
    --databases auth_platform \
    --routines --triggers --events \
    > backup_$(date +%Y%m%d_%H%M%S).sql

# Using mysqldump directly (local MySQL)
mysqldump \
    -h localhost \
    -u root -p \
    --databases auth_platform \
    --routines --triggers --events \
    --single-transaction \
    > backup_$(date +%Y%m%d_%H%M%S).sql
```

### 7.5 Database Restore

```bash
# Restore from backup
docker exec -i auth-platform-mysql mysql \
    -u root -p${MYSQL_ROOT_PASSWORD} \
    auth_platform < backup_20260401_120000.sql
```

---

## 8. Nginx Configuration

### 8.1 Default Configuration

The default `docker/nginx/nginx.conf` provides:

- Reverse proxy to the API container
- Header forwarding (X-Real-IP, X-Forwarded-For, X-Forwarded-Proto)
- Rate limiting (100 requests/second per IP)
- Dedicated `/swagger` and `/health` locations

### 8.2 Production Nginx (with SSL)

```nginx
upstream auth_api {
    server auth-api:80;
    keepalive 64;
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name auth.example.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name auth.example.com;

    # SSL certificates
    ssl_certificate     /etc/nginx/ssl/fullchain.pem;
    ssl_certificate_key /etc/nginx/ssl/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    ssl_session_cache   shared:SSL:10m;
    ssl_session_timeout 10m;

    # HSTS
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Rate limiting
    limit_req zone=api burst=50 nodelay;
    limit_req_status 429;

    location / {
        proxy_pass http://auth_api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "keep-alive";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 30s;
        proxy_connect_timeout 10s;
    }

    location /health {
        proxy_pass http://auth_api;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        access_log off;
    }

    # Deny access to internal paths
    location ~ /\. {
        deny all;
        access_log off;
        log_not_found off;
    }
}
```

### 8.3 Rate Limiting

| Zone | Rate | Burst | Purpose |
|------|------|-------|---------|
| `api` | 100 req/s | 50 | General API rate limit |
| `login` | 5 req/min | 5 | Login endpoint (configured in app) |

Configurable via `appsettings.json`:

```json
{
  "RateLimiting": {
    "LoginMaxAttempts": 5,
    "LoginWindowMinutes": 15
  }
}
```

---

## 9. SSL/TLS Setup

### 9.1 Development Certificate

```bash
# Generate self-signed certificate for local development
dotnet dev-certs https --trust

# Export as PFX for Docker
dotnet dev-certs https -ep ~/.aspnet/https/aspnetapp.pfx -p <password>
```

The PFX file is mounted into the Docker container:

```yaml
# docker-compose.yml
volumes:
  - ~/.aspnet/https:/https:ro
```

### 9.2 Production Certificate (Let's Encrypt)

```bash
# Install certbot
sudo apt-get install certbot

# Obtain certificate
sudo certbot certonly --standalone -d auth.example.com

# Certificate location:
#   /etc/letsencrypt/live/auth.example.com/fullchain.pem
#   /etc/letsencrypt/live/auth.example.com/privkey.pem

# Mount into Nginx container
volumes:
  - /etc/letsencrypt:/etc/nginx/ssl:ro
```

### 9.3 Auto-Renewal (Let's Encrypt)

```bash
# Test renewal
sudo certbot renew --dry-run

# Auto-renew via cron (weekly)
echo "0 3 * * 0 certbot renew --quiet && docker compose restart nginx" | sudo crontab -
```

---

## 10. Monitoring & Health Checks

### 10.1 Health Endpoint

The API exposes a health check at `/health`.

```bash
# Check container health
curl http://localhost:5000/health
# Response: 200 OK

# Check via Docker
docker compose ps
```

### 10.2 Docker Health Checks

MySQL health check is configured in `docker-compose.yml`:

```yaml
healthcheck:
  test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
  timeout: 20s
  retries: 10
```

### 10.3 Logging

| Environment | Log Level | Output |
|-------------|-----------|--------|
| Development | Debug | Console (stdout) |
| Staging | Information | Console + File |
| Production | Warning | Console + Centralized logging |

Configure log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Auth.Infrastructure.Services.JwtService": "Warning"
    }
  }
}
```

### 10.4 Docker Logging (Production)

```yaml
# docker-compose.prod.yml
services:
  auth-api:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

---

## 11. Backup & Restore

### 11.1 Automated Database Backups

Create a backup script `scripts/backup-db.sh`:

```bash
#!/bin/bash
BACKUP_DIR="/backups/mysql"
RETENTION_DAYS=30
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

mkdir -p "$BACKUP_DIR"

docker exec auth-platform-mysql mysqldump \
    -u root -p${MYSQL_ROOT_PASSWORD} \
    --databases auth_platform \
    --routines --triggers --events \
    --single-transaction \
    --quick \
    | gzip > "$BACKUP_DIR/auth_platform_$TIMESTAMP.sql.gz"

# Remove backups older than retention period
find "$BACKUP_DIR" -name "*.sql.gz" -mtime +$RETENTION_DAYS -delete

echo "Backup completed: auth_platform_$TIMESTAMP.sql.gz"
fi
```

### 11.2 Cron Job Setup

```bash
# Add to crontab (daily at 2 AM)
0 2 * * * /path/to/auth-platform/scripts/backup-db.sh
```

### 11.3 What to Backup

| Data | Method | Frequency |
|------|--------|-----------|
| MySQL database | `mysqldump` | Daily |
| JWT keys (private) | File copy | On generation |
| Configuration files | Git | Via CI/CD |
| Docker volumes | Volume snapshot | Weekly |

---

## 12. Scaling

### 12.1 Horizontal Scaling

The API is stateless (JWT-based auth), making it suitable for horizontal scaling:

```yaml
# docker-compose.prod.yml
services:
  auth-api:
    deploy:
      replicas: 3
      resources:
        limits:
          cpus: '2'
          memory: 2G
```

### 12.2 Load Balancer (Nginx)

```nginx
upstream auth_api_cluster {
    least_conn;
    server auth-api-1:80 weight=3;
    server auth-api-2:80 weight=2;
    server auth-api-3:80 weight=2;
    keepalive 64;
}
```

### 12.3 Session State

Authentication is **stateless** via JWT. No session affinity required.

For token blacklisting across instances, Redis must be shared:

```json
{
  "Redis": {
    "ConnectionString": "redis-cluster.example.com:6379,password=..."
  }
}
```

### 12.4 Database Scaling

- **Read replicas** — Offload read queries to replicas
- **Connection pooling** — Configure `Max Pool Size` in connection string
- **Sharding** — Future support via multi-tenant architecture

---

## 13. Troubleshooting

### 13.1 Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| `JWT private key must be configured` | Missing or invalid JWT keys | Generate RSA keys and set `PrivateKeyPath` or `PrivateKey` |
| `Cannot resolve service for ApplicationDbContext` | Missing DB connection | Check `ConnectionStrings:DefaultConnection` |
| `Unable to connect to MySQL` | DB not running or wrong connection string | Verify MySQL is up and connection string is correct |
| `Build failed` on Docker | Missing packages or SDK version | Use `docker compose build --no-cache` |
| `401 Unauthorized` on API calls | Missing or expired JWT token | Get a fresh token via `/api/auth/login` |
| `Swagger UI` not loading | Not in Development mode | Set `ASPNETCORE_ENVIRONMENT=Development` or remove the condition in `Program.cs` |

### 13.2 Debugging Commands

```bash
# Check container logs
docker compose logs -f auth-api

# Inspect a running container
docker exec -it auth-platform-api bash

# Check environment variables
docker exec auth-platform-api printenv

# Test database connection
docker exec auth-platform-mysql mysql -u root -p${MYSQL_ROOT_PASSWORD} -e "SHOW DATABASES;"

# Ping the API
curl -v http://localhost:5000/health

# Check Redis connectivity
docker exec auth-platform-redis redis-cli ping

# View running containers resource usage
docker stats
```

### 13.3 Health Check Commands

```bash
# Full health check
curl -s http://localhost:5000/health

# API version
curl -s http://localhost:5000/api/health/version

# Database connectivity
curl -s http://localhost:5000/api/health/database
```

---

## Appendix

### A. Useful Commands

```bash
# === Docker ===
docker compose ps                    # List running services
docker compose logs -f --tail=100    # View last 100 log lines
docker compose restart auth-api      # Restart a single service
docker compose down -v               # Stop and remove volumes
docker system prune -a               # Clean up unused Docker resources

# === .NET ===
dotnet build                        # Build solution
dotnet test                         # Run tests
dotnet ef migrations list           # List pending migrations
dotnet ef database update           # Apply pending migrations

# === MySQL ===
docker exec -it auth-platform-mysql mysql -u root -p
SHOW DATABASES;
USE auth_platform;
SHOW TABLES;
SELECT * FROM Users;
EXIT;

# === Redis ===
docker exec -it auth-platform-redis redis-cli
KEYS *
GET <key>
TTL <key>
EXIT;
```

### B. File Reference

| File | Purpose |
|------|---------|
| `Dockerfile` | .NET API container image |
| `docker-compose.yml` | Local development stack |
| `docker-compose.prod.yml` | Production stack overrides |
| `.env` | Docker Compose environment variables |
| `docker/nginx/nginx.conf` | Nginx reverse proxy configuration |
| `docker/mysql/my.cnf` | MySQL server configuration |
| `docker/mysql/init.sql` | MySQL initialization script |
| `docs/ARCHITECTURE.md` | Architecture overview |
| `docs/DEPLOYMENT.md` | **This document** |

### C. Security Checklist

- [ ] JWT keys are **not** stored in source control
- [ ] Database credentials are **not** hardcoded in source
- [ ] Production passwords are stored in Docker secrets or vault
- [ ] HTTPS is enforced in production
- [ ] CORS origins are restricted to known domains
- [ ] Rate limiting is configured
- [ ] Account lockout is enabled
- [ ] Audit logging is active
- [ ] Regular key rotation is scheduled
- [ ] Database backups are automated
