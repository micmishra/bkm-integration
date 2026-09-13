 f# BKM.Integration — Project Memory

This file tracks every feature, design decision, and pattern established in BKM.Integration.
Update it whenever a new feature is added. It is the single source of truth for onboarding
and for generating any future documentation artefact.

---

## Product Identity

| Item | Value |
|---|---|
| Product name | BKM.Integration |
| Solution file | `bkm-integration/BKM.Integration.sln` |
| Runtime | .NET 8 / ASP.NET Core 8 |
| Language | C# 12 |
| Architecture | Clean Architecture (Onion) + Feature Slices |
| Database | SQL Server (EF Core 8, Code First) |
| Cache | SQL Server Distributed Cache (`IDistributedCache`) |
| Base URL (dev) | http://localhost:5000 |
| Swagger UI (dev) | http://localhost:5000/swagger |

---

## Project Structure

```
bkm-integration/
├── BKM.Integration.sln
└── src/
    ├── BKM.Integration.Domain/          layer: entities + interfaces, zero deps
    ├── BKM.Integration.Application/     layer: use cases, DTOs, services
    ├── BKM.Integration.Infrastructure/  layer: EF Core, SQL, cache, crypto
    └── BKM.Integration.Api/             layer: controllers, middleware, filters
```

### Dependency direction
```
Api → Application → Domain ← Infrastructure
```
Domain has zero NuGet dependencies. Application references Domain only.
Infrastructure references Domain + Application. Api references Application + Infrastructure.

### Feature slice pattern (inside each layer)
```
<Layer>/
└── Features/
    └── <FeatureName>/       ← one folder per feature at the same depth
        ├── Entities/        (Domain only)
        ├── Interfaces/
        ├── DTOs/            (Application only)
        ├── Services/        (Application only)
        ├── Persistence/     (Infrastructure only)
        ├── Caching/         (Infrastructure only)
        └── Crypto/          (Infrastructure only)
```

### 3 shared files touched when adding any new feature
1. `Infrastructure/Persistence/AppDbContext.cs` — add DbSet + entity config region
2. `Application/DependencyInjection.cs` — add one scoped registration line
3. `Infrastructure/DependencyInjection.cs` — add one registration block

---

## Features

### 1. UrlShortener
**Purpose:** Shorten any URL to an 8-char Base62 code. Redirect via short code.

| Item | Detail |
|---|---|
| Endpoints | `POST /shorten`, `GET /{code}` |
| Code generation | Base62 encoding of SQL Server IDENTITY Id — zero collisions |
| Deduplication | Same URL always returns the same code (DB index on OriginalUrl) |
| Cache | SQL Server `IDistributedCache` — TTL 24h, cache-first on resolve |
| DB tables | `dbo.ShortenedUrls`, `dbo.UrlCache` |

**Key files:**
- Domain entity: `Domain/Features/UrlShortener/Entities/ShortenedUrl.cs`
- Domain entity: `Domain/Features/UrlShortener/Entities/UrlCacheEntry.cs`
- Interfaces: `Domain/Features/UrlShortener/Interfaces/IUrlRepository.cs`, `IUrlCacheService.cs`
- Service: `Application/Features/UrlShortener/Services/UrlShortenerService.cs`
- Encoder: `Application/Features/UrlShortener/Services/Base62Encoder.cs`
- Repository: `Infrastructure/Features/UrlShortener/Persistence/UrlRepository.cs`
- Cache impl: `Infrastructure/Features/UrlShortener/Caching/SqlServerCacheService.cs`
- Controller: `Api/Features/UrlShortener/UrlShortenerController.cs`

**Sample request/response:**
```json
POST /shorten  →  { "url": "https://example.com/long" }
200 OK         →  { "success": true, "data": { "shortUrl": "http://localhost:5000/1Z3bKx", "code": "1Z3bKx" }, ... }
```

---

### 2. Encryption
**Purpose:** Encrypt / decrypt any UTF-8 text (strings, JSON, Unicode, emojis) using AES-256-GCM.

| Item | Detail |
|---|---|
| Endpoints | `POST /api/encryption/encrypt`, `POST /api/encryption/decrypt` |
| Algorithm | AES-256-GCM (authenticated encryption — confidentiality + integrity + tamper detection) |
| Key | 32-byte key, Base64-encoded, stored in `appsettings.json → Encryption:Key` |
| Nonce | 96-bit cryptographic random per call — safe until ~4 billion encryptions per key |
| Output | Base64-encoded package: `[12-byte nonce] + [cipher text] + [16-byte GCM tag]` |
| No DB | Pure stateless service — no EF/DB changes needed |
| DI lifetime | Singleton (stateless after construction) |
| Tamper behaviour | Any byte flip → `CryptographicException` → middleware → 400 Bad Request |
| Key config (dev) | `appsettings.json` Encryption:Key |
| Key config (prod) | Environment variable or Azure Key Vault |

**Key files:**
- Domain interface: `Domain/Features/Encryption/Interfaces/IEncryptionService.cs`
- App interface: `Application/Features/Encryption/Interfaces/IEncryptionAppService.cs`
- App service: `Application/Features/Encryption/Services/EncryptionAppService.cs`
- DTOs: `Application/Features/Encryption/DTOs/` (Encrypt/DecryptRequest, Encrypt/DecryptResponse)
- Implementation: `Infrastructure/Features/Encryption/Crypto/AesGcmEncryptionService.cs`
- Controller: `Api/Features/Encryption/EncryptionController.cs`

**Sample request/response:**
```json
POST /api/encryption/encrypt  →  { "plainText": "Hello 你好 🔐" }
200 OK  →  { "success": true, "data": { "cipherPackage": "base64...", "algorithm": "AES-256-GCM" }, ... }

POST /api/encryption/decrypt  →  { "cipherPackage": "base64..." }
200 OK  →  { "success": true, "data": { "plainText": "Hello 你好 🔐" }, ... }
```

---

### 3. ApiResponse (Cross-cutting)
**Purpose:** Universal response envelope + global exception handling for all endpoints.

| Item | Detail |
|---|---|
| Envelope fields | `success`, `message`, `data`, `meta`, `warnings`, `errors`, `debug` |
| Global exception | `GlobalExceptionHandlerMiddleware` — first in pipeline, catches everything |
| Validation | `ValidationFilter` — global MVC filter, auto-rejects invalid ModelState |
| Debug block | Populated only outside Production (traceId, exceptionType, stackTrace, timestamp) |
| Try/catch in controllers | **Zero** — never needed, middleware handles all exceptions |
| Error codes | Centralised in `Application/Features/ApiResponse/Models/ErrorCodes.cs` |

**Exception → HTTP status mapping:**
| Exception type | HTTP status | Error code |
|---|---|---|
| ArgumentException / ArgumentNullException | 400 | BAD_REQUEST |
| CryptographicException | 400 | DECRYPTION_FAILED |
| UnauthorizedAccessException | 401 | UNAUTHORIZED |
| KeyNotFoundException / FileNotFoundException | 404 | NOT_FOUND |
| InvalidOperationException | 409 | CONFLICT |
| NotImplementedException | 501 | INTERNAL_ERROR |
| OperationCanceledException | 499 | TIMEOUT |
| TimeoutException | 504 | TIMEOUT |
| Everything else | 500 | INTERNAL_ERROR |

**Key files:**
- Model: `Application/Features/ApiResponse/Models/ApiResponse.cs`
- Error codes: `Application/Features/ApiResponse/Models/ErrorCodes.cs`
- Builder: `Application/Features/ApiResponse/Builders/ApiResponseBuilder.cs`
- Middleware: `Api/Features/ApiResponse/Middleware/GlobalExceptionHandlerMiddleware.cs`
- Filter: `Api/Features/ApiResponse/Filters/ValidationFilter.cs`

**Builder usage pattern:**
```csharp
// Success with data
return Ok(ApiResponseBuilder.Ok(data, "Created successfully."));

// Success with meta
return Ok(ApiResponseBuilder<T>.Success(data)
    .WithMeta("totalCount", 1000).WithMeta("page", 1).Build());

// Failure
return NotFound(ApiResponseBuilder.Error(ErrorCodes.NotFound, "Item not found."));

// Warning
return Ok(ApiResponseBuilder<T>.Success(data)
    .WithWarning("Field 'legacyId' is deprecated.").Build());
```

---

## Configuration Reference (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=BkmIntegrationDb;..."
  },
  "BaseUrl": "http://localhost:5000",
  "Encryption": {
    "Key": "<32-byte Base64 key — use env var or Key Vault in production>"
  },
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
}
```

**Generate a new key:**
```powershell
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$b = New-Object byte[] 32; $rng.GetBytes($b); [Convert]::ToBase64String($b)
```

---

## Rules for Adding a New Feature

1. Create `Features/<NewFeature>/` folder inside each of the 4 projects
2. Domain: entities + interfaces only — no NuGet, no EF, no HTTP
3. Application: DTOs + interface + service — no EF, no SQL
4. Infrastructure: concrete implementations (Persistence/, Caching/, Crypto/ as needed)
5. Api: one controller, zero try/catch, use `ApiResponseBuilder`
6. Add error codes to `ErrorCodes.cs`
7. Register in `Application/DependencyInjection.cs` and `Infrastructure/DependencyInjection.cs`
8. If DB table needed: add DbSet + entity config region to `AppDbContext.cs`, then `dotnet-ef migrations add <FeatureName>`
9. Run `dotnet build BKM.Integration.sln` — must be 0 warnings, 0 errors

---

## EF Core Migration Commands

```bash
cd bkm-integration

# Add migration after schema change
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
dotnet-ef migrations add <MigrationName> \
  --project src/BKM.Integration.Infrastructure/BKM.Integration.Infrastructure.csproj \
  --startup-project src/BKM.Integration.Api/BKM.Integration.Api.csproj \
  --output-dir Persistence/Migrations

# Migrations apply automatically on startup via dbCtx.Database.Migrate()
```

---

## Session History Summary

| Session | What was built |
|---|---|
| 1 | Go-based in-memory URL shortener (urlshortener/) |
| 2 | .NET 8 + SQL Server + IDistributedCache URL shortener (urlshortener-dotnet/) |
| 3 | Refactored to Clean Architecture (4 projects) |
| 4 | Feature-slice folders inside each layer |
| 5 | Renamed to BKM.Integration (bkm-integration/) |
| 6 | Added Encryption feature (AES-256-GCM) |
| 7 | Added ApiResponse feature (universal envelope + global exception middleware) |
| 8 | Added AppLog feature (Serilog dual-sink: DB + rolling text file, configurable, Code First) |
| 9 | Added Log Purging — per-feature retention policies (`dbo.LogRetentionPolicies`), scheduled background purge of DB rows + log files |
| 10 | Promoted distributed cache to platform-wide generic `ICacheService<T>` — UrlShortener (24h TTL) + AppLog queries (30s TTL) |
| 11 | Added Email Notification — pure BCL SMTP sender (SslStream/TcpClient, Veracode CWE-297/319 clean), DB-stored config + templates + audit log, AES-256-GCM password encryption |
| 12 | Added PushFeed + PullFeed — two independent social feed services sharing Posts/Follows social graph |
| 13 | Added Auth + SSO + RBAC + ABAC — JWT Bearer, OpenIdConnect (Google/Microsoft), ASP.NET Identity, role/permission DB-backed, Swagger JWT UI |
| 14 | Added FileIngestion — generic streaming ingest (CSV/TSV/pipe/fixed-width/JSON/JSON Lines/XML/Excel), one master table, SHA-256 dedup, JSON payload column with indexes |

---

## Features: Feed System (PushFeed + PullFeed)

### Shared domain (`Domain/Features/Feed/`)

| Entity | Table | Purpose |
|---|---|---|
| `Post` | `dbo.Posts` | Content unit — UserId, Body, MediaUrl, CreatedAt |
| `Follow` | `dbo.Follows` | Social graph — FollowerId → FolloweeId, unique index |
| `UserFeedEntry` | `dbo.UserFeeds` | Pre-computed push-feed rows per follower |

### Feature: PushFeed — Pre-computed (fan-out on write)

**How it works:**
1. User posts → `POST /api/posts?authorId=X`
2. `PostService` saves the post → calls `PushFeedService.FanOutAsync(post)`
3. `FanOutAsync` loads all followers, writes one `UserFeedEntry` per follower into `dbo.UserFeeds`
4. Trims each follower's feed to 500 entries (oldest removed)
5. Evicts cache pages 1–5 for all affected followers

**Read:** `GET /api/feed/push?userId=X` reads directly from `dbo.UserFeeds` — **O(1)**, no aggregation
**Cache:** `push:feed:{userId}:p{page}` — TTL 60 s

**Tradeoff:** Write amplification proportional to follower count. Best for accounts with < 10k followers.

### Feature: PullFeed — On-demand (fan-in on read)

**How it works:**
1. User requests feed → `GET /api/feed/pull?userId=X`
2. `PullFeedService` loads followee IDs → queries `dbo.Posts` for each followee (up to 1000 per followee)
3. Merges, sorts by `PostedAt DESC`, paginates in memory
4. Caches result 30 s

**Read:** Computed fresh per request (or from 30 s cache). No `dbo.UserFeeds` involved.
**Cache:** `pull:feed:{userId}:p{page}` — TTL 30 s

**Tradeoff:** Read latency grows with followee count. Best for high-follower accounts or lazy-read clients.

### API Endpoints

| Method | Path | Service |
|---|---|---|
| `POST` | `/api/posts?authorId=X` | Creates post + triggers push fan-out |
| `GET` | `/api/feed/push?userId=X&page=1&pageSize=20` | PushFeed read (pre-computed) |
| `GET` | `/api/feed/pull?userId=X&page=1&pageSize=20` | PullFeed read (on-demand) |
| `POST` | `/api/follow?followerId=X` + body `{ "followeeId": "Y" }` | Follow a user |
| `DELETE` | `/api/follow/{followeeId}?followerId=X` | Unfollow |
| `GET` | `/api/follow/followees?userId=X` | List followees |

### Key files

| File | Purpose |
|---|---|
| `Application/Features/Feed/Services/PushFeedService.cs` | Fan-out + cached read |
| `Application/Features/Feed/Services/PullFeedService.cs` | Fan-in on demand + 30 s cache |
| `Application/Features/Feed/Services/PostService.cs` | Create post + trigger fan-out |
| `Infrastructure/Features/Feed/Persistence/UserFeedRepository.cs` | `dbo.UserFeeds` CRUD + purge |
| `Api/Features/Feed/FeedController.cs` | `/api/feed/push` + `/api/feed/pull` |

---

## Cross-cutting: Distributed Cache (`ICacheService<T>`)

**Location:** `Domain/Shared/Interfaces/ICacheService.cs` (contract) + `Infrastructure/Shared/Caching/DistributedCacheService.cs` (impl)

**Store:** SQL Server `dbo.UrlCache` via `Microsoft.Extensions.Caching.SqlServer` (`IDistributedCache`)

**DI registration:** Open-generic — one line covers every `T`:
```csharp
services.AddTransient(typeof(ICacheService<>), typeof(DistributedCacheService<>));
```

**Usage per feature:**

| Feature | Inject | Key pattern | TTL |
|---|---|---|---|
| UrlShortener | `ICacheService<string>` | `url:{code}` | 24 hours |
| AppLog queries | `ICacheService<AppLogPageResult>` | `log:query:{hash16}` | 30 seconds |

**Adding cache to a new feature:**
1. Inject `ICacheService<YourDto>` in the Application service constructor
2. Call `await cache.GetAsync(key, ct)` → if not null, return early
3. Call `await cache.SetAsync(key, value, ttl, ct)` after loading from DB
4. No DI change needed — open-generic covers it automatically

**Fault tolerance:**
- `GetAsync` returns `null` on any cache error — callers always fall through to DB
- `SetAsync` / `RemoveAsync` swallow exceptions — cache faults never break the request path
- Logged at `Warning` level via `ILogger<DistributedCacheService<T>>`

---

### 4. AppLog
**Purpose:** Structured application logging to DB (`dbo.AppLogs`) and rolling text files via Serilog. Queryable via REST API with pagination and filters.

| Item | Detail |
|---|---|
| Endpoints | `GET /api/logs` — paginated query with filters |
| Sinks | SQL Server (`dbo.AppLogs`) + rolling file (`logs/bkm-*.txt`) |
| Buffer | `Channel<LogEvent>` — 100k capacity, batch 100 — never blocks request thread |
| DB table | `dbo.AppLogs` (Id, Timestamp, Level, Message, Feature, Exception, Properties) |
| Log levels | Verbose, Debug, Information, Warning, Error, Fatal |

**Key files:**
- Entity: `Domain/Features/AppLog/Entities/AppLogEntry.cs`
- Interface: `Domain/Features/AppLog/Interfaces/IAppLogRepository.cs`
- Service: `Application/Features/AppLog/Services/AppLogService.cs`
- Sink: `Infrastructure/Features/AppLog/Logging/DatabaseLogSink.cs`
- Repository: `Infrastructure/Features/AppLog/Logging/AppLogRepository.cs`
- Controller: `Api/Features/AppLog/AppLogController.cs`

---

### 5. Log Retention & Purging
**Purpose:** Configure per-feature DB and file log retention periods. A background service purges expired rows and old log files on a configurable schedule.

| Item | Detail |
|---|---|
| Endpoints | `GET /api/logs/retention`, `PUT /api/logs/retention` |
| DB table | `dbo.LogRetentionPolicies` (Id, Feature, DbRetentionDays, FileRetentionDays, Description, LastUpdatedAt, LastPurgedAt, LastPurgeDeletedCount) |
| Default policy | Row where `Feature IS NULL` — applies to any feature with no specific policy |
| Feature policy | Row with `Feature = 'FeatureName'` — overrides default |
| Skip forever | Set `DbRetentionDays = 0` or `FileRetentionDays = 0` — that sink is never purged |
| Schedule | `Logging:Purge:InitialDelayMinutes` (default 5), `Logging:Purge:RunIntervalHours` (default 24) |
| Background service | `LogPurgeBackgroundService` — `IHostedService`, uses `IServiceScopeFactory` for scoped deps |
| File purge | Deletes `*.txt` in the log directory older than default policy `FileRetentionDays` |

**Key files:**
- Entity: `Domain/Features/AppLog/Entities/LogRetentionPolicy.cs`
- Interface: `Domain/Features/AppLog/Interfaces/IRetentionPolicyRepository.cs`
- App interface: `Application/Features/AppLog/Interfaces/IRetentionPolicyService.cs`
- App service: `Application/Features/AppLog/Services/RetentionPolicyService.cs`
- DTOs: `Application/Features/AppLog/DTOs/RetentionPolicyDto.cs`, `UpsertRetentionPolicyRequest.cs`
- Repository: `Infrastructure/Features/AppLog/Purging/RetentionPolicyRepository.cs`
- Background service: `Infrastructure/Features/AppLog/Purging/LogPurgeBackgroundService.cs`
- Controller: `Api/Features/AppLog/LogRetentionController.cs`

**Sample request/response:**
```json
GET /api/logs/retention
→ { "success": true, "data": [ { "feature": null, "dbRetentionDays": 90, "fileRetentionDays": 30, ... } ] }

PUT /api/logs/retention
body: { "feature": "UrlShortener", "dbRetentionDays": 30, "fileRetentionDays": 14, "description": "Short-lived URL logs" }
→ { "success": true, "data": { "id": 2, "feature": "UrlShortener", "dbRetentionDays": 30, ... } }
```
