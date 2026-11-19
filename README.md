# Amigo Presente

Gift list and secret santa (Amigo Secreto) app for groups of people and events.
Current phase: ASP.NET Core API + Blazor WebAssembly webapp, served together by one host.
The UI lives in a shared Razor Class Library so a future .NET MAUI mobileapp can reuse it.

## Solution layout

`AlgoristSecretSanta.slnx`

| Project | Purpose |
|---|---|
| `src/Algorist.Api` | ASP.NET Core host: API under `/api` and the webapp for every other path |
| `src/Algorist.Api.Application` | Application layer; server-side localized texts (e.g. emails) |
| `src/Algorist.Api.Domain` | Domain model (no dependencies) |
| `src/Algorist.Api.Infra` | Infrastructure: EF Core `MainDbContext` (SQL Server) |
| `src/Algorist.Contracts` | Types shared by API and client (error codes, configuration keys) |
| `src/Algorist.App.UI` | Razor Class Library: pages, layout, view models, client services, platform interfaces, `.resx` texts, shared CSS/images |
| `src/Algorist.App.Web` | Blazor WebAssembly webapp: startup, `index.html`, `wwwroot/lib` (Bootstrap 5.3.9, Bootstrap Icons 1.13.2), web implementations of platform interfaces |
| `tests/Algorist.Api.Tests` | API integration tests (xUnit + WebApplicationFactory) |
| `tests/Algorist.App.UI.Tests` | UI unit and component tests (xUnit + bUnit) |

## Prerequisites

- .NET 10 SDK (see `global.json`)
- SQL Server LocalDB (local development) with the database `AlgoristSecretSanta`

## Run locally

The server reads the connection string from the configuration key `MainDatabaseConnectionString`.
Locally it comes from .NET user secrets (never commit it):

```bash
dotnet user-secrets set MainDatabaseConnectionString "Server=(localdb)\MSSQLLocalDB;Database=AlgoristSecretSanta;Trusted_Connection=True;TrustServerCertificate=True;" --project src/Algorist.Api
dotnet run --project src/Algorist.Api --launch-profile https
```

Then open https://localhost:7180 (or http://localhost:5180).

## Test

```bash
dotnet test --solution AlgoristSecretSanta.slnx
```

The repo opts into Microsoft.Testing.Platform in `global.json`, so the solution is passed with `--solution`.

## Localization

- Languages: `pt-BR` (default, formatting culture `pt-BR`) and `en` (formatting culture `en-GB`).
- UI texts: `src/Algorist.App.UI/Resources/AppStrings.resx` (Portuguese) and `AppStrings.en.resx`. Every key must exist in both files (a test enforces this). Do not hardcode user-visible text.
- The current language is stored only on the device (`localStorage` key `app.language`). On first run the browser language is detected (Portuguese → `pt-BR`, other known languages → `en`, otherwise the Select Language screen at `/select-language`).
- The API returns error codes (`Algorist.Contracts.Errors.ErrorCodes`) in problem details (`code` member); the client translates them via the `Error_<CODE>` resource keys.

## Styling

All colors, radii and shadows are CSS variables in `src/Algorist.App.UI/wwwroot/css/app.css` (`:root`). Component styles (`*.razor.css`) use `var(...)` only.
Bootstrap and Bootstrap Icons are committed under `src/Algorist.App.Web/wwwroot/lib` (versions pinned in `libman.json`); no CDN is used.

## Deployment (GitHub Actions → Azure App Service)

Workflow: `.github/workflows/ci-cd.yml`. It builds and tests on every push and pull request to `main`, and deploys `Algorist.Api` on pushes to `main`.

Required configuration:

| Where | Name | Value |
|---|---|---|
| GitHub secret | `AZURE_CLIENT_ID` | Client ID of the Entra ID app / managed identity with a federated credential for this repo |
| GitHub secret | `AZURE_TENANT_ID` | Entra ID tenant ID |
| GitHub secret | `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| GitHub variable | `AZURE_WEBAPP_NAME` | Name of the Azure App Service |
| GitHub environment | `production` | Used by the deploy job (add protection rules if desired) |
| App Service setting | `MainDatabaseConnectionString` | Connection string of the Azure SQL database |
