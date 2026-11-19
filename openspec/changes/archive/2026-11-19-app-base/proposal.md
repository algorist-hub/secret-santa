# Proposal

## Why

Amigo Presente (a gift list app with secret santa for multiple groups of people and events) has no code yet. Before building features such as sign in, sign up or events, we need a working foundation: solution layout, hosting, localization, a responsive app shell and a deployment pipeline. Doing this now keeps every later change small and lets the deferred mobileapp reuse the UI.

## What Changes

- Create solution `AlgoristSecretSanta.slnx` with Clean Architecture projects named `Algorist.<Name>` (no "SecretSanta" in project names): API host, Application, Domain, Infra, shared Contracts, shared Razor Class Library (all UI) and a Blazor WebAssembly webapp, plus test projects.
- A single ASP.NET Core host (Azure App Service) serves both the API (`/api/...`) and the Blazor WebAssembly webapp.
- Main database wiring: EF Core + SQL Server with the connection string read from the `MainDatabaseConnectionString` environment variable. No entities or schema yet.
- Mobile-ready seams: platform-specific concerns (local storage, secure token storage, client language detection, ads, billing) sit behind interfaces in the RCL; only web implementations are created. No MAUI project.
- Localization: `.resx` + `IStringLocalizer<T>` in the RCL; supported languages `pt-BR` (default, Brazil flag + "Português do Brasil") and `en` (UK flag + "English", formatting culture `en-GB`). UI and formatting culture are applied together.
- Current language stored locally only (not in the main database). On first run the app detects the client language: Portuguese → `pt-BR`, any other known language → `en`, unknown/failure → Select Language screen.
- API errors are returned as stable error codes that the client translates; server-side localization infrastructure is prepared for localized emails.
- Responsive app shell using Bootstrap grid, with Bootstrap 5.3.9 and Bootstrap Icons 1.13.2 embedded under `wwwroot/lib` (no CDN) and styling via CSS variables in a root stylesheet.
- Main menu: lateral (left) menu on wide screens with logo + app title above items and Profile at the bottom; on narrow screens, logo + title at the top with menu buttons below. Items: Home, Events, Friends, Profile, each opening a blank page with its title.
- Blank "Sign In" screen as the initial screen.
- "Select Language" screen, reachable only by URL for now: radio list of available languages (current one preselected), Apply and Cancel buttons.
- GitHub Actions workflow to build, test and deploy to Azure App Service.

## Capabilities

### New Capabilities
- `hosting-deployment`: single-host serving of API and webapp, configuration of the main database connection, and CI/CD to Azure App Service.
- `localization`: supported languages/cultures, first-run language detection, local persistence of the current language, translated UI texts, API error codes and server-side localized content.
- `language-selection`: the Select Language screen and its apply/cancel behavior.
- `app-shell`: responsive layout, main menu and navigation, initial routing to Sign In, placeholder pages and the shared styling foundation.

### Modified Capabilities

(none)

## Impact

- New code: entire `src/` and `tests/` trees, `AlgoristSecretSanta.slnx`, `.github/workflows/`.
- New dependencies: .NET 10 SDK, ASP.NET Core, Blazor WebAssembly, EF Core SQL Server provider, `Microsoft.Extensions.Localization`, xUnit and bUnit for tests; Bootstrap 5.3.9 and Bootstrap Icons 1.13.2 as embedded static files.
- Infrastructure: one Azure App Service; GitHub repository secrets for deployment and an app setting for `MainDatabaseConnectionString`.
- No database schema yet, so no EF Core migration in this change.
