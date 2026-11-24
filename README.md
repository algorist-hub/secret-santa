# Amigo Presente

Gift list and secret santa (Amigo Secreto) app for groups of people and events.
Current phase: ASP.NET Core API + Blazor WebAssembly webapp, served together by one host.
The UI lives in a shared Razor Class Library so a future .NET MAUI mobileapp can reuse it.

## Solution layout

`AlgoristSecretSanta.slnx`

| Project | Purpose |
|---|---|
| `src/Algorist.Api` | ASP.NET Core host: API under `/api` and the webapp for every other path |
| `src/Algorist.Api.Application` | Application layer: use cases (sign-in, Google sign-in, sign-up, session renewal), ports, options; server-side localized texts (e.g. emails) |
| `src/Algorist.Api.Domain` | Domain model (`User`, `UserAvatar`; no dependencies) |
| `src/Algorist.Api.Infra` | Infrastructure: EF Core `MainDbContext` (SQL Server) and migrations, password hashing, JWT tokens, Google ID token validation |
| `src/Algorist.Contracts` | Types shared by API and client: error codes, configuration keys, API DTOs and routes, validation rules (password, fields, minimum age, avatar) |
| `src/Algorist.App.UI` | Razor Class Library: pages, layout, view models, client services, platform interfaces, `.resx` texts, shared CSS/images |
| `src/Algorist.App.Web` | Blazor WebAssembly webapp: startup, `index.html`, `wwwroot/lib` (Bootstrap 5.3.9, Bootstrap Icons 1.13.2), web implementations of platform interfaces |
| `tests/Algorist.Api.Tests` | API integration tests (xUnit + WebApplicationFactory) |
| `tests/Algorist.App.UI.Tests` | UI unit and component tests (xUnit + bUnit) |

## Prerequisites

- .NET 10 SDK (see `global.json`)
- SQL Server LocalDB (local development) with the database `AlgoristSecretSanta`

## Run locally

The server needs these secrets. Locally they come from .NET user secrets (never commit them):

```bash
dotnet user-secrets set MainDatabaseConnectionString "Server=(localdb)\MSSQLLocalDB;Database=AlgoristSecretSanta;Trusted_Connection=True;TrustServerCertificate=True;" --project src/Algorist.Api
dotnet user-secrets set JwtSigningKey "<at least 32 random characters>" --project src/Algorist.Api
dotnet user-secrets set GoogleClientId "<id>.apps.googleusercontent.com" --project src/Algorist.Api
dotnet run --project src/Algorist.Api --launch-profile https
```

A signing key can be generated in PowerShell with
`[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))`.
Use a different key in each environment; changing it signs everybody out.

Then open https://localhost:7180 (or http://localhost:5180).

## Configuration

| Key | Required | Default | Meaning |
|---|---|---|---|
| `MainDatabaseConnectionString` | yes (secret) | – | Main SQL Server database. Startup fails naming the key when missing. |
| `JwtSigningKey` | yes (secret) | – | Signs authentication tokens and Google registration tickets; at least 32 characters. Startup fails naming the key when missing or short. |
| `GoogleClientId` | no | – | Google OAuth web client ID. When absent the Sign In screen hides the Google button. |
| `Registration:MinimumAgeYears` | no | `18` | Minimum age, in whole years, to register (must be 1 or more). |
| `Auth:TokenLifetimeDays` | no | `60` | Lifetime of an authentication token. The app renews it on every start, so active users stay signed in. |
| `Auth:AttemptsPerMinute` | no | `10` | Sign-in/sign-up attempts per client IP per minute before `TOO_MANY_ATTEMPTS`. |

Non-secret defaults are in `src/Algorist.Api/appsettings.json`. In App Service, nested keys use a double underscore (e.g. `Registration__MinimumAgeYears`).

## Authentication

- Email/password and Google sign-in. Passwords are stored as salted PBKDF2 hashes (ASP.NET Core Identity's hasher).
- The API issues a JWT (`Authorization: Bearer`). The client stores it through `ISecureTokenStorage` (web: `localStorage` key `app.auth-token`) and calls `POST /api/auth/renew` at every start.
- Google sign-in uses Google Identity Services in the webapp (`accounts.google.com/gsi/client`, loaded only on the Sign In screen). The API validates the Google ID token; a person without an account receives a 30-minute signed registration ticket and finishes on the Sign Up screen.

### Google OAuth client

1. In Google Cloud Console, open **Google Auth Platform** (APIs & Services → OAuth consent screen): set the app name and contact emails, choose **External**, and publish the app when real users should sign in (scopes used: `openid`, `email`, `profile`).
2. Create an **OAuth client ID** of type **Web application**. Authorized JavaScript origins: `https://localhost:7180`, `http://localhost:5180`, `http://localhost` and the App Service default domain. No redirect URIs are needed (popup flow).
3. Set the client ID as `GoogleClientId` (user secrets locally, App Service setting in Azure). The client secret is not used.

## Database migrations

EF Core migrations live in `src/Algorist.Api.Infra/Persistence/Migrations`. The server applies pending migrations at startup (except in the `Testing` environment used by the tests), so deployments need no manual database step.

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Algorist.Api.Infra --startup-project src/Algorist.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Algorist.Api.Infra --startup-project src/Algorist.Api
```

The design-time factory reads `MainDatabaseConnectionString` from the user secrets of `Algorist.Api` or an environment variable. Review every generated migration before committing it.

## Test

```bash
dotnet test --solution AlgoristSecretSanta.slnx
```

The repo opts into Microsoft.Testing.Platform in `global.json`, so the solution is passed with `--solution`.
API tests use an in-memory SQLite database and a fake Google token validator; they never touch SQL Server or Google.

## Localization

- Languages: `pt-BR` (default, formatting culture `pt-BR`) and `en` (formatting culture `en-GB`).
- UI texts: `src/Algorist.App.UI/Resources/AppStrings.resx` (Portuguese) and `AppStrings.en.resx`. Every key must exist in both files (a test enforces this). Do not hardcode user-visible text.
- The current language is stored only on the device (`localStorage` key `app.language`). On first run the browser language is detected (Portuguese → `pt-BR`, other known languages → `en`, otherwise the Select Language screen at `/select-language`).
- The API returns error codes (`Algorist.Contracts.Errors.ErrorCodes`) in problem details (`code` member); the client translates them via the `Error_<CODE>` resource keys.

## Styling

All colors, radii and shadows are CSS variables in `src/Algorist.App.UI/wwwroot/css/app.css` (`:root`). Component styles (`*.razor.css`) use `var(...)` only.
Bootstrap and Bootstrap Icons are committed under `src/Algorist.App.Web/wwwroot/lib` (versions pinned in `libman.json`); no CDN is used.

## Deployment (GitHub Actions → Azure App Service)

Workflow: `.github/workflows/main_algorist-secret-santa.yml` (created by Azure, adapted manually to this solution). It restores, builds and tests `AlgoristSecretSanta.slnx` on every push to `main`; on pushes to `main` (or a manual run on `main`) it publishes only `src/Algorist.Api` (which includes the webapp) and deploys it to the App Service `algorist-secret-santa`.

Required configuration:

| Where | Name | Value |
|---|---|---|
| GitHub secret | `AZUREAPPSERVICE_PUBLISHPROFILE_DDF578C816E3410489E6E536ADE99EB5` | Publish profile of the App Service (created by Azure) |
| GitHub environment | `Production` | Used by the deploy job (add protection rules if desired) |
| App Service setting | `MainDatabaseConnectionString` | Connection string of the Azure SQL database |
| App Service setting | `JwtSigningKey` | Token signing key (at least 32 characters, different from local) |
| App Service setting | `GoogleClientId` | Google OAuth web client ID |
