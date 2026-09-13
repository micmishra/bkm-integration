# BKM.Utility — Project Memory

This file tracks every feature, design decision, and pattern established in BKM.Utility.
Update it whenever a new feature is added or a structural decision is made.
It is the single source of truth for onboarding and generating future documentation.

---

## Product Identity

| Item | Value |
|---|---|
| Product name | BKM.Utility |
| Solution file | `bkm-integration/BKM.Utility.sln` |
| Runtime | .NET 8 / ASP.NET Core 8 |
| Language | C# 12 |
| Architecture | Independent vertical-slice NuGet packages, each with internal Clean Architecture |
| Database | SQL Server (EF Core 8, Code First) |
| Cache | SQL Server Distributed Cache (`IDistributedCache`) — `dbo.BkmCache` |
| Base URL (dev) | http://localhost:5000 |
| Swagger UI (dev) | http://localhost:5000/swagger |
| DB name (default) | `BkmUtilityDb` |

---

## Architecture: Independent Vertical-Slice Packages

### What this means

Each NuGet package is **fully self-contained**. It owns its own:
- Domain entities and interfaces (`Domain/`)
- Application use-case services and DTOs (`Application/`)
- Infrastructure (EF Core DbContext, repositories, external libs) (`Infrastructure/`)
- EF Core migrations (`Infrastructure/Persistence/Migrations/`)
- DI entry point (`*Extensions.cs` with `AddBkm*()`)

No shared `Domain`, `Application`, or `Infrastructure` project exists.
The three-layer discipline is enforced *inside* each package as a folder structure,
not as separate projects.

### Internal layer discipline (per package)

```
Domain/         ← pure C# entities + interfaces; zero NuGet deps
    ↑ referenced by
Application/    ← use-case services + DTOs; references Domain only; no EF, no SQL
    ↑ referenced by
Infrastructure/ ← EF Core, SQL, SMTP, Serilog, AES etc.; references Domain + Application
```

### Solution layout

```
bkm-integration/
├── BKM.Utility.sln
└── src/
    ├── Directory.Build.props          ← single version source of truth
    ├── BKM.Utility.Abstractions/      ← shared cross-cutting types (no feature logic)
    ├── BKM.Utility.Encryption/        ← independent NuGet package
    ├── BKM.Utility.Cache/             ← independent NuGet package
    ├── BKM.Utility.Auth/              ← independent NuGet package
    ├── BKM.Utility.Email/             ← independent NuGet package
    ├── BKM.Utility.Feed/              ← independent NuGet package
    ├── BKM.Utility.UrlShortener/      ← independent NuGet package
    ├── BKM.Utility.FileIngestion/     ← independent NuGet package
    ├── BKM.Utility.AppLog/            ← independent NuGet package
    ├── BKM.Utility/                   ← meta-package (references all 9 above)
    └── BKM.Utility.Api/               ← ASP.NET Core host; NOT packaged
```

### Package dependency graph

```
BKM.Utility.Abstractions     ← zero package deps (only Microsoft.Extensions.DI.Abstractions)
BKM.Utility.Encryption       ← Abstractions
BKM.Utility.Cache            ← Abstractions
BKM.Utility.Auth             ← Abstractions
BKM.Utility.FileIngestion    ← Abstractions
BKM.Utility.Email            ← Abstractions + Encryption + Cache
BKM.Utility.Feed             ← Abstractions + Cache
BKM.Utility.UrlShortener     ← Abstractions + Cache
BKM.Utility.AppLog           ← Abstractions + Cache
BKM.Utility (meta)           ← all 9 above
```

### Why Email depends on Encryption
`EmailService` decrypts the stored SMTP password using `IEncryptionService` at send time.
This is a hard dependency — `AddBkmEmail()` always auto-registers Encryption + Cache.

### Why Feed depends on Cache
`PostService.CreateAsync()` triggers push fan-out via `IPushFeedService`, which uses
`ICacheService<T>` for the pre-computed feed. This is a hard dependency.

---

## BKM.Utility.Abstractions

Shared cross-cutting types consumed by every feature package.
**Zero feature logic lives here.** Only plumbing interfaces and response models.

### Files

| File | Purpose |
|---|---|
| `Shared/Interfaces/ICacheService.cs` | Generic cache abstraction (`Get`, `Set`, `Remove`) |
| `Features/ApiResponse/Models/ApiResponse.cs` | `ApiResponse<T>` envelope returned by all endpoints |
| `Features/ApiResponse/Models/ErrorCodes.cs` | Enum of all error codes across all features |
| `Features/ApiResponse/Builders/ApiResponseBuilder.cs` | Fluent builder for `ApiResponse<T>` |

### Consumer usage

```csharp
// Success
return Ok(ApiResponseBuilder.Ok(data));

// Error
return NotFound(ApiResponseBuilder.Error(ErrorCodes.NotFound, "Not found."));
```

---

## Feature: Encryption

**Purpose:** AES-256-GCM stateless encrypt/decrypt. Zero database dependency.

| Item | Detail |
|---|---|
| Package | `BKM.Utility.Encryption` |
| DI method | `AddBkmEncryption(config)` |
| Config key | `"Encryption:Key"` — 32-byte Base64 string |
| DB tables | None |

### Key files

```
BKM.Utility.Encryption/
├── Domain/Interfaces/IEncryptionService.cs
├── Application/Interfaces/IEncryptionAppService.cs
├── Application/Services/EncryptionAppService.cs
├── Application/DTOs/EncryptRequest.cs, EncryptResponse.cs, DecryptRequest.cs, DecryptResponse.cs
├── Infrastructure/Crypto/AesGcmEncryptionService.cs
└── EncryptionExtensions.cs
```

### Endpoints

| Method | Path | Auth |
|---|---|---|
| `POST` | `/api/encryption/encrypt` | Bearer |
| `POST` | `/api/encryption/decrypt` | Bearer |

---

## Feature: Cache

**Purpose:** SQL Server distributed cache backing `ICacheService<T>`. Shared by Email, Feed, UrlShortener, AppLog.

| Item | Detail |
|---|---|
| Package | `BKM.Utility.Cache` |
| DI method | `AddBkmCache(config)` |
| Connection string key | `"Cache"` (falls back to `"DefaultConnection"`) |
| DB table | `dbo.BkmCache` |

### Key files

```
BKM.Utility.Cache/
├── Domain/Entities/UrlCacheEntry.cs
├── Infrastructure/Caching/DistributedCacheService.cs
├── Infrastructure/Persistence/CacheDbContext.cs
├── Infrastructure/Persistence/Migrations/
└── CacheExtensions.cs
```

---

## Feature: Auth

**Purpose:** JWT Bearer authentication, refresh tokens, SSO (Google + Microsoft), ASP.NET Identity, RBAC, ABAC.

| Item | Detail |
|---|---|
| Package | `BKM.Utility.Auth` |
| DI method | `AddBkmAuth(config)` |
| Connection string key | `"Auth"` (falls back to `"DefaultConnection"`) |
| DB tables | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `RefreshTokens`, `Permissions`, `RolePermissions` |

### Key files

```
BKM.Utility.Auth/
├── Domain/Entities/AppUser.cs, AppRole.cs, Permission.cs, RefreshToken.cs, RolePermission.cs
├── Domain/Interfaces/ITokenService.cs, IRefreshTokenRepository.cs, IPermissionRepository.cs
├── Application/Services/AuthService.cs, UserManagementService.cs, RoleManagementService.cs, PermissionService.cs
├── Infrastructure/Identity/TokenService.cs
├── Infrastructure/Persistence/AuthDbContext.cs
├── Infrastructure/Persistence/PermissionRepository.cs, RefreshTokenRepository.cs
├── Infrastructure/Persistence/Migrations/
└── AuthExtensions.cs
```

### JWT config

```json
"Jwt": {
  "SecretKey": "<min 32 chars>",
  "Issuer": "BKM.Utility",
  "Audience": "BKM.Utility.Clients",
  "AccessTokenMinutes": 60,
  "RefreshTokenDays": 30
}
```

### SSO config

```json
"Sso": {
  "Google":    { "ClientId": "", "ClientSecret": "" },
  "Microsoft": { "TenantId": "common", "ClientId": "", "ClientSecret": "" }
}
```

---

## Feature: Email

**Purpose:** SMTP email sender with DB-stored configs and templates, send audit log, AES-encrypted SMTP password.

| Item | Detail |
|---|---|
| Package | `BKM.Utility.Email` |
| DI method | `AddBkmEmail(config)` — auto-includes Encryption + Cache |
| Connection string key | `"Email"` (falls back to `"DefaultConnection"`) |
| DB tables | `EmailConfigs`, `EmailTemplates`, `EmailAuditLogs` |
| Hard dependency | `IEncryptionService` — SMTP password is stored encrypted |

### Key files

```
BKM.Utility.Email/
├── Domain/Entities/EmailConfig.cs, EmailTemplate.cs, EmailAuditLog.cs, SmtpTlsMode.cs
├── Application/Services/EmailService.cs
├── Infrastructure/Smtp/SmtpSender.cs
├── Infrastructure/Persistence/EmailDbContext.cs, Repos, Migrations
└── EmailExtensions.cs
```

---

## Feature: Feed

**Purpose:** Social feed with Push (pre-computed fan-out) and Pull (on-demand fan-in) strategies, Follow graph.

| Item | Detail |
|---|---|
| Package | `BKM.Utility.Feed` |
| DI method | `AddBkmFeed(config)` — auto-includes Cache |
| Connection string key | `"Feed"` (falls back to `"DefaultConnection"`) |
| DB tables | `Posts`, `Follows`, `UserFeedEntries` |
| Push feed cache TTL | 60 seconds |
| Pull feed cache TTL | 30 seconds |

### Key files

```
BKM.Utility.Feed/
├── Domain/Entities/Post.cs, Follow.cs, UserFeedEntry.cs
├── Application/Services/PostService.cs, PushFeedService.cs, PullFeedService.cs, FollowService.cs
├── Infrastructure/Persistence/FeedDbContext.cs, PostRepository.cs, FollowRepository.cs, UserFeedRepository.cs
├── Infrastructure/Persistence/Migrations/
└── FeedExtensions.cs
```

---

## Feature: UrlShortener

**Purpose:** Shorten URLs to 8-char Base62 codes. Redirect via short code. Cache-first resolution.

| Item | Detail |
|---|---|
| Package | `BKM.Utility.UrlShortener` |
| DI method | `AddBkmUrlShortener(config)` — auto-includes Cache |
| Connection string key | `"UrlShortener"` (falls back to `"DefaultConnection"`) |
| DB tables | `ShortenedUrls` |
| Cache TTL | 24 hours |
| Code generation | Base62 encoding of SQL IDENTITY Id — zero collisions |

### Key files

```
BKM.Utility.UrlShortener/
├── Domain/Entities/ShortenedUrl.cs, UrlCacheEntry.cs
├── Application/Services/UrlShortenerService.cs, Base62Encoder.cs
├── Infrastructure/Persistence/UrlShortenerDbContext.cs, UrlRepository.cs
├── Infrastructure/Persistence/Migrations/
└── UrlShortenerExtensions.cs
```

---

## Feature: FileIngestion

**Purpose:** Ingest CSV, TSV, JSON, JSON Lines, XML, and Excel files. SHA-256 deduplication per file.

| Item | Detail |
|---|---|
| Package | `BKM.Utility.FileIngestion` |
| DI method | `AddBkmFileIngestion(config)` |
| Connection string key | `"FileIngestion"` (falls back to `"DefaultConnection"`) |
| DB tables | `IngestionBatches`, `IngestedRecords` |
| Supported formats | CSV, TSV, JSON, JSON Lines, XML, `.xls`, `.xlsx` |
| Deduplication | SHA-256 hash of file content; duplicate files rejected |

### Key files

```
BKM.Utility.FileIngestion/
├── Domain/Entities/IngestionBatch.cs, IngestedRecord.cs, ParseOptions.cs
├── Application/Services/FileIngestionService.cs
├── Application/Interfaces/IFileParser.cs, IFileParserFactory.cs
├── Infrastructure/Parsing/DelimitedFileParser.cs, JsonFileParser.cs, XmlFileParser.cs, ExcelFileParser.cs, FileParserFactory.cs
├── Infrastructure/Persistence/FileIngestionDbContext.cs, IngestionRepository.cs
├── Infrastructure/Persistence/Migrations/
└── FileIngestionExtensions.cs
```

---

## Feature: AppLog

**Purpose:** Structured application logging via Serilog. DB + rolling-file sinks. Queryable log API. Per-feature retention policies with scheduled purge.

| Item | Detail |
|---|---|
| Package | `BKM.Utility.AppLog` |
| DI method | `AddBkmAppLog(config)` — auto-includes Cache |
| Connection string key | `"AppLog"` (falls back to `"DefaultConnection"`) |
| DB tables | `AppLogs`, `LogRetentionPolicies` |
| Serilog sinks | Database (`DatabaseLogSink`) + rolling file |
| Purge | `LogPurgeBackgroundService` — runs on a configurable interval |

### Logging config

```json
"Logging": {
  "MinimumLevel": "Information",
  "Sinks": { "Database": true, "File": true },
  "File": {
    "Path": "logs/bkm-.txt",
    "FileSizeLimitBytes": 10485760,
    "RetainedFileCountLimit": 30
  },
  "Purge": {
    "InitialDelayMinutes": 5,
    "RunIntervalHours": 24
  }
}
```

### Key files

```
BKM.Utility.AppLog/
├── Domain/Entities/AppLogEntry.cs, LogRetentionPolicy.cs
├── Application/Services/AppLogService.cs, RetentionPolicyService.cs
├── Infrastructure/Logging/AppLogRepository.cs, DatabaseLogSink.cs, SerilogConfigurator.cs
├── Infrastructure/Purging/LogPurgeBackgroundService.cs, RetentionPolicyRepository.cs
├── Infrastructure/Persistence/AppLogDbContext.cs
├── Infrastructure/Persistence/Migrations/
└── AppLogExtensions.cs
```

---

## DbContext Map

Each feature has its **own** `DbContext`, its **own** migration history, and its **own** connection string key.
They all fall back to `"DefaultConnection"` if the named key is absent.

| DbContext | Package | Connection string key | Tables |
|---|---|---|---|
| `CacheDbContext` | BKM.Utility.Cache | `"Cache"` | `BkmCache` |
| `AuthDbContext` | BKM.Utility.Auth | `"Auth"` | `AspNetUsers`, `AspNetRoles`, `RefreshTokens`, `Permissions`, `RolePermissions` |
| `EmailDbContext` | BKM.Utility.Email | `"Email"` | `EmailConfigs`, `EmailTemplates`, `EmailAuditLogs` |
| `FeedDbContext` | BKM.Utility.Feed | `"Feed"` | `Posts`, `Follows`, `UserFeedEntries` |
| `UrlShortenerDbContext` | BKM.Utility.UrlShortener | `"UrlShortener"` | `ShortenedUrls` |
| `FileIngestionDbContext` | BKM.Utility.FileIngestion | `"FileIngestion"` | `IngestionBatches`, `IngestedRecords` |
| `AppLogDbContext` | BKM.Utility.AppLog | `"AppLog"` | `AppLogs`, `LogRetentionPolicies` |

---

## BKM.Utility Meta-Package

`BKM.Utility.csproj` references all 9 feature packages and Abstractions.
`BkmUtilityExtensions.cs` exposes:
- `AddBkmUtility(config)` — registers all features at once
- Individual `AddBkm*(config)` methods — register one feature at a time (delegates to each feature's own extensions)

---

## Rules for Adding a New Feature

1. Create a new project `src/BKM.Utility.<FeatureName>/` — `<IsPackable>true</IsPackable>`
2. Add internal folder structure: `Domain/Entities/`, `Domain/Interfaces/`, `Application/DTOs/`, `Application/Interfaces/`, `Application/Services/`, `Infrastructure/Persistence/`
3. **Domain** — entities + interfaces only; zero NuGet deps, zero EF
4. **Application** — DTOs + interface + service; references Domain only; no EF, no SQL
5. **Infrastructure** — DbContext, repositories, external libs; references Domain + Application
6. Create `<Feature>Extensions.cs` with a single `AddBkm<Feature>(IServiceCollection, IConfiguration)` that inlines all DI registrations
7. Add a controller to `BKM.Utility.Api/Features/<Feature>/`; zero `try/catch`; use `ApiResponseBuilder`
8. Add error codes to `BKM.Utility.Abstractions/.../ErrorCodes.cs`
9. Add `ProjectReference` to both `BKM.Utility.Api.csproj` and `BKM.Utility.csproj`
10. Add a delegate call to `BkmUtilityExtensions.cs` in `AddBkmUtility()`
11. Add a project entry to `BKM.Utility.sln`
12. Run `dotnet build BKM.Utility.sln` — must be **0 warnings, 0 errors**

---

## EF Core Migration Commands

Each feature's migrations live inside that feature's own project.

```bash
cd bkm-integration

# Auth
dotnet ef migrations add <Name> \
  --project src/BKM.Utility.Auth/BKM.Utility.Auth.csproj \
  --startup-project src/BKM.Utility.Api/BKM.Utility.Api.csproj \
  --context AuthDbContext \
  --output-dir Infrastructure/Persistence/Migrations

# Cache
dotnet ef migrations add <Name> \
  --project src/BKM.Utility.Cache/BKM.Utility.Cache.csproj \
  --startup-project src/BKM.Utility.Api/BKM.Utility.Api.csproj \
  --context CacheDbContext \
  --output-dir Infrastructure/Persistence/Migrations

# Email
dotnet ef migrations add <Name> \
  --project src/BKM.Utility.Email/BKM.Utility.Email.csproj \
  --startup-project src/BKM.Utility.Api/BKM.Utility.Api.csproj \
  --context EmailDbContext \
  --output-dir Infrastructure/Persistence/Migrations

# Feed
dotnet ef migrations add <Name> \
  --project src/BKM.Utility.Feed/BKM.Utility.Feed.csproj \
  --startup-project src/BKM.Utility.Api/BKM.Utility.Api.csproj \
  --context FeedDbContext \
  --output-dir Infrastructure/Persistence/Migrations

# AppLog
dotnet ef migrations add <Name> \
  --project src/BKM.Utility.AppLog/BKM.Utility.AppLog.csproj \
  --startup-project src/BKM.Utility.Api/BKM.Utility.Api.csproj \
  --context AppLogDbContext \
  --output-dir Infrastructure/Persistence/Migrations

# UrlShortener
dotnet ef migrations add <Name> \
  --project src/BKM.Utility.UrlShortener/BKM.Utility.UrlShortener.csproj \
  --startup-project src/BKM.Utility.Api/BKM.Utility.Api.csproj \
  --context UrlShortenerDbContext \
  --output-dir Infrastructure/Persistence/Migrations

# FileIngestion
dotnet ef migrations add <Name> \
  --project src/BKM.Utility.FileIngestion/BKM.Utility.FileIngestion.csproj \
  --startup-project src/BKM.Utility.Api/BKM.Utility.Api.csproj \
  --context FileIngestionDbContext \
  --output-dir Infrastructure/Persistence/Migrations
```

Migrations are applied automatically on startup in `Program.cs` via `dbContext.Database.Migrate()`.

---

## NuGet Packages

### Packable projects

| Package ID | Project | Description |
|---|---|---|
| `BKM.Utility.Abstractions` | `BKM.Utility.Abstractions` | Shared interfaces + response models |
| `BKM.Utility.Encryption` | `BKM.Utility.Encryption` | AES-256-GCM, zero DB deps |
| `BKM.Utility.Cache` | `BKM.Utility.Cache` | SQL distributed cache |
| `BKM.Utility.Auth` | `BKM.Utility.Auth` | JWT, Identity, SSO, RBAC, ABAC |
| `BKM.Utility.Email` | `BKM.Utility.Email` | SMTP + templates + audit |
| `BKM.Utility.Feed` | `BKM.Utility.Feed` | Social feed push + pull |
| `BKM.Utility.UrlShortener` | `BKM.Utility.UrlShortener` | Base62 URL shortener |
| `BKM.Utility.FileIngestion` | `BKM.Utility.FileIngestion` | CSV/JSON/XML/Excel ingestion |
| `BKM.Utility.AppLog` | `BKM.Utility.AppLog` | Serilog + retention + purge |
| `BKM.Utility` | `BKM.Utility` | Meta-package (all features) |

### Building packages manually

```bash
cd bkm-integration
dotnet build BKM.Utility.sln
dotnet pack src/BKM.Utility/BKM.Utility.csproj --no-build -o nupkg
# or pack all:
dotnet pack BKM.Utility.sln --no-build -o nupkg
```

---

## Versioning

### Single source of truth

`src/Directory.Build.props`:

```xml
<VersionPrefix>1.0.0</VersionPrefix>
<VersionSuffix></VersionSuffix>   <!-- empty = stable; "rc.1" = pre-release -->
```

Never set `<Version>` inside individual `.csproj` files.

### CI pipeline — `.github/workflows/ci.yml`

Triggers on every push and PR. Builds + packs as `1.0.0-ci.<run_number>+<sha>`.

### Release pipeline — `.github/workflows/release.yml`

Manual `workflow_dispatch` with inputs:
- `bump_type`: `patch` / `minor` / `major`
- `prerelease`: empty (stable) or `rc.1`, `beta.2` etc.
- `dry_run`: `true` to preview without pushing

Steps: bump `VersionPrefix` in `Directory.Build.props` → commit → tag `v<version>` → build → pack → push to NuGet.

### Required GitHub secrets

| Secret | Purpose |
|---|---|
| `NUGET_API_KEY` | nuget.org or private feed API key |
| `NUGET_FEED_URL` | *(optional)* private feed URL |

---

## Session History

| Session | What was done |
|---|---|
| 1–10 | Initial build: Clean Architecture monolith with 3 shared layers (Domain/Application/Infrastructure) + 9 hollow NuGet wrapper projects |
| 11–15 | Auth (JWT + SSO + RBAC/ABAC), Email, Feed (push+pull), FileIngestion, AppLog features added; per-feature DbContexts introduced |
| 16 | `AppDbContext` replaced by 7 independent DbContexts with separate migration histories |
| 17 | Project renamed to BKM.Utility — all files, folders, and namespaces updated to BKM.Utility |
| 18 | `BkmUtilityExtensions.cs` created; `AddBkmUtility()` entry point established; DB name set to `BkmUtilityDb`; all packages produced as `BKM.Utility.*.nupkg` |
| 19 | **Full restructure to independent vertical-slice packages.** Deleted `BKM.Utility.Domain`, `BKM.Utility.Application`, `BKM.Utility.Infrastructure` projects. Each feature package now carries its own Domain/Application/Infrastructure folders internally. Added `BKM.Utility.Abstractions` for shared cross-cutting types. Build: **0 warnings, 0 errors**. |
