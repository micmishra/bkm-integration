# BKM.Utility

A collection of **independently installable .NET 8 NuGet packages** — pick only the features
you need and add them to any ASP.NET Core 8 application with a single `AddBkm*()` call.

---

## Table of Contents

1. [Packages](#packages)
2. [Quick Start — Install Everything](#quick-start--install-everything)
3. [Quick Start — Install One Feature](#quick-start--install-one-feature)
4. [Project Structure](#project-structure)
5. [Configuration Reference](#configuration-reference)
6. [API Reference](#api-reference)
7. [Adding a New Feature](#adding-a-new-feature)
8. [EF Core Migrations](#ef-core-migrations)
9. [Versioning](#versioning)
10. [License](#license)

---

## Packages

| NuGet Package | What you get | Auto-includes |
|---|---|---|
| `BKM.Utility.Abstractions` | `ICacheService<T>`, `ApiResponse<T>`, `ApiResponseBuilder`, `ErrorCodes` | — |
| `BKM.Utility.Encryption` | AES-256-GCM encrypt/decrypt, zero DB deps | Abstractions |
| `BKM.Utility.Cache` | SQL Server distributed cache (`dbo.BkmCache`), `ICacheService<T>` | Abstractions |
| `BKM.Utility.Auth` | JWT, refresh tokens, SSO (Google/Microsoft), Identity, RBAC, ABAC | Abstractions |
| `BKM.Utility.Email` | SMTP sender, DB-stored configs + templates, audit log | Abstractions + Encryption + Cache |
| `BKM.Utility.Feed` | Posts, Follow graph, Push feed (fan-out), Pull feed (fan-in) | Abstractions + Cache |
| `BKM.Utility.UrlShortener` | Base62 short codes, SQL cache, 24 h TTL | Abstractions + Cache |
| `BKM.Utility.FileIngestion` | CSV, TSV, JSON, JSON Lines, XML, Excel, SHA-256 dedup | Abstractions |
| `BKM.Utility.AppLog` | Serilog DB + file sinks, queryable log API, retention + purge | Abstractions + Cache |
| `BKM.Utility` | Meta-package — installs all of the above | Everything |

---

## Quick Start — Install Everything

```bash
dotnet add package BKM.Utility
```

```csharp
// Program.cs
builder.Services.AddBkmUtility(builder.Configuration);
```

That single call registers every feature's services and infrastructure.

### Minimum `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BkmUtilityDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "SecretKey": "<min 32-char random string>",
    "Issuer":    "BKM.Utility",
    "Audience":  "BKM.Utility.Clients"
  },
  "Encryption": {
    "Key": "<32-byte Base64 key>"
  }
}
```

Generate an encryption key:

```powershell
$b = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
[Convert]::ToBase64String($b)
```

Migrations apply automatically on startup — no manual `dotnet ef database update` needed.
Default seed: roles `Admin / Moderator / User` and admin `admin@bkm.local / Admin@12345`.

---

## Quick Start — Install One Feature

Each package is fully self-contained. Install only what you need:

```bash
# Encryption only — zero DB dependencies
dotnet add package BKM.Utility.Encryption
```

```csharp
builder.Services.AddBkmEncryption(builder.Configuration);
// appsettings.json: "Encryption": { "Key": "..." }
```

```bash
# Auth only
dotnet add package BKM.Utility.Auth
```

```csharp
builder.Services.AddBkmAuth(builder.Configuration);
```

```bash
# Email only (Encryption + Cache auto-included)
dotnet add package BKM.Utility.Email
```

```csharp
builder.Services.AddBkmEmail(builder.Configuration);
```

```bash
# Any combination
dotnet add package BKM.Utility.Auth
dotnet add package BKM.Utility.Feed
```

```csharp
builder.Services
    .AddBkmAuth(builder.Configuration)
    .AddBkmFeed(builder.Configuration);
```

### Per-feature connection strings (optional)

By default every feature falls back to `"DefaultConnection"`. To point features at separate databases:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=BkmUtilityDb;...",
    "Auth":              "Server=...;Database=BkmAuthDb;...",
    "Email":             "Server=...;Database=BkmEmailDb;..."
  }
}
```

| Feature | Connection string key |
|---|---|
| Auth | `"Auth"` |
| Email | `"Email"` |
| Feed | `"Feed"` |
| AppLog | `"AppLog"` |
| UrlShortener | `"UrlShortener"` |
| FileIngestion | `"FileIngestion"` |
| Cache | `"Cache"` |

---

## Project Structure

```
bkm-integration/
├── BKM.Utility.sln
├── README.md
├── MEMORY.md                          ← design decisions & session history
├── nupkg/                             ← built .nupkg output
├── .github/
│   └── workflows/
│       ├── ci.yml                     ← build + pack on every push/PR
│       └── release.yml                ← manual version bump, tag, NuGet publish
└── src/
    ├── Directory.Build.props          ← single version source of truth (VersionPrefix)
    │
    ├── BKM.Utility.Abstractions/      ← shared: ICacheService<T>, ApiResponse, ErrorCodes
    │
    ├── BKM.Utility.Encryption/        ← self-contained NuGet package
    │   ├── Domain/Interfaces/         ← IEncryptionService
    │   ├── Application/               ← IEncryptionAppService, DTOs, EncryptionAppService
    │   ├── Infrastructure/Crypto/     ← AesGcmEncryptionService
    │   └── EncryptionExtensions.cs    ← AddBkmEncryption()
    │
    ├── BKM.Utility.Cache/             ← self-contained NuGet package
    │   ├── Domain/                    ← UrlCacheEntry
    │   ├── Infrastructure/Caching/    ← DistributedCacheService<T>
    │   ├── Infrastructure/Persistence/← CacheDbContext + Migrations
    │   └── CacheExtensions.cs         ← AddBkmCache()
    │
    ├── BKM.Utility.Auth/              ← self-contained NuGet package
    │   ├── Domain/                    ← AppUser, AppRole, Permission, RefreshToken
    │   ├── Application/               ← DTOs, IAuthService, AuthService, etc.
    │   ├── Infrastructure/            ← AuthDbContext, TokenService, Repos, Migrations
    │   └── AuthExtensions.cs          ← AddBkmAuth()
    │
    ├── BKM.Utility.Email/             ← self-contained NuGet package
    │   ├── Domain/                    ← EmailConfig, EmailTemplate, EmailAuditLog
    │   ├── Application/               ← DTOs, IEmailService, EmailService
    │   ├── Infrastructure/            ← EmailDbContext, SmtpSender, Repos, Migrations
    │   └── EmailExtensions.cs         ← AddBkmEmail()
    │
    ├── BKM.Utility.Feed/              ← self-contained NuGet package
    │   ├── Domain/                    ← Post, Follow, UserFeedEntry
    │   ├── Application/               ← DTOs, IPostService/IPushFeedService/etc.
    │   ├── Infrastructure/            ← FeedDbContext, Repos, Migrations
    │   └── FeedExtensions.cs          ← AddBkmFeed()
    │
    ├── BKM.Utility.UrlShortener/      ← self-contained NuGet package
    │   ├── Domain/                    ← ShortenedUrl, UrlCacheEntry
    │   ├── Application/               ← DTOs, IUrlShortenerService, Base62Encoder
    │   ├── Infrastructure/            ← UrlShortenerDbContext, UrlRepository, Migrations
    │   └── UrlShortenerExtensions.cs  ← AddBkmUrlShortener()
    │
    ├── BKM.Utility.FileIngestion/     ← self-contained NuGet package
    │   ├── Domain/                    ← IngestionBatch, IngestedRecord, ParseOptions
    │   ├── Application/               ← DTOs, IFileIngestionService, IFileParser
    │   ├── Infrastructure/            ← FileIngestionDbContext, Parsers, Migrations
    │   └── FileIngestionExtensions.cs ← AddBkmFileIngestion()
    │
    ├── BKM.Utility.AppLog/            ← self-contained NuGet package
    │   ├── Domain/                    ← AppLogEntry, LogRetentionPolicy
    │   ├── Application/               ← DTOs, IAppLogService, AppLogService
    │   ├── Infrastructure/            ← AppLogDbContext, Serilog sinks, Purge, Migrations
    │   └── AppLogExtensions.cs        ← AddBkmAppLog()
    │
    ├── BKM.Utility/                   ← meta-package (references all 9 above)
    │   └── BkmUtilityExtensions.cs    ← AddBkmUtility() — wires everything
    │
    └── BKM.Utility.Api/               ← ASP.NET Core host (not packaged)
        ├── Features/                  ← one controller per feature
        └── Program.cs
```

### Clean Architecture within each package

Every feature package enforces the same layer discipline internally:

```
Domain      → zero NuGet deps, pure C# entities + interfaces
    ↑
Application → references Domain only; use-case services + DTOs
    ↑
Infrastructure → references Domain + Application; EF Core, SQL, SMTP, crypto
```

### Package dependency graph

```
BKM.Utility.Abstractions     (zero package deps)
BKM.Utility.Encryption       → Abstractions
BKM.Utility.Cache            → Abstractions
BKM.Utility.Auth             → Abstractions
BKM.Utility.FileIngestion    → Abstractions
BKM.Utility.Email            → Abstractions + Encryption + Cache
BKM.Utility.Feed             → Abstractions + Cache
BKM.Utility.UrlShortener     → Abstractions + Cache
BKM.Utility.AppLog           → Abstractions + Cache
BKM.Utility                  → all 9 above
```

---

## Configuration Reference

### Full `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BkmUtilityDb;Trusted_Connection=True;TrustServerCertificate=True;",
    "Auth":              "...",
    "Email":             "...",
    "Feed":              "...",
    "AppLog":            "...",
    "UrlShortener":      "...",
    "FileIngestion":     "...",
    "Cache":             "..."
  },
  "BaseUrl": "http://localhost:5000",
  "Jwt": {
    "SecretKey":           "<min 32 chars>",
    "Issuer":              "BKM.Utility",
    "Audience":            "BKM.Utility.Clients",
    "AccessTokenMinutes":  60,
    "RefreshTokenDays":    30
  },
  "Sso": {
    "Google":    { "ClientId": "", "ClientSecret": "" },
    "Microsoft": { "TenantId": "common", "ClientId": "", "ClientSecret": "" }
  },
  "Encryption": {
    "Key": "<32-byte Base64 key>"
  },
  "Logging": {
    "MinimumLevel": "Information",
    "Sinks":        { "Database": true, "File": true },
    "File": {
      "Path":                   "logs/bkm-.txt",
      "FileSizeLimitBytes":     10485760,
      "RetainedFileCountLimit": 30
    },
    "Purge": {
      "InitialDelayMinutes": 5,
      "RunIntervalHours":    24
    }
  }
}
```

### Authorization policies (built-in)

| Policy | Requirement |
|---|---|
| `AdminOnly` | Role = `Admin` |
| `ModeratorPlus` | Role = `Admin` or `Moderator` |
| `CanCreatePost` | Claim `permission` = `posts:create` |
| `CanManageUsers` | Claim `permission` = `users:manage` |
| `CanSendEmail` | Claim `permission` = `email:send` |
| `CanViewLogs` | Claim `permission` = `logs:view` |

---

## API Reference

### Auth

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | — | Register a new user |
| `POST` | `/api/auth/login` | — | Login → JWT + refresh token |
| `POST` | `/api/auth/refresh` | — | Exchange refresh token |
| `POST` | `/api/auth/logout` | Bearer | Revoke refresh token |
| `POST` | `/api/auth/sso/google` | — | Exchange Google auth code |
| `POST` | `/api/auth/sso/microsoft` | — | Exchange Microsoft auth code |

### User Management

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/api/users` | AdminOnly | List all users |
| `GET` | `/api/users/{id}` | Bearer | Get user by ID |
| `PUT` | `/api/users/{id}` | Bearer | Update profile |
| `DELETE` | `/api/users/{id}` | AdminOnly | Delete user |

### Role & Permission Management

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/api/roles` | AdminOnly | List roles |
| `POST` | `/api/roles` | AdminOnly | Create role |
| `POST` | `/api/roles/assign` | AdminOnly | Assign role to user |
| `DELETE` | `/api/roles/{roleId}/users/{userId}` | AdminOnly | Remove role from user |
| `GET` | `/api/permissions` | AdminOnly | List all permissions |
| `POST` | `/api/permissions` | AdminOnly | Create permission |
| `POST` | `/api/permissions/{id}/assign` | AdminOnly | Assign permission to role |
| `DELETE` | `/api/permissions/{id}/roles/{roleId}` | AdminOnly | Remove permission from role |

### Email

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/email/send` | CanSendEmail | Send plain or template email |
| `GET` | `/api/email/configs` | AdminOnly | List SMTP configs |
| `POST` | `/api/email/configs` | AdminOnly | Create SMTP config |
| `PUT` | `/api/email/configs/{id}` | AdminOnly | Update SMTP config |
| `PUT` | `/api/email/configs/{id}/activate` | AdminOnly | Set active SMTP config |
| `DELETE` | `/api/email/configs/{id}` | AdminOnly | Delete SMTP config |
| `GET` | `/api/email/templates` | Bearer | List email templates |
| `POST` | `/api/email/templates` | AdminOnly | Create template |
| `PUT` | `/api/email/templates/{id}` | AdminOnly | Update template |
| `DELETE` | `/api/email/templates/{id}` | AdminOnly | Delete template |
| `GET` | `/api/email/audit` | CanViewLogs | Query send audit log |

### Feed

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/posts?authorId=X` | CanCreatePost | Create post + trigger push fan-out |
| `GET` | `/api/posts?userId=X` | Bearer | List posts by user |
| `GET` | `/api/feed/push?userId=X` | Bearer | Read pre-computed push feed |
| `GET` | `/api/feed/pull?userId=X` | Bearer | Read on-demand pull feed |
| `POST` | `/api/follow?followerId=X` | Bearer | Follow a user |
| `DELETE` | `/api/follow/{followeeId}?followerId=X` | Bearer | Unfollow |
| `GET` | `/api/follow/followees?userId=X` | Bearer | List followees |

### File Ingestion

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/ingest` | Bearer | Upload file (CSV/TSV/JSON/XML/Excel) |
| `GET` | `/api/ingest/batches` | Bearer | List ingestion batches |
| `GET` | `/api/ingest/batches/{id}` | Bearer | Get batch by ID |
| `GET` | `/api/ingest/records` | Bearer | Query ingested records |

### URL Shortener

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/shorten` | Bearer | Shorten a URL |
| `GET` | `/{code}` | — | Redirect to original URL |

### Encryption

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/encryption/encrypt` | Bearer | AES-256-GCM encrypt |
| `POST` | `/api/encryption/decrypt` | Bearer | AES-256-GCM decrypt |

### App Logs & Retention

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/api/logs` | CanViewLogs | Query structured logs (paginated) |
| `GET` | `/api/logs/retention` | AdminOnly | List retention policies |
| `PUT` | `/api/logs/retention` | AdminOnly | Upsert retention policy |

---

## Adding a New Feature

1. Create a new project `src/BKM.Utility.<FeatureName>/` with `<IsPackable>true</IsPackable>`
2. Add internal folders: `Domain/Entities/`, `Domain/Interfaces/`, `Application/DTOs/`, `Application/Interfaces/`, `Application/Services/`, `Infrastructure/Persistence/`
3. **Domain** — entities + interfaces only; no EF, no NuGet beyond framework
4. **Application** — DTOs + interface + service; no EF, no SQL
5. **Infrastructure** — `DbContext`, repositories, any external libs (EF, SMTP, etc.)
6. Create `<Feature>Extensions.cs` with `AddBkm<Feature>(IServiceCollection, IConfiguration)` that wires up DI inline
7. Add controller to `BKM.Utility.Api/Features/<Feature>/`; zero `try/catch`; use `ApiResponseBuilder`
8. Add error codes to `BKM.Utility.Abstractions/Features/ApiResponse/Models/ErrorCodes.cs`
9. Add `ProjectReference` in `BKM.Utility.Api.csproj` and `BKM.Utility.csproj`
10. Add project entry to `BKM.Utility.sln`
11. Run `dotnet build BKM.Utility.sln` — must be 0 warnings, 0 errors

### Controller pattern

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MyFeatureController(IMyFeatureService svc) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await svc.GetAllAsync(ct);
        return Ok(ApiResponseBuilder.Ok(result));
    }
}
```

### Universal response envelope

```csharp
// Success with data
Ok(ApiResponseBuilder.Ok(data))

// Not found
NotFound(ApiResponseBuilder.Error(ErrorCodes.NotFound, "Item not found."))
```

---

## EF Core Migrations

Each feature has its own `DbContext` and its own migration history under
`Infrastructure/Persistence/Migrations/` inside that feature's project.

```bash
cd bkm-integration

# Add a migration for a specific feature (e.g. Auth)
dotnet ef migrations add <MigrationName> \
  --project src/BKM.Utility.Auth/BKM.Utility.Auth.csproj \
  --startup-project src/BKM.Utility.Api/BKM.Utility.Api.csproj \
  --context AuthDbContext \
  --output-dir Infrastructure/Persistence/Migrations

# Other contexts — same pattern, change --project and --context:
#   CacheDbContext        → BKM.Utility.Cache
#   EmailDbContext        → BKM.Utility.Email
#   FeedDbContext         → BKM.Utility.Feed
#   AppLogDbContext       → BKM.Utility.AppLog
#   UrlShortenerDbContext → BKM.Utility.UrlShortener
#   FileIngestionDbContext→ BKM.Utility.FileIngestion
```

Migrations apply automatically on startup — `dbContext.Database.Migrate()` is called for all
7 DbContexts in `Program.cs`.

---

## Versioning

Version is managed via **`src/Directory.Build.props`** — one file, inherited by all projects.

```xml
<VersionPrefix>1.0.0</VersionPrefix>   <!-- MAJOR.MINOR.PATCH -->
<VersionSuffix></VersionSuffix>         <!-- empty = stable; "rc.1" / "beta.2" = pre-release -->
```

| Scenario | Resulting version |
|---|---|
| Stable release | `1.2.0` |
| Pre-release | `1.2.0-rc.1` |
| CI build | `1.2.0-ci.42+a3f8b2c` |

### Cutting a release

Go to **GitHub → Actions → Release → Run workflow** and fill in:

| Input | Values |
|---|---|
| `bump_type` | `patch` / `minor` / `major` |
| `prerelease` | empty (stable) or `rc.1`, `beta.2` etc. |
| `dry_run` | `true` to preview without pushing |

### Required secrets

| Secret | Value |
|---|---|
| `NUGET_API_KEY` | nuget.org or private feed API key |
| `NUGET_FEED_URL` | *(optional)* private feed URL |

---

## License

MIT — see `LICENSE` for details.
