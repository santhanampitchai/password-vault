# 🔐 Password Vault

A full-stack, production-ready **secure password manager** with **End-to-End Encryption (E2EE)**.  
Built with **Angular 19 + ASP.NET Core .NET 10 + SQL Server**.

---

## Architecture Overview

```
password-vault/
├── backend/
│   ├── PasswordVault.Domain/          # Entities, interfaces (no dependencies)
│   ├── PasswordVault.Application/     # DTOs, service interfaces, business logic
│   ├── PasswordVault.Infrastructure/  # EF Core, repositories, JWT, AES encryption
│   ├── PasswordVault.API/             # Controllers, middleware, Program.cs
│   └── PasswordVault.Tests/           # xUnit + Moq unit tests
├── frontend/
│   └── password-vault-ui/             # Angular 19 standalone components
├── scripts/
│   ├── 01_create_database.sql         # DB + tables creation
│   └── 02_seed_data.sql               # Demo data
├── docker-compose.yml
└── README.md
```

---

## Security Design

| Layer | Mechanism |
|-------|-----------|
| Client-side encryption | AES-256-CBC via Web Crypto API before any HTTP request |
| Transport | HTTPS + JWT Bearer tokens |
| Server-side re-encryption | AES-256-CBC (double-layer, defence in depth) |
| Password storage | PBKDF2-SHA512, 350,000 iterations, random 32-byte salt |
| JWT | HMAC-SHA512, 1-hour expiry + refresh token |
| Headers | CSP, X-Frame-Options, HSTS, Permissions-Policy |
| Rate limiting | 20 auth requests / 15 minutes |
| Audit logging | Every password reveal is logged (Serilog structured log) |

> **Plain-text passwords never leave the browser** and are never stored anywhere on the server.

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0+ |
| Node.js | 22+ |
| Angular CLI | 19+ (`npm i -g @angular/cli`) |
| SQL Server | 2019+ or Azure SQL |
| Docker (optional) | 24+ |

---

## Quick Start (Local Development)

### 1 – Database

```sql
-- Run in SSMS or sqlcmd:
sqlcmd -S localhost -E -i scripts/01_create_database.sql
```

Or let Entity Framework auto-migrate on first API run (development mode).

### 2 – Backend API

```bash
cd backend

# Set secrets (never commit these!)
dotnet user-secrets set "Security:EncryptionKey"  "YourExact32CharKeyHere!!!!!!!!!!"  --project PasswordVault.API
dotnet user-secrets set "Security:JwtSecret"      "YourVeryLongJwtSecretAtLeast64Chars!!!!!!!!!!!!!!!!!!!!!!"  --project PasswordVault.API

# Run
dotnet run --project PasswordVault.API
# API at: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### 3 – Frontend

```bash
cd frontend/password-vault-ui
npm install
ng serve
# App at: http://localhost:4200
```

---

## Docker (Full Stack)

```bash
# ⚠️  Edit docker-compose.yml and set strong values for:
#   Security__EncryptionKey
#   Security__JwtSecret
#   SA_PASSWORD

docker compose up --build -d

# App:    http://localhost:4200
# API:    http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

---

## Environment Variables (Production)

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Security__EncryptionKey` | **32-char** AES server key |
| `Security__JwtSecret` | **64+ char** JWT signing key |
| `Security__JwtIssuer` | JWT issuer (default: `PasswordVaultAPI`) |
| `Security__JwtAudience` | JWT audience (default: `PasswordVaultClient`) |
| `Security__JwtExpiryMinutes` | Token lifetime in minutes (default: 60) |
| `AllowedOrigins__0` | Angular app origin e.g. `https://app.yourdomain.com` |

> Use **Azure Key Vault**, **AWS Secrets Manager**, or **dotnet user-secrets** — never commit secrets to source control.

---

## API Reference

### Auth

| Method | Endpoint | Auth |
|--------|----------|------|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/refresh-token` | Public |
| POST | `/api/auth/logout` | Bearer |

### Accounts

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts` | Paginated list (search, sort, filter) |
| GET | `/api/accounts/{id}` | Get by ID (no password payload) |
| POST | `/api/accounts` | Create (client-encrypted payload) |
| PUT | `/api/accounts/{id}` | Update |
| DELETE | `/api/accounts/{id}` | Delete |
| POST | `/api/accounts/{id}/decrypt-password` | Unwrap server layer → client decrypts |

All account endpoints require `Authorization: Bearer <token>`.

---

## Running Tests

```bash
# .NET tests
cd backend
dotnet test --logger "console;verbosity=normal"

# Angular tests
cd frontend/password-vault-ui
ng test --watch=false --browsers=ChromeHeadless
```

---

## IIS Deployment

1. Publish the API:
   ```bash
   dotnet publish PasswordVault.API -c Release -o ./publish
   ```
2. Create an IIS Application Pool (.NET CLR: No Managed Code)
3. Point site to `./publish` folder
4. Set environment variables in IIS → Application Pool → Advanced Settings → Environment Variables
5. Install [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download)

For the Angular app, build and deploy to any static host:
```bash
ng build --configuration production
# Copy dist/password-vault-ui/browser/ to IIS wwwroot
```

---

## Folder Structure Detail

```
PasswordVault.Domain/
  Entities/
    User.cs           – User entity
    Account.cs        – Account credential entity
  Interfaces/
    IRepositories.cs  – IUserRepository, IAccountRepository, IUnitOfWork

PasswordVault.Application/
  DTOs/Dtos.cs        – All request/response records
  Interfaces/         – IAuthService, IAccountService, IEncryptionService…
  Services/
    AuthService.cs    – Register, login, refresh token
    AccountService.cs – CRUD, pagination, filtering

PasswordVault.Infrastructure/
  Data/
    AppDbContext.cs           – EF Core DbContext with Fluent API config
    Repositories/             – UserRepository, AccountRepository, UnitOfWork
    Migrations/               – EF Core migration files
  Security/
    CryptoServices.cs         – AES-256 encryption + PBKDF2 password hasher
    JwtTokenService.cs        – JWT generation & validation

PasswordVault.API/
  Controllers/
    AuthController.cs         – Auth endpoints
    AccountsController.cs     – Account CRUD + decrypt
  Middleware/
    GlobalExceptionMiddleware – Structured error responses
  Program.cs                  – DI, JWT, CORS, rate limiting, Swagger

frontend/src/app/
  core/
    services/
      auth.service.ts         – Login, register, JWT storage
      account.service.ts      – CRUD + client-side encrypt/decrypt
      encryption.service.ts   – AES-256-CBC via Web Crypto API
      toast.service.ts        – Snackbar notifications
      theme.service.ts        – Dark/light theme toggle
    interceptors/
      jwt.interceptor.ts      – Attach Bearer token + auto-refresh
    guards/
      auth.guard.ts           – Route protection
  features/
    auth/login/               – Login form component
    auth/register/            – Register form + password strength
    accounts/account-list/    – Data table + reveal/copy/delete
    accounts/account-form/    – Add/edit form + password generator
  shared/
    components/shell/         – Sidebar layout + theme toggle
    components/confirm-dialog/ – Delete confirmation dialog
```

---

## License

MIT
"# password-vault" 
