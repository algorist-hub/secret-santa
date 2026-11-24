# Design

## Context

The app-base change produced the solution skeleton (see `openspec/changes/archive/2026-11-19-app-base/design.md`): Blazor WebAssembly served by the API host, all UI in the RCL `Algorist.App.UI`, platform seams in `Algorist.App.UI/Platform` with web implementations in `Algorist.App.Web`. Relevant current state:

- `MainDbContext` exists with no entities and no migrations; `Program.cs` fails fast without `MainDatabaseConnectionString`.
- `ISecureTokenStorage` (web: `LocalStorageTokenStorage`, key `app.auth-token`) exists but is unused.
- Errors are `ProblemDetails` with a `code` extension (`ProblemResults`), translated on the client by `ErrorMessageTranslator` (`Error_<CODE>` resx keys).
- `AppRoutes.Initial` is the constant `/sign-in`, used by `Index.razor`, `SelectLanguageViewModel` and `NotFound`.
- All pages use `MainLayout`; `SignIn.razor` is a title-only placeholder.
- API tests use `WebApplicationFactory` with a dummy connection string and never open a database.

Requirements are in `specs/`; motivation in proposal.md.

## Goals / Non-Goals

**Goals:**
- Stateless token authentication that works identically for the webapp and a future MAUI mobileapp (no cookies, no server redirects).
- Business rules (age, password, field rules) in one place, shared by API and client.
- Google sign-in behind a platform interface so MAUI can use a native Google SDK later.

**Non-Goals:**
- ASP.NET Core Identity (UI, roles, lockout tables, email confirmation).
- Refresh-token rotation, token revocation lists, sign-out from all devices.
- Serving stored avatars (the Profile change will add a read endpoint).
- Content Security Policy (tracked as a risk below).

## Decisions

### D1. Data model and migration

New entities in `Algorist.Api.Domain/Users`, mapped in `Algorist.Api.Infra/Persistence/Configurations`:

**`Users`**
| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` PK | generated in code (`Guid.CreateVersion7()`) |
| `Email` | `nvarchar(254)` not null | lowercase + trimmed; **unique index** |
| `Name` | `nvarchar(100)` not null | |
| `DateOfBirth` | `date` not null | `DateOnly` |
| `PhoneNumber` | `nvarchar(20)` null | |
| `Address` | `nvarchar(300)` null | |
| `PasswordHash` | `nvarchar(512)` null | null for Google-only accounts |
| `GoogleSubject` | `nvarchar(255)` null | Google `sub`; **unique filtered index** `WHERE [GoogleSubject] IS NOT NULL` |
| `GoogleAvatarUrl` | `nvarchar(2048)` null | Google picture when no upload replaced it |
| `CreatedAtUtc` | `datetime2` not null | |

**`UserAvatars`** (1:0..1, kept separate so user queries never load image bytes)
| Column | Type | Notes |
|---|---|---|
| `UserId` | `uniqueidentifier` PK, FK → `Users.Id` cascade delete | |
| `ContentType` | `varchar(20)` not null | `image/jpeg`, `image/png`, `image/webp` |
| `Content` | `varbinary(max)` not null | ≤ 1 MB, enforced in Application |
| `UpdatedAtUtc` | `datetime2` not null | |

EF Core migration `AddUsers` in `Algorist.Api.Infra/Persistence/Migrations` (first migration). Requires `Microsoft.EntityFrameworkCore.Design` in `Algorist.Api` (startup project) and an `IDesignTimeDbContextFactory<MainDbContext>` in Infra reading `MainDatabaseConnectionString` from environment/user secrets so `dotnet ef` works locally. Avatar access goes through `IAvatarStore` (Application interface, EF implementation in Infra) so a later move to Azure Blob Storage only replaces the implementation.

### D2. Applying migrations

`Program.cs` calls `Database.MigrateAsync()` before `app.Run()` unless the environment is `Testing`. App Service runs a single instance today, so concurrent migrators are not a concern.

*Alternative:* migration bundle executed from GitHub Actions — rejected for now: needs database credentials and network access from CI. Revisit when scaling out.

### D3. Layering of the server logic

- **Contracts** (`Algorist.Contracts/Auth`, `/Registration`): request/response DTOs, `PasswordPolicy`, `RegistrationRules` (email/name/phone/address limits, returning field error codes), new `ErrorCodes`: `INVALID_CREDENTIALS`, `TOO_MANY_ATTEMPTS`, `GOOGLE_SIGN_IN_FAILED`, `UNDERAGE`, `EMAIL_ALREADY_REGISTERED`, `WEAK_PASSWORD`, `INVALID_AVATAR`, `UNAUTHORIZED`. Shared with the RCL so client and server validate identically.
- **Domain**: `User`, `UserAvatar`, `AgePolicy.IsOldEnough(DateOnly birth, DateOnly today, int minimumYears)` (birth ≤ `today.AddYears(-minimumYears)`; Feb-29 births handled by `AddYears`).
- **Application**: use cases `PasswordSignIn`, `GoogleSignIn`, `RegisterUser`, `RenewSession` returning a `Result<T>` with an error code; ports `IUserRepository`, `IAvatarStore`, `IPasswordHasher`, `ITokenIssuer`, `IGoogleIdTokenValidator`, `IGoogleRegistrationTicketService`; options `RegistrationOptions` and `AuthOptions`; `TimeProvider` injected for testable dates.
- **Infra**: EF repositories, `IdentityPasswordHasher` (wraps `PasswordHasher<User>` from `Microsoft.Extensions.Identity.Core`, PBKDF2), `JwtTokenIssuer`, `GoogleIdTokenValidator` (`Google.Apis.Auth` `GoogleJsonWebSignature.ValidateAsync` with audience = `GoogleClientId`, requires `email_verified`).
- **Api**: `AuthEndpoints` and `PublicSettingsEndpoints` modules (`IEndpointModule`), mapping `Result` errors to `ProblemResults`.

*Alternative:* ASP.NET Core Identity + `MapIdentityApi` — rejected: brings many tables and cookie/bearer conventions we would mostly override, and doesn't fit the Google ID-token flow or the custom fields cleanly.

### D4. Tokens

JWT (HMAC-SHA256) signed with `JwtSigningKey`; claims `sub` (user id), `email`, `name`; issuer/audience `amigo-presente`; lifetime `Auth:TokenLifetimeDays` (default 60). Validated by `Microsoft.AspNetCore.Authentication.JwtBearer`. `JwtBearerEvents.OnChallenge` writes the `UNAUTHORIZED` problem response so the error-code contract holds.

"Permanently signed in" = sliding renewal: on each app start the client calls `POST /api/auth/renew` and stores the new token.

*Alternative:* short access token + rotating refresh token — more secure against token theft, but adds a token table and rotation logic; deferred until sign-out/revocation is needed.

### D5. API surface

| Endpoint | Auth | Body → Response |
|---|---|---|
| `GET /api/public-settings` | none | → `{ minimumAgeYears, googleClientId? }` |
| `POST /api/auth/sign-in` | none, rate-limited | `{ email, password }` → `{ token, expiresAtUtc }` |
| `POST /api/auth/google` | none, rate-limited | `{ idToken }` → `{ status: "SignedIn", token, expiresAtUtc }` or `{ status: "RegistrationRequired", registrationTicket, email, name, pictureUrl }` |
| `POST /api/auth/sign-up` | none, rate-limited | `{ email, name, dateOfBirth, password?, phoneNumber?, address?, avatar? { contentType, base64 }, googleRegistrationTicket?, useGoogleAvatar }` → `{ token, expiresAtUtc }` |
| `POST /api/auth/renew` | bearer | → `{ token, expiresAtUtc }` (401 if the user no longer exists) |

- **Google registration ticket**: a JWT signed with the same key but audience `google-registration`, 30-minute lifetime, carrying Google `sub`, email, name, picture. With a ticket, sign-up takes email and Google identity from the ticket (body email ignored), requires no password, and rejects an invalid/expired ticket with `GOOGLE_SIGN_IN_FAILED`. This prevents a client from registering arbitrary emails as "Google" accounts.
- **Google account matching**: by `GoogleSubject` first; otherwise by verified email, linking `GoogleSubject` to the existing account.
- **Rate limiting**: `Microsoft.AspNetCore.RateLimiting` fixed-window policy `auth` (10 requests / minute / client IP) on the three anonymous auth POSTs; `OnRejected` writes 429 + `TOO_MANY_ATTEMPTS`. `UseForwardedHeaders` (X-Forwarded-For) so App Service's proxy doesn't make all clients share one IP.
- **Unique email race**: unique index violation (`DbUpdateException`) maps to `EMAIL_ALREADY_REGISTERED`.
- **Avatar**: JSON base64 (≤ 1 MB decoded, checked against content type and magic bytes) rather than multipart, keeping one request per sign-up.

### D6. Configuration

`ConfigurationKeys` gains `JwtSigningKey` and `GoogleClientId`. `AuthOptions` (`Auth:TokenLifetimeDays`) and `RegistrationOptions` (`Registration:MinimumAgeYears`) are bound with `[Range(1, …)]` + `ValidateDataAnnotations().ValidateOnStart()`; defaults live in the options classes and are also written to `appsettings.json` for visibility. The signing key check sits next to the existing connection-string check in `Program.cs`. Local values via `dotnet user-secrets set JwtSigningKey …` / `GoogleClientId …`.

### D7. Client session and routing

- `AuthSession` (RCL, singleton): current token + parsed claims (payload decoded without validation; the server is the authority), `SignInAsync(token)` (stores via `ISecureTokenStorage`), `SignOutAsync()`, `Changed` event.
- `AppAuthenticationStateProvider : AuthenticationStateProvider` built on `AuthSession` (`Microsoft.AspNetCore.Components.Authorization`). `AppRoot` wraps the router in `CascadingAuthenticationState` and uses `AuthorizeRouteView`; `NotAuthorized` renders `RedirectToSignIn`. Home/Events/Friends/Profile get `@attribute [Authorize]`.
- `SessionBootstrapper` (RCL), awaited in `Program.cs` after `LanguageBootstrapper`: no token → signed out; `POST /api/auth/renew` 200 → store new token; 401 → clear; network error or 5xx → keep token (spec: server unreachable).
- `AuthorizationHeaderHandler` (DelegatingHandler on the `Api` HttpClient): adds `Authorization: Bearer`; on 401 from a non-`/api/auth/` request calls `AuthSession.SignOutAsync()`, and `AppRoot` navigates to Sign In on the `Changed` event.
- `AppRoutes.Initial` constant becomes `AppRoutes.InitialFor(bool signedIn)` used by `Index`, `SelectLanguageViewModel` and `NotFound`.
- Sign In and Sign Up redirect to Home in `OnInitialized` when signed in.

### D8. Layouts

- New `AuthLayout` (RCL): brand (🎁 + title) centered above a single card column (`col-12 col-sm-10 col-md-7 col-lg-5 col-xl-4`), no menu. Sign In and Sign Up declare `@layout AuthLayout`.
- `MainLayout` renders the `AuthLayout` shell instead of the menu when the user is signed out, so Select Language and not-found automatically follow the spec without per-page logic.
- Visual style (card surface, rounded inputs and buttons, palette, Google button placement under a divider "or") follows `prompts/reference.html` via the existing CSS variables; no content or structure is copied from it.

### D9. Screens and view models

- `SignInViewModel`, `SignUpViewModel` (RCL `ViewModels/`), tested with bUnit; pages stay thin. `AuthApiClient` wraps the HTTP calls and turns problem responses into error codes translated by `ErrorMessageTranslator`.
- Forms use `EditForm` with a custom validator component that runs `RegistrationRules`/`PasswordPolicy` from Contracts and maps field error codes to `Validation_*` resx keys. Password rules render as a live checklist.
- Date of birth: `InputDate` with `max` = today and `min` = 1900-01-01; the minimum-age message takes the value from `GET /api/public-settings` (fetched once, cached in a `PublicSettings` service).
- Avatar: `InputFile accept="image/jpeg,image/png,image/webp"`; `IBrowserFile.RequestImageFileAsync(sameType, 512, 512)` resizes on the client; preview via data URL; Remove button. Works in MAUI Blazor Hybrid as well.
- Google pre-fill: `PendingGoogleRegistration` (RCL, in-memory singleton) holds the ticket/profile between Sign In and Sign Up; it is lost on reload, giving the plain email form (spec scenario).

### D10. Google sign-in on the client

`IGoogleSignInProvider` (RCL Platform): `IsAvailable`, `RenderButtonAsync(ElementReference container, string clientId, string languageCode, Func<string, Task> onIdToken)`. Web implementation `GoogleIdentityServicesProvider` in App.Web: `platform.js` loads `https://accounts.google.com/gsi/client` on demand, calls `google.accounts.id.initialize` + `renderButton` (button text in the current language, Google's required branding), and invokes .NET via `DotNetObjectReference` with the credential. Cancelling the popup produces no callback, which yields "no error" naturally. A MAUI implementation can render its own button in the container and use a native SDK, returning the same ID token.

*Alternative:* server-side OAuth redirect (`AddGoogle` cookie handler) — rejected: cookie/redirect based, not usable from a MAUI client, and conflicts with bearer tokens.

### D11. Tests

- **API**: `ApiFactory` sets `JwtSigningKey`, replaces `MainDbContext` options with SQLite in-memory (`Microsoft.EntityFrameworkCore.Sqlite`, open connection held by the factory, `EnsureCreated`), and replaces `IGoogleIdTokenValidator` with a fake. Covers every error code, renewal, rate limiting, Google link/registration and ticket tampering.
- **Domain/Contracts**: unit tests for `AgePolicy` (birthday today, Feb 29, configured 21), `PasswordPolicy`, `RegistrationRules`.
- **UI**: bUnit tests for Sign In/Sign Up view models and pages (fake `AuthApiClient`, `IGoogleSignInProvider`, `ISecureTokenStorage`), routing guards, layouts with/without menu.

## Risks / Trade-offs

- [Token in `localStorage` is readable by injected scripts] → Already accepted in app-base for web; add a Content Security Policy in a later change; MAUI will use SecureStorage via `ISecureTokenStorage`.
- [Long-lived tokens cannot be revoked] → 60-day configurable lifetime; renewal fails once the user is deleted; refresh-token rotation can be added later without spec changes to sign-in.
- [Auto-linking Google to an existing email account] → Only when Google reports `email_verified`; Google is the email's authority, so this matches common practice.
- [Migrations at startup] → Fine for one instance; a failed migration stops startup (visible in App Service logs) rather than serving with a wrong schema.
- [SQLite in tests differs from SQL Server] → Schema features used (unique/filtered indexes, varbinary) are supported; the migration itself is reviewed manually and exercised locally against LocalDB.
- [Avatars in SQL grow the database] → 1 MB cap and client-side resize; `IAvatarStore` seam allows moving to Blob Storage.
- [Google script is an external dependency] → Loaded only on Sign In and only when configured; email sign-in works if it fails to load.

## Migration Plan

1. ✅ (2026-11-24) OAuth 2.0 Web client created in Google Cloud Console; authorized JavaScript origins: local dev URLs from `launchSettings.json` and the App Service URL.
2. ✅ (2026-11-24) Locally: `JwtSigningKey` and `GoogleClientId` set with `dotnet user-secrets` on `Algorist.Api`.
3. ✅ (2026-11-24) Azure App Service: `JwtSigningKey` and `GoogleClientId` added as environment variables (app settings).
4. Deploy; the `AddUsers` migration runs at startup.
5. Rollback: redeploy the previous build. The new tables are unused by it and can stay; rotating `JwtSigningKey` invalidates all sessions if needed.
