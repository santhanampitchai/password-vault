# 🔐 Password Vault

A full-stack, production-ready **secure password manager** with **End-to-End Encryption (E2EE)**.  
Built with **Angular 19 + ASP.NET Core .NET 10 + SQL Server 2022**.

---

## Architecture Overview

```
password-vault/
├── backend/
│   ├── PasswordVault.Domain/          # Entities, interfaces (no dependencies)
│   ├── PasswordVault.Application/     # DTOs, service interfaces, business logic
│   ├── PasswordVault.Infrastructure/  # EF Core, repositories, JWT, AES encryption
│   ├── PasswordVault.API/             # Controllers, middleware, Program.cs
│   ├── PasswordVault.Tests/           # xUnit + Moq unit tests
│   ├── .dockerignore                  # Excludes bin/obj from Docker build context
│   └── Dockerfile                     # Multi-stage build (.NET SDK → ASP.NET runtime)
├── frontend/
│   └── password-vault-ui/             # Angular 19 standalone components
│       └── Dockerfile                 # Multi-stage build (Node → Nginx)
├── scripts/
│   ├── 01_create_database.sql         # DB + tables creation
│   └── 02_seed_data.sql               # Demo data
├── .env.example                       # Secret variable template (safe to commit)
├── .env                               # Your local secrets (gitignored — never commit)
├── docker-compose.yml                 # Orchestrates all 3 containers
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
| JWT | HMAC-SHA512, minimum 64-char key, 1-hour expiry + refresh token |
| Headers | CSP, X-Frame-Options, HSTS, Permissions-Policy |
| Rate limiting | 20 auth requests / 15 minutes |
| Audit logging | Every password reveal is logged (Serilog structured log) |
| Non-root container | API runs as `$APP_UID` — never as root inside Docker |

> **Plain-text passwords never leave the browser** and are never stored anywhere on the server.

---

## Prerequisites

### For Docker (Recommended)

| Tool | Version | Notes |
|------|---------|-------|
| Docker Desktop | 24+ | Requires WSL 2 on Windows |
| WSL 2 | Latest | Windows only — `wsl --install` |

### For Local Development (Without Docker)

| Tool | Version |
|------|---------|
| .NET SDK | 10.0+ |
| Node.js | 22+ |
| Angular CLI | 19+ (`npm i -g @angular/cli`) |
| SQL Server | 2019+ or Azure SQL |

---

## Running with Docker (Recommended)

Docker is the easiest way to run the full stack — no local .NET, Node, or SQL Server install needed.

### Step 1 — Create your secrets file

```bash
# Copy the template
cp .env.example .env
```

Open `.env` and fill in your own values:

```env
# SQL Server SA password — must include upper, lower, number, symbol
SA_PASSWORD=YourStrong!Passw0rd

# AES-256 server-side encryption key — EXACTLY 32 characters
ENCRYPTION_KEY=YourExact32CharKeyHereABCDEFGHIJ

# JWT signing key — AT LEAST 64 characters (512 bits for HMAC-SHA512)
JWT_SECRET=YourVeryLongJwtSecretKeyThatIsAtLeast64CharactersLongForSecurity!!
```

> ⚠️ `.env` is gitignored — it will never be committed. Never share or commit this file.

### Step 2 — Build and start all containers

```bash
docker compose up --build -d
```

This starts **3 containers**:

```
Your Browser
    │
    ├─► http://localhost:4200  →  [pv-frontend]  Angular app (Nginx)
    │
    └─► http://localhost:5000  →  [pv-api]       .NET 10 API
                                       │
                                       └─► [pv-sqlserver]  SQL Server 2022
```

> First run takes **5–10 minutes** — Docker downloads SQL Server (~1.5 GB), compiles the .NET API, and builds the Angular app.  
> Subsequent runs start in **under 30 seconds**.

### Step 3 — Verify all containers are running

```bash
docker compose ps
```

Expected output:
```
NAME            STATUS
pv-sqlserver    Up (healthy)
pv-api          Up
pv-frontend     Up
```

### Step 4 — Open the app

| What | URL |
|------|-----|
| 🌐 Angular App | http://localhost:4200 |
| 📋 API Swagger | http://localhost:5000/swagger |
| ❤️ Health Check | http://localhost:5000/health |

---

## Secret Management

Secrets are **never hardcoded** in any committed file. They flow like this:

```
.env  (gitignored)
  │
  └─► docker-compose.yml  reads ${SA_PASSWORD}, ${ENCRYPTION_KEY}, ${JWT_SECRET}
            │
            └─► API container environment variables
                      │
                      └─► ASP.NET Core Configuration → DI → Services
```

| File | Committed? | Purpose |
|------|-----------|---------|
| `.env` | ❌ Never | Your real local secrets |
| `.env.example` | ✅ Yes | Template showing required variables |
| `docker-compose.yml` | ✅ Yes | References `${VARIABLE}` — no hardcoded values |
| `appsettings.json` | ✅ Yes | Non-secret defaults and placeholders only |
| `appsettings.Development.json` | ❌ Never | Local dev overrides (gitignored) |

---

## Handy Docker Commands

```bash
# Start (after first run — much faster, no rebuild)
docker compose up -d

# Stop containers (data preserved in volume)
docker compose down

# Stop AND delete all data (wipes the database)
docker compose down -v

# Rebuild a single container after a code change
docker compose up --build api -d
docker compose up --build frontend -d

# Force recreate a container to pick up new .env values
docker compose up --force-recreate api -d

# Watch live logs
docker compose logs -f

# Watch logs for one container
docker compose logs -f api
docker compose logs -f frontend
docker compose logs -f sqlserver
```

---

## Running Locally (Without Docker)

### 1 — Database

```bash
# Run in SSMS or sqlcmd:
sqlcmd -S localhost -E -i scripts/01_create_database.sql
```

Or simply start the API — the database and tables are created automatically on first startup (EF Core reads the model and builds the schema; no manual SQL needed).

### 2 — Backend API

```bash
cd backend

# Set secrets via dotnet user-secrets (never commit these)
dotnet user-secrets set "Security:EncryptionKey" "YourExact32CharKeyHereABCDEFGHIJ" --project PasswordVault.API
dotnet user-secrets set "Security:JwtSecret"     "YourVeryLongJwtSecretKeyThatIsAtLeast64CharactersLong!!" --project PasswordVault.API

# Run
dotnet run --project PasswordVault.API
# API:     http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### 3 — Frontend

```bash
cd frontend/password-vault-ui
npm install
ng serve
# App at: http://localhost:4200
```

---

## Environment Variables Reference

| Variable | Required | Description |
|----------|----------|-------------|
| `SA_PASSWORD` | ✅ | SQL Server SA password |
| `ENCRYPTION_KEY` | ✅ | **Exactly 32 chars** — AES-256 server-side key |
| `JWT_SECRET` | ✅ | **At least 64 chars** — HMAC-SHA512 JWT signing key |
| `ConnectionStrings__DefaultConnection` | ✅ | Full SQL Server connection string |
| `Security__JwtIssuer` | ➖ | JWT issuer (default: `PasswordVaultAPI`) |
| `Security__JwtAudience` | ➖ | JWT audience (default: `PasswordVaultClient`) |
| `Security__JwtExpiryMinutes` | ➖ | Token lifetime in minutes (default: `60`) |
| `AllowedOrigins__0` | ➖ | Angular app origin (default: `http://localhost:4200`) |

> For production use **Azure Key Vault**, **AWS Secrets Manager**, or equivalent — never commit secrets to source control.

---

## API Reference

### Auth Endpoints

| Method | Endpoint | Auth Required |
|--------|----------|--------------|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/refresh-token` | Public |
| POST | `/api/auth/logout` | Bearer |

### Account Endpoints

All account endpoints require `Authorization: Bearer <token>`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts` | Paginated list (search, sort, filter) |
| GET | `/api/accounts/{id}` | Get by ID (no password in response) |
| POST | `/api/accounts` | Create (client-encrypted payload) |
| PUT | `/api/accounts/{id}` | Update |
| DELETE | `/api/accounts/{id}` | Delete |
| POST | `/api/accounts/{id}/decrypt-password` | Strip server layer → client decrypts |

---

## Running Tests

```bash
# .NET unit tests
cd backend
dotnet test --logger "console;verbosity=normal"

# Angular unit tests
cd frontend/password-vault-ui
ng test --watch=false --browsers=ChromeHeadless
```

---

## Docker Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `pv-sqlserver` stays `health: starting` | SQL Server takes 30–60s to initialise | Wait and re-run `docker compose ps` |
| `pv-api` keeps restarting | SQL Server not ready yet | Restart automatically — wait 1–2 min |
| Port already in use | Another process owns 1433/5000/4200 | `netstat -ano \| findstr :<port>` then kill the PID |
| `ERR_CONNECTION_RESET` in browser | `.env` values not loaded / container not recreated | Run `docker compose up --force-recreate api -d` |
| Icons broken in Angular | Google Fonts blocked by network | Check browser console for CSP errors |
| New `.env` values not taking effect | `restart` reuses old config | Use `--force-recreate` not `restart` |

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
# Copy dist/password-vault-ui/browser/ to IIS wwwroot or any CDN
```

---

## Folder Structure Detail

```
PasswordVault.Domain/
  Entities/
    User.cs           – User entity
    Account.cs        – Account credential entity (stores AES-encrypted password)
  Interfaces/
    IRepositories.cs  – IUserRepository, IAccountRepository, IUnitOfWork

PasswordVault.Application/
  DTOs/Dtos.cs        – All request/response records
  Interfaces/         – IAuthService, IAccountService, IEncryptionService…
  Services/
    AuthService.cs    – Register, login, refresh token, revoke
    AccountService.cs – CRUD, pagination, filtering

PasswordVault.Infrastructure/
  Data/
    AppDbContext.cs           – EF Core DbContext with Fluent API config
    Repositories/             – UserRepository, AccountRepository, UnitOfWork
    Migrations/               – EF Core migration files
  Security/
    CryptoServices.cs         – AES-256-CBC encryption + PBKDF2 password hasher
    JwtTokenService.cs        – JWT generation & validation (HMAC-SHA512)
  Logging/
    AuditService.cs           – Password reveal audit logging

PasswordVault.API/
  Controllers/
    AuthController.cs         – Auth endpoints (rate-limited)
    AccountsController.cs     – Account CRUD + decrypt endpoint
  Middleware/
    GlobalExceptionMiddleware – Structured JSON error responses
  Program.cs                  – DI wiring, JWT, CORS, rate limiting, Swagger,
                                auto schema creation on startup (EnsureCreatedAsync
                                + MigrateAsync fallback)

frontend/src/app/
  core/
    services/
      auth.service.ts         – Login, register, JWT storage
      account.service.ts      – CRUD + client-side encrypt/decrypt
      encryption.service.ts   – AES-256-CBC via Web Crypto API
      toast.service.ts        – Snackbar notifications
      theme.service.ts        – Dark/light theme toggle
    interceptors/
      jwt.interceptor.ts      – Attach Bearer token + auto-refresh on 401
    guards/
      auth.guard.ts           – Route protection
  features/
    auth/login/               – Login form component
    auth/register/            – Register form + password strength meter
    accounts/account-list/    – Data table + reveal/copy/delete
    accounts/account-form/    – Add/edit form + password generator
  shared/
    components/shell/         – Sidebar layout + theme toggle
    components/confirm-dialog/ – Delete confirmation dialog
```

---

## License

MIT
