# Tasks

## 1. Solution scaffolding

- [x] 1.1 Add `global.json` (SDK 10.0.x, rollForward latestFeature), `Directory.Build.props` (net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors) and `Directory.Packages.props` (central package management); verify `dotnet --version` resolves a 10.0 SDK in the repo root
- [x] 1.2 Create class libraries `Algorist.Api.Domain`, `Algorist.Api.Application`, `Algorist.Api.Infra`, `Algorist.Contracts` under `src/` with project references per design D1; verify `dotnet build` succeeds for each
- [x] 1.3 Create Razor Class Library `Algorist.App.UI` and Blazor WebAssembly project `Algorist.App.Web` (references App.UI, `BlazorWebAssemblyLoadAllGlobalizationData=true`); verify both build
- [x] 1.4 Create ASP.NET Core project `Algorist.Api` referencing Application, Infra, Contracts and App.Web, with `Microsoft.AspNetCore.Components.WebAssembly.Server`; verify it builds
- [x] 1.5 Create test projects `tests/Algorist.Api.Tests` (xUnit, `Microsoft.AspNetCore.Mvc.Testing`) and `tests/Algorist.App.UI.Tests` (xUnit, bUnit) with one trivial passing test each; create `AlgoristSecretSanta.slnx` including all projects; verify `dotnet build AlgoristSecretSanta.slnx` and `dotnet test AlgoristSecretSanta.slnx` succeed and no project name contains "SecretSanta"

## 2. Server host, database wiring and error codes

- [x] 2.1 Add `MainDbContext` (no entity sets) in `Algorist.Api.Infra` with a DI extension using the SQL Server provider; read `MainDatabaseConnectionString` from configuration in `Algorist.Api` and throw a startup error naming the key when missing; verify with an Api.Tests test that startup without the value fails with that message (no migration: no schema change)
- [x] 2.2 Enable user secrets on `Algorist.Api` (`dotnet user-secrets init --project src/Algorist.Api`) and run `dotnet user-secrets set MainDatabaseConnectionString "Server=(localdb)\MSSQLLocalDB;Database=AlgoristSecretSanta;Trusted_Connection=True;TrustServerCertificate=True;" --project src/Algorist.Api` (the database already exists); verify `dotnet user-secrets list --project src/Algorist.Api` shows the key, no connection string appears in any committed file, and a temporary startup check of `MainDbContext.Database.CanConnectAsync()` returns true when running locally
- [x] 2.3 Add `ErrorCodes` (`UNEXPECTED_ERROR`, `NOT_FOUND`, `VALIDATION_FAILED`) to `Algorist.Contracts`; add global exception handler returning ProblemDetails with `code` extension; verify an Api.Tests test that a throwing test endpoint returns 500 with `code = UNEXPECTED_ERROR` and no exception details
- [x] 2.4 Map `/api/health` (health checks, no DB check) and an `/api/{**path}` fallback returning 404 ProblemDetails with `code = NOT_FOUND`; verify Api.Tests: `GET /api/health` → 200, `GET /api/does-not-exist` → 404 JSON with the code
- [x] 2.5 Serve the WASM webapp from `Algorist.Api` (static web assets + `MapFallbackToFile("index.html")` after API routes); verify by running `Algorist.Api` locally that `/events` returns the webapp page and `/api/x` still returns 404 JSON
- [x] 2.6 Configure server request localization (supported `pt-BR`, `en`/`en-GB`, default `pt-BR`, Accept-Language only) and add `Algorist.Api.Application/Resources/EmailTexts.resx` + `EmailTexts.en.resx` with one placeholder key; verify an Api.Tests test that the localized value differs for `Accept-Language: en` vs. none/`fr` (falls back to pt-BR)

## 3. Static assets and styling foundation

- [x] 3.1 Copy Bootstrap 5.3.9 (css + bundle js) and Bootstrap Icons 1.13.2 (css + fonts) from official release packages into `Algorist.App.Web/wwwroot/lib/`, add `libman.json` pinning both versions; verify the files are present and contain the expected version banners
- [x] 3.2 Create `Algorist.App.UI/wwwroot/css/app.css` with all `:root` CSS variables (`--cor-*`, `--raio-*`, `--sombra-*`) based on the reference palette, plus base body/link styles using only `var(...)`; verify by grep that no hex color appears outside the `:root` block
- [x] 3.3 Write `Algorist.App.Web/wwwroot/index.html` referencing only local stylesheets/scripts (lib, `_content/Algorist.App.UI/css/app.css`, scoped styles), with icon-only loading indicator and bilingual error banner; verify in the browser Network tab that the webapp makes no requests to external origins
- [x] 3.4 Add flag SVGs `br.svg` and `gb.svg` under `Algorist.App.UI/wwwroot/images/flags/`; verify they load at `/_content/Algorist.App.UI/images/flags/br.svg` when running the app

## 4. Platform seams

- [x] 4.1 Define interfaces in `Algorist.App.UI/Platform/`: `ILocalStorage`, `ISecureTokenStorage`, `IClientLanguageDetector`, `ILanguageActivator`, `IAdService`, `IBillingService`; verify the RCL builds and has no reference to WebAssembly-specific packages
- [x] 4.2 Implement web versions in `Algorist.App.Web` (`platform.js` + JS-interop `LocalStorage`, `LocalStorageTokenStorage`, `BrowserLanguageDetector` returning null on any exception, `ReloadLanguageActivator` using `forceLoad`, `NoAdService`, `UnsupportedBillingService`) and register them in `Program.cs`; verify the app starts and a manual `localStorage` round-trip works from a debug page or test harness

## 5. Localization core

- [x] 5.1 Add `Resources/AppStrings.resx` (Portuguese default, `[NeutralResourcesLanguage("pt-BR")]`) and `AppStrings.en.resx` with keys for app title, menu items, page titles, Sign In, Select Language screen (title, Apply, Cancel), language names, not-found page and error messages (`Error_UNEXPECTED_ERROR`, `Error_NOT_FOUND`, `Error_VALIDATION_FAILED`, `Error_Unknown`); register `AddLocalization`; verify a UI.Tests test that both files have identical key sets
- [x] 5.2 Implement `SupportedLanguages` catalog (pt-BR/pt-BR/br.svg default, en/en-GB/gb.svg) and `LanguageResolver`; verify UI.Tests: `pt`, `pt-BR`, `pt-PT` → pt-BR; `en-US`, `es`, `fr-FR` → en; null, empty, `xx-invalid!!`, unknown tag → unresolved
- [x] 5.3 Implement `LanguageState`/culture applier setting UI culture to the code and formatting culture to its culture (`en` → `en-GB`); verify UI.Tests that applying `en` yields `CurrentUICulture = en` and `CurrentCulture = en-GB` (date formatted dd/MM/yyyy)
- [x] 5.4 Implement `LanguageBootstrapper.InitializeAsync` per design D5 (stored → apply; unsupported stored → detect; detect resolved → apply + store; unresolved → pt-BR in memory + `SelectionRequired`); call it in `Program.cs` before `RunAsync`; verify UI.Tests with fake storage/detector covering all four paths including detector exception
- [x] 5.5 Implement `ErrorMessageTranslator` (code → `Error_<code>`, fallback `Error_Unknown`) and `LanguageHeaderHandler` setting `Accept-Language` on the API `HttpClient`; verify UI.Tests for known/unknown codes and that the handler adds the current language header

## 6. App shell and navigation

- [x] 6.1 Implement `AppRoot` in the RCL (Router over RCL assembly, `NotFoundPage` = translated `NotFound` page with link to `/sign-in`, first-render redirect to `/select-language` when `SelectionRequired`) and `INavigationHistory` tracking the previous in-app URL; mount `AppRoot` from `Algorist.App.Web`; all texts from resx; verify UI.Tests for history tracking and bUnit test that NotFound renders the translated message and link
- [x] 6.2 Implement `MainLayout` with Bootstrap grid per design D8 (brand 🎁 + "Amigo Presente" from resx, nav items Home/Events/Friends/Profile with Bootstrap Icons, active state via `NavLink`, Profile pushed to bottom on `md+`, row of buttons below brand on `<md`), styles in `MainLayout.razor.css` using only CSS variables; verify bUnit test renders four items in order with correct hrefs and no Select Language link
- [x] 6.3 Create pages `/` (redirect to `/sign-in`), `/sign-in`, `/home`, `/events`, `/friends`, `/profile`, each showing only its translated title; verify bUnit tests per page title in pt-BR and en and that `/` navigates to `/sign-in`
- [x] 6.4 Manually verify responsive layout in the browser at 1280 px (lateral menu, Profile at bottom) and 375 px (brand on top, one row of four buttons, no horizontal scroll), and that resizing across 768 px switches layouts without reload

## 7. Select Language screen

- [x] 7.1 Implement `SelectLanguageViewModel` (options from catalog, preselect current language or pt-BR when none stored, `ApplyAsync` stores code and calls `ILanguageActivator` with history target or `/sign-in`, `Cancel` navigates to same target without storing; after first-run fallback target is `/sign-in`); verify UI.Tests covering preselection, apply, cancel, cancel-without-history and fallback cases
- [x] 7.2 Implement `/select-language` page: radio list with flag image and translated name, Apply and Cancel buttons, all texts from resx, styles via CSS variables; verify bUnit tests that the current language radio is checked, changing selection does not alter culture or storage, and buttons invoke the view model
- [x] 7.3 Manually verify end to end: clear `localStorage`, set browser language to `pt-PT` → app opens in Portuguese with `pt-BR` stored; set to `de` → English with `en` stored; open `/select-language`, choose English, Apply → all texts change and the previous screen is shown; Cancel from a direct URL → Sign In

## 8. CI/CD

- [x] 8.1 Add `.github/workflows/ci-cd.yml` per design D11 (build-test on push/PR to `main`; deploy job on push to `main` only, needs build-test, OIDC `azure/login`, `azure/webapps-deploy` with `AZURE_WEBAPP_NAME`); verify YAML with `actionlint` (or a dry PR run) and that the deploy job has the `if: github.event_name == 'push'` guard
- [x] 8.2 Update `README.md` with solution layout, local run instructions (setting `MainDatabaseConnectionString` via `dotnet user-secrets set`, `dotnet run --project src/Algorist.Api`), test command and required GitHub secrets/variable and App Service setting; verify the documented commands run as written

## 9. Integration check

- [x] 9.1 Run `dotnet build AlgoristSecretSanta.slnx -c Release` and `dotnet test AlgoristSecretSanta.slnx -c Release` from a clean clone, then `dotnet publish src/Algorist.Api -c Release` and run the published output with the `MainDatabaseConnectionString` environment variable set (user secrets are not loaded outside Development); verify `/api/health` returns 200 and `/home` deep link renders the webapp with the menu

## Workflow follow-up

- After the first push to `main`, confirm the GitHub Actions deploy succeeds and the Azure App Service URL serves the webapp and `/api/health`.
- Archive the change after review with `/opsx:archive`.
