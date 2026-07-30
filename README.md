# Employee Management System

A full-stack Employee Management System built with **.NET 8 Web API** (Clean Architecture) and **Angular 22** (standalone, zoneless).

See [REQUIREMENT_ANALYSIS.md](REQUIREMENT_ANALYSIS.md) and [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) for the full requirement breakdown and phased implementation plan, and [AI_USAGE_LOG.md](AI_USAGE_LOG.md) for a record of AI-assisted development on this project.

## Tech Stack

- **Backend:** .NET 8 Web API, Clean Architecture (Domain / Application / Infrastructure / API), EF Core, SQL Server, JWT auth with refresh tokens, Serilog, Asp.Versioning, rate limiting, response caching, health checks, Swagger.
- **Frontend:** Angular 22 (standalone components, zoneless change detection).
- **Database:** SQL Server (via Docker, LocalDB, or a full SQL Server instance).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (pinned via [global.json](global.json))
- [Node.js 20+](https://nodejs.org/) and npm
- SQL Server instance — either:
  - Docker Desktop (recommended, uses [docker-compose.yml](docker-compose.yml)), or
  - A local SQL Server / LocalDB instance

## Getting Started

### 1. Start the database (Docker)

```powershell
docker compose up -d sqlserver
```

This starts SQL Server 2022 on `localhost:1433` (sa / `Your_password123`, matching `docker-compose.yml`).

> Alternatively, update `ConnectionStrings:DefaultConnection` in [src/EmployeeManagement.API/appsettings.json](src/EmployeeManagement.API/appsettings.json) (or `appsettings.Development.json`) to point at your own SQL Server / LocalDB instance.

### 2. Apply the database schema

From the repository root:

```powershell
dotnet ef database update `
  --project src/EmployeeManagement.Infrastructure/EmployeeManagement.Infrastructure.csproj `
  --startup-project src/EmployeeManagement.API/EmployeeManagement.API.csproj
```

This runs all EF Core migrations (schema + seed data for permissions/roles/admin user). A raw SQL equivalent is also available under [src/EmployeeManagement.Infrastructure/Persistence/Scripts](src/EmployeeManagement.Infrastructure/Persistence/Scripts) if you prefer to apply it manually.

### 3. Configure the JWT signing key (TD-02)

The `Jwt:SecretKey` in `appsettings.json` is a placeholder and **must not be used as-is**. Set it locally via .NET user secrets:

```powershell
dotnet user-secrets init --project src/EmployeeManagement.API/EmployeeManagement.API.csproj
dotnet user-secrets set "Jwt:SecretKey" "$(New-Guid)-$(New-Guid)-$(New-Guid)" `
  --project src/EmployeeManagement.API/EmployeeManagement.API.csproj
```

For deployments, inject it via the `Jwt__SecretKey` environment variable or a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.). The API will refuse to start in Production with a weak or placeholder key.

### 4. Run the backend API

```powershell
dotnet run --project src/EmployeeManagement.API/EmployeeManagement.API.csproj
```

- API: `https://localhost:7259` (and `http://localhost:5164`)
- Swagger UI: `https://localhost:7259/swagger`
- Health check: `https://localhost:7259/health`

Default seeded login: **admin / Admin@123**

### 5. Run the frontend

```powershell
cd client
npm install
npm start
```

The Angular dev server runs on `http://localhost:4200` and proxies `/api` requests to `https://localhost:7259` (see [client/proxy.conf.json](client/proxy.conf.json)). Log in with the seeded admin credentials above.

## Environment Variables Reference

| Variable | Purpose | Required in Production |
|---|---|---|
| `Jwt__SecretKey` | JWT HMAC-SHA256 signing key (min 32 chars, random) | ✅ |
| `Seed__AdminPassword` | Override the default admin seed password | ✅ |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | ✅ |
| `Cors__AllowedOrigins__0` | First allowed CORS origin (e.g. `https://app.example.com`) | ✅ |

## Running with Docker Compose (API + SQL Server)

```powershell
docker compose up --build
```

This builds and runs both the API (`http://localhost:8080`) and SQL Server. Run the Angular client separately with `npm start` from `client/` (update the proxy target to `http://localhost:8080` if using this mode).

## Running Tests

```powershell
dotnet test tests/EmployeeManagement.Tests/EmployeeManagement.Tests.csproj
```

## Project Structure

```
src/
  EmployeeManagement.Domain/         # Entities, enums, base types
  EmployeeManagement.Application/    # Interfaces, DTOs, constants
  EmployeeManagement.Infrastructure/ # EF Core, auth, persistence, migrations
  EmployeeManagement.API/            # Controllers, middleware, Program.cs
tests/
  EmployeeManagement.Tests/          # Unit tests
client/                              # Angular 22 frontend
```

## Key Features

- JWT authentication with refresh token rotation, account lockout, password expiry, and forgot/reset/change password flows
- Role & permission-based authorization (policy-based)
- Employee, Department, User, Role, Settings, Audit Log, and Dashboard modules
- Report exports (CSV, Excel, PDF)
- Employee photo upload
- Global exception handling, Serilog logging, API versioning, rate limiting, response caching, health checks
- Docker support for the API and SQL Server
