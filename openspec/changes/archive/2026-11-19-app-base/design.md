# Design

## Context

The repository holds only OpenSpec scaffolding, prompts and a visual reference (`prompts/reference.html`); there is no code. The .NET 10 SDK is installed locally. See proposal.md for motivation and specs/ for required behavior. Constraints come from the project config: .NET 10, Clean Architecture, SQL Server via EF Core, Bootstrap 5.3.9 + Bootstrap Icons 1.13.2, `.resx` + `IStringLocalizer<T>` in the shared RCL, GitHub Actions → Azure App Service. The MAUI mobileapp is deferred, but the RCL must be usable by a future MAUI Blazor Hybrid host without changes.

## Goals / Non-Goals

**Goals:**
- A buildable, testable, deployable skeleton that later changes (sign in, sign up, events) extend without restructuring.
- All UI and client logic in the RCL; the webapp project only boots the app and supplies web implementations of platform interfaces.
- Client-side localization that works the same way in a browser and, later, in MAUI.

**Non-Goals:**
- Authentication, users, database entities or migrations.
- Real ads or billing integration (interfaces and no-op web implementations only).
- Sending emails (only the server-side localization plumbing that emails will use).
- Provisioning Azure resources (the App Service and SQL database are created manually; the workflow only deploys).

## Decisions

### D1. Solution and project layout

```
AlgoristSecretSanta.slnx
global.json                    (SDK 10.0.x, rollForward latestFeature)
Directory.Build.props          (net10.0, Nullable, ImplicitUsings, warnings as errors)
Directory.Packages.props       (central package versions)
src/
  Algorist.Api.Domain          class lib, no dependencies (empty for now)
  Algorist.Api.Application     class lib → Domain, Contracts; server resources (emails)
  Algorist.Api.Infra           class lib → Application; EF Core MainDbContext (SQL Server)
  Algorist.Api                 ASP.NET Core host → Application, Infra, Contracts, App.Web
  Algorist.Contracts           class lib shared by API and client: error codes, DTOs
  Algorist.App.UI              Razor Class Library: pages, layouts, components, view models,
                               client services, platform interfaces, .resx, shared css/images
  Algorist.App.Web             Blazor WebAssembly: Program.cs, index.html, wwwroot/lib,
                               web implementations of platform interfaces
tests/
  Algorist.Api.Tests           xUnit + WebApplicationFactory
  Algorist.App.UI.Tests        xUnit + bUnit
```

Names follow the user's `Algorist.Api.Infra` example (server layers prefixed `Algorist.Api.`, client projects `Algorist.App.`). `Algorist.Contracts` is separate so the RCL never references server layers. A future `Algorist.App.Maui` would reference `Algorist.App.UI` only.

*Alternative:* one `Algorist.Shared` project for contracts and UI — rejected because the API would then depend on Razor components.

### D2. Hosting: Blazor WebAssembly served by the API host

`Algorist.Api` references `Algorist.App.Web` and the `Microsoft.AspNetCore.Components.WebAssembly.Server` package, serves the WASM static assets, maps API endpoints under `/api`, maps `/api/{**path}` to a 404 error-code response, and finally `MapFallbackToFile("index.html")` for client routes. No prerendering. One publish output, one App Service. (User-confirmed.)

*Alternative:* Blazor Web App with Server/Auto render modes — rejected: dual execution modes complicate localization and storage, and differ from how MAUI Hybrid runs the RCL.

### D3. Platform seams (interfaces in RCL, web implementations in App.Web)

| Interface (RCL `Platform/`) | Web implementation (App.Web) |
|---|---|
| `ILocalStorage` (get/set/remove string) | `window.localStorage` via JS interop |
| `ISecureTokenStorage` | `localStorage` (browsers have no secure store; documented limitation) |
| `IClientLanguageDetector` (returns tag or null) | `navigator.languages[0] ?? navigator.language` via JS interop, exceptions → null |
| `ILanguageActivator` (activate after a language is stored) | full reload to the target URL (`forceLoad`) |
| `IAdService` | `NoAdService` (no-op) |
| `IBillingService` | `UnsupportedBillingService` (reports "remove ads" not purchased/unsupported) |

JS interop functions live in a small `wwwroot/js/platform.js` in App.Web, not in the RCL, so MAUI can use native APIs instead.

### D4. Localization model

- `SupportedLanguages` catalog in RCL: `{ Code: "pt-BR", Culture: "pt-BR", FlagImage: "br.svg", NameKey }`, `{ Code: "en", Culture: "en-GB", FlagImage: "gb.svg", NameKey }`. Default `pt-BR`.
- Texts: one shared resource `Resources/AppStrings.resx` (default = Portuguese, assembly marked `[NeutralResourcesLanguage("pt-BR")]`) plus `AppStrings.en.resx`, consumed via `IStringLocalizer<AppStrings>`. One shared resource avoids dozens of tiny per-page files; split later if it grows. Language names are resx keys with identical values in both files, keeping the "no hardcoded UI strings" rule.
- Flags are small SVG files in `Algorist.App.UI/wwwroot/images/flags/`; flag emoji are not used because Windows browsers render them as letters.
- Applying a language sets `CultureInfo.DefaultThreadCurrentUICulture` to the language code and `DefaultThreadCurrentCulture` to its formatting culture (`en` → `en-GB`).
- App.Web sets `BlazorWebAssemblyLoadAllGlobalizationData=true` so ICU data for detection and formatting is available.

### D5. Startup language flow

`Program.cs` builds the host, then awaits `LanguageBootstrapper.InitializeAsync()` (RCL) before `RunAsync()`, following the documented WASM culture pattern:

1. Read `app.language` from `ILocalStorage`. Supported → apply, done.
2. Otherwise call `IClientLanguageDetector`. `LanguageResolver` (pure, unit-tested) classifies the tag: primary subtag `pt` → `pt-BR`; valid tag whose primary subtag is a predefined culture → `en`; null/invalid/unknown → unresolved.
3. Resolved → apply and store. Unresolved → apply `pt-BR` in memory only and set `LanguageState.SelectionRequired = true`.
4. The root component, on first render, redirects (replace) to `/select-language` when `SelectionRequired` is set and clears navigation history so "previous screen" is empty.

### D6. Changing the language at runtime

`SelectLanguageViewModel` (RCL) holds the options and selection. Apply: store the code, then call `ILanguageActivator.ActivateAsync(returnUrl)`. The web implementation reloads the app at `returnUrl`; startup then applies the stored language (D5 step 1). Reloading guarantees that satellite resource assemblies for the new culture are loaded and every component re-renders. A MAUI implementation can instead re-apply cultures and re-render in place. `returnUrl` comes from `INavigationHistory` (RCL service tracking the previous in-app location via `NavigationManager.LocationChanged`), falling back to `/sign-in`. Cancel navigates to the same target without storing anything.

*Alternative:* change culture in place and raise a "culture changed" event — rejected for web because satellite assemblies for a culture not loaded at startup may be missing, giving partly untranslated screens.

### D7. Error codes and server localization

- `Algorist.Contracts.ErrorCodes` holds stable string constants (initially `UNEXPECTED_ERROR`, `NOT_FOUND`, `VALIDATION_FAILED`).
- API errors are RFC 7807 `ProblemDetails` with a `code` extension member; a global exception handler maps unhandled exceptions to `UNEXPECTED_ERROR` (500) without leaking details.
- Client: `ErrorMessageTranslator` (RCL) maps code `X` to resx key `Error_X`, falling back to `Error_Unknown`.
- Client `HttpClient` gets a `LanguageHeaderHandler` that sets `Accept-Language` to the current language.
- Server: `UseRequestLocalization` with supported cultures `pt-BR` and `en`/`en-GB`, default `pt-BR`, Accept-Language provider only. `Algorist.Api.Application/Resources/EmailTexts.resx` (+ `.en`) is created with one placeholder key and a test proving the culture switch; real email templates come with the features that send them.

### D8. Layout and styling

Visual style (palette, rounded nav buttons, gift brandmark, light surfaces) follows `prompts/reference.html`; its content and structure are not copied.

- `MainLayout` (RCL) uses the Bootstrap grid: `container-fluid > row`, menu column `col-12 col-md-3 col-xl-2`, content column `col`. On `md+` the menu column is a full-height sticky flex column (brand, nav items, Profile pushed down with `mt-md-auto`). Below `md`, brand on top and the four items in a `d-flex` row of equal-width buttons (icon over label). Pure CSS/Bootstrap breakpoints, no JS. Breakpoint `md` = 768 px.
- Icons (Bootstrap Icons): Home `bi-house-door`, Events `bi-calendar-event`, Friends `bi-people`, Profile `bi-person-circle`. Brand logo: 🎁 emoji in a gradient square.
- `Algorist.App.UI/wwwroot/css/app.css` declares all `:root` variables (Portuguese `--cor-*` naming as in the user's example, e.g. `--cor-primaria: #2678f8`, `--cor-fundo`, `--cor-texto`, `--cor-borda`, `--raio-borda`, `--sombra-cartao`); component `.razor.css` files use only `var(...)`.
- `Algorist.App.Web/wwwroot/lib/bootstrap/` and `.../lib/bootstrap-icons/` contain only the needed files from the official 5.3.9 and 1.13.2 release packages (css, bundle js, icon font + css), committed to git; a `libman.json` records versions for future updates. `index.html` references only local paths. The loading indicator and error banner in `index.html` are icon-only/bilingual because they appear before resources load.

### D9. Routing

Routes (all pages in RCL): `/` → redirect to `/sign-in`; `/sign-in`, `/home`, `/events`, `/friends`, `/profile`, `/select-language`. Router lives in the RCL root component (`AppRoot`) with `NotFoundPage` set to a translated not-found page. All pages use `MainLayout` in this change (the later sign-in change will remove the menu from Sign In).

### D10. Database

`Algorist.Api.Infra` registers `MainDbContext` (no entity sets) with the SQL Server provider. `Program.cs` reads `MainDatabaseConnectionString` from configuration/environment and throws a clear startup error if it is empty. No schema change, therefore no EF Core migration in this change. The health check does not touch the database yet.

Local development uses .NET user secrets on `Algorist.Api` (a `UserSecretsId` in the csproj; ASP.NET Core loads user secrets automatically in the Development environment, and environment variables still override them). The developer database `AlgoristSecretSanta` already exists on LocalDB, so the value is set once with:

```
dotnet user-secrets set MainDatabaseConnectionString "Server=(localdb)\MSSQLLocalDB;Database=AlgoristSecretSanta;Trusted_Connection=True;TrustServerCertificate=True;" --project src/Algorist.Api
```

(The request said `user-secret add`; the actual CLI verb is `dotnet user-secrets set`.) The value lives in the user profile, outside the repo, and is not put in `launchSettings.json` or `appsettings*.json`.

*Alternative:* LocalDB string in `launchSettings.json` — rejected in favor of user secrets so no connection string is committed.

### D11. CI/CD

`.github/workflows/ci-cd.yml`: job `build-test` (checkout, setup-dotnet 10.0.x, restore, build Release, test) runs on push and pull request to `main`; job `deploy` (needs `build-test`, only on push to `main`) runs `dotnet publish src/Algorist.Api`, logs in with `azure/login` using OIDC (secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`) and deploys with `azure/webapps-deploy` to the app named by repository variable `AZURE_WEBAPP_NAME`. `MainDatabaseConnectionString` is set as an App Service app setting, never in the repo.

*Alternative:* publish-profile secret — simpler but long-lived credential; OIDC preferred.

## Risks / Trade-offs

- [Reload on language change loses unsaved in-page state] → Only the Select Language screen changes language and it has no other state; MAUI can switch in place.
- [`localStorage` is not secure token storage] → Accepted for web; hidden behind `ISecureTokenStorage` so MAUI can use SecureStorage. Mitigate XSS risk later with CSP when auth lands.
- [Full ICU globalization data increases WASM download size] → Needed for culture detection/formatting; revisit with custom ICU subset if size matters.
- [Startup fails if `MainDatabaseConnectionString` is missing, even though no entities exist yet] → Intentional fail-fast per spec; tests and local dev set the variable.
- [Committed third-party files can drift from declared versions] → `libman.json` pins versions; files taken only from official release packages.
- [Single shared resx may grow large] → Can be split per feature later without changing behavior.

## Migration Plan

Greenfield. Before the first deployment: create the Azure App Service (.NET 10, Linux or Windows) and SQL database, set the `MainDatabaseConnectionString` app setting, configure an Entra ID federated credential for the GitHub repo, and add the secrets/variable named in D11. Rollback: redeploy a previous successful workflow run.

## Open Questions

- Final brand logo artwork (the 🎁 emoji brandmark is a placeholder that can be swapped without spec changes).
- Exact color values beyond the reference palette; adjustable later through the CSS variables.
