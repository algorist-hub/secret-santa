# Tasks

## 1. Packages and server configuration

- [ ] 1.1 Add central package versions (10.0.x where applicable) for `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Identity.Core`, `Google.Apis.Auth`, `Microsoft.AspNetCore.Components.Authorization`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Sqlite` (tests) and reference them from the projects named in design D3/D9/D11; verify `dotnet build AlgoristSecretSanta.slnx` succeeds
- [ ] 1.2 Add `ConfigurationKeys.JwtSigningKey` and `GoogleClientId`; in `Program.cs` fail at startup naming `JwtSigningKey` when missing or shorter than 32 characters; set a test key in `ApiFactory`; verify an Api.Tests test for the missing/short-key message (local user secrets and Azure settings for `JwtSigningKey` and `GoogleClientId` already exist)
- [ ] 1.3 Add `AuthOptions` (`Auth:TokenLifetimeDays`, default 60) and `RegistrationOptions` (`Registration:MinimumAgeYears`, default 18), both `[Range(1, …)]` with `ValidateOnStart`, and write the defaults to `appsettings.json`; verify Api.Tests that defaults apply when unset and that `Registration:MinimumAgeYears=0` fails startup naming the key

## 2. Shared contracts and rules

- [ ] 2.1 Add error codes `INVALID_CREDENTIALS`, `TOO_MANY_ATTEMPTS`, `GOOGLE_SIGN_IN_FAILED`, `UNDERAGE`, `EMAIL_ALREADY_REGISTERED`, `WEAK_PASSWORD`, `INVALID_AVATAR`, `UNAUTHORIZED` to `ErrorCodes` (and `All`); add `Error_<CODE>` entries to `AppStrings.resx` and `AppStrings.en.resx`; verify the existing resx key-parity test and an `ErrorMessageTranslator` test for every code pass
- [ ] 2.2 Add auth/registration DTOs to `Algorist.Contracts` per design D5 (sign-in, Google sign-in result with status, sign-up with optional avatar and ticket, token response, public settings); verify the solution builds and JSON round-trip tests serialize `status` as a string
- [ ] 2.3 Implement `PasswordPolicy` (8–128 chars, upper, lower, digit, symbol) returning each unmet rule; verify Contracts unit tests: `password1` fails upper+symbol, `Gift#2026box` passes, 129 chars fails
- [ ] 2.4 Implement `RegistrationRules` (email format/254, name 1–100 trimmed, phone allowed chars + 8–15 digits, address ≤300, email normalization lowercase+trim) returning field error codes; verify unit tests including `12ab` phone, 101-char name, `Ana@Mail.com ` normalized to `ana@mail.com`

## 3. Domain, persistence and migration

- [ ] 3.1 Add `User` and `UserAvatar` to `Algorist.Api.Domain/Users` and `AgePolicy.IsOldEnough`; verify Domain unit tests: birthday exactly N years ago passes, one day later fails, Feb-29 birth, minimum 21
- [ ] 3.2 Add EF configurations (tables, column types/lengths, unique `Email` index, filtered unique `GoogleSubject` index, `UserAvatars` 1:0..1 cascade) and `DbSet`s on `MainDbContext` per design D1; verify the project builds and a model test asserts both indexes exist
- [ ] 3.3 Add `IDesignTimeDbContextFactory<MainDbContext>` reading `MainDatabaseConnectionString` from environment/user secrets; verify `dotnet ef dbcontext info --project src/Algorist.Api.Infra --startup-project src/Algorist.Api` succeeds
- [ ] 3.4 Create EF Core migration `AddUsers` (`dotnet ef migrations add AddUsers --project src/Algorist.Api.Infra --startup-project src/Algorist.Api --output-dir Persistence/Migrations`) and review the generated code: column types, nullability, both unique indexes with the `GoogleSubject IS NOT NULL` filter, cascade FK, no unexpected tables; verify `dotnet ef database update` succeeds on LocalDB
- [ ] 3.5 Apply pending migrations at startup unless the environment is `Testing` (design D2); verify running `Algorist.Api` locally against a fresh LocalDB database creates `Users` and `UserAvatars`
- [ ] 3.6 Change `ApiFactory` to use SQLite in-memory with `EnsureCreated` and a fake `IGoogleIdTokenValidator` (design D11); verify existing Api.Tests still pass and a smoke test can insert and read a `User`

## 4. Application and infrastructure services

- [ ] 4.1 Implement `IUserRepository` and `IAvatarStore` (EF) with lookups by normalized email and Google subject; verify Api.Tests (SQLite) for lookup, unique-email violation mapped to a duplicate result, and avatar save
- [ ] 4.2 Implement `IdentityPasswordHasher` over `PasswordHasher<User>`; verify tests that hash ≠ password, verify succeeds for the right password and fails for a wrong one
- [ ] 4.3 Implement `JwtTokenIssuer` (claims `sub`, `email`, `name`; issuer/audience `amigo-presente`; lifetime from `AuthOptions`, `TimeProvider`) and register JwtBearer validation with `OnChallenge` writing `UNAUTHORIZED`; verify tests that an issued token authorizes a test endpoint and an expired/garbled token returns 401 with `code = UNAUTHORIZED`
- [ ] 4.4 Implement `GoogleIdTokenValidator` (Google.Apis.Auth, audience = `GoogleClientId`, require `email_verified`) and `GoogleRegistrationTicketService` (JWT, audience `google-registration`, 30 min); verify ticket tests: valid round-trip, expired after 31 min (fake `TimeProvider`), tampered signature rejected, normal auth token rejected as ticket

## 5. API endpoints

- [ ] 5.1 Add `GET /api/public-settings` returning `minimumAgeYears` and `googleClientId` (null when not configured); verify Api.Tests with and without `GoogleClientId` and with minimum age 21
- [ ] 5.2 Implement `PasswordSignIn` use case and `POST /api/auth/sign-in`; verify Api.Tests: correct credentials with different email case → token; wrong password, unknown email and Google-only account → 401/`INVALID_CREDENTIALS` with identical bodies
- [ ] 5.3 Implement `RegisterUser` for email sign-ups and `POST /api/auth/sign-up` (field rules, `WEAK_PASSWORD`, `UNDERAGE` using `RegistrationOptions` + `TimeProvider`, `EMAIL_ALREADY_REGISTERED`, hashed password); verify Api.Tests for each error code, success returning a token, and the stored password not equal to the plain text
- [ ] 5.4 Add avatar handling to sign-up (base64 decode, content type in jpeg/png/webp, magic-byte check, ≤1 MB, else `INVALID_AVATAR`); verify Api.Tests: valid PNG stored in `UserAvatars`, PDF bytes and 1.1 MB image rejected
- [ ] 5.5 Implement `GoogleSignIn` and `POST /api/auth/google` (match by subject, else link by verified email, else `RegistrationRequired` with ticket/profile; invalid token → `GOOGLE_SIGN_IN_FAILED`); verify Api.Tests for all four outcomes using the fake validator, and that no user row is created for `RegistrationRequired`
- [ ] 5.6 Extend sign-up for Google tickets (identity/email from ticket, no password, `useGoogleAvatar` stores `GoogleAvatarUrl`, invalid ticket → `GOOGLE_SIGN_IN_FAILED`); verify Api.Tests: account linked to subject with null `PasswordHash`, tampered/expired ticket rejected, and later Google sign-in returns `SignedIn`
- [ ] 5.7 Implement `RenewSession` and `POST /api/auth/renew` (bearer required; 401 when the user no longer exists); verify Api.Tests: renewed token has a later expiry, missing token → 401 `UNAUTHORIZED`
- [ ] 5.8 Add `UseForwardedHeaders` and the `auth` fixed-window rate-limit policy (10/min/IP) on sign-in, google and sign-up with `OnRejected` → 429 `TOO_MANY_ATTEMPTS`; verify an Api.Tests test that the 11th sign-in within a minute returns 429 with the code

## 6. Client session and routing

- [ ] 6.1 Add `AuthSession` (token, parsed claims, `SignInAsync`/`SignOutAsync` via `ISecureTokenStorage`, `Changed` event) and `AppAuthenticationStateProvider`; register in `AddAppUI`; verify UI.Tests that sign-in stores the token and state becomes authenticated, sign-out clears both
- [ ] 6.2 Add `AuthorizationHeaderHandler` to the `Api` HttpClient (Bearer header; 401 on non-`/api/auth/` requests → `SignOutAsync`); verify UI.Tests with a stub inner handler for header presence and sign-out on 401
- [ ] 6.3 Add `SessionBootstrapper` (no token → signed out; renew 200 → store new token; 401 → clear; network error/5xx → keep) and await it in `App.Web/Program.cs` after the language bootstrapper; verify UI.Tests for all four paths with a stub HTTP handler
- [ ] 6.4 Replace `AppRoutes.Initial` with `InitialFor(bool signedIn)` and update `Index`, `SelectLanguageViewModel` and `NotFound`; verify updated UI.Tests: `/` → Home when signed in, Sign In otherwise; Cancel with no history → Home when signed in
- [ ] 6.5 Wrap the router in `CascadingAuthenticationState` + `AuthorizeRouteView` with a `RedirectToSignIn` component, add `[Authorize]` to Home/Events/Friends/Profile, and navigate to Sign In when `AuthSession` signs out; verify UI.Tests: signed-out `/events` → `/sign-in`, signed-in placeholders still render

## 7. Layouts

- [ ] 7.1 Create `AuthLayout` (brand above a centered card column, no menu) with scoped CSS using only CSS variables, styled after `prompts/reference.html`; verify a UI.Tests test that it renders the brand and body but no `nav`, and a manual check at 375 px shows no horizontal scroll
- [ ] 7.2 Make `MainLayout` render the auth shell instead of the menu when signed out; verify UI.Tests: Select Language and not-found show the menu only when signed in, and the not-found link targets Home/Sign In accordingly

## 8. Sign In screen

- [ ] 8.1 Add resx texts (pt-BR and en) for Sign In: Email, Password, Sign In, "or", "Don't have an account?", Sign Up, Change Language, validation messages (required, invalid email), generic error; verify the resx key-parity test passes
- [ ] 8.2 Implement `AuthApiClient` (sign-in, google, sign-up, renew, public settings; problem responses → error codes) and `PublicSettings` cached service; verify UI.Tests with a stub handler mapping 401 `INVALID_CREDENTIALS` and 429 to error codes
- [ ] 8.3 Implement `SignInViewModel` (validation, busy state, sign-in → `AuthSession.SignInAsync` → Home; error code → translated message, keep email); verify UI.Tests for empty fields, `ana@` invalid, success navigation, and `INVALID_CREDENTIALS` message
- [ ] 8.4 Build `SignIn.razor` with `@layout AuthLayout`: form, Sign In button (disabled while busy), Google button container, "Don't have an account?" + Sign Up → `/sign-up`, Change Language → `/select-language`, redirect to Home when signed in; verify bUnit tests for all elements and both navigations, and that Select Language Cancel returns to `/sign-in`

## 9. Google sign-in on the client

- [ ] 9.1 Add `IGoogleSignInProvider` to `Platform/` and a `GoogleSignInButton` component that renders only when the provider is available and `googleClientId` is set; verify bUnit tests that the button is hidden without a client ID
- [ ] 9.2 Implement `GoogleIdentityServicesProvider` in App.Web (on-demand load of `https://accounts.google.com/gsi/client` in `platform.js`, `initialize` + `renderButton` with the current language, credential callback via `DotNetObjectReference`, load failure → unavailable) and register it; verify manually that the button appears on Sign In in both languages and the Network tab shows the script only on Sign In
- [ ] 9.3 Wire Google results in `SignInViewModel`: `SignedIn` → store token → Home; `RegistrationRequired` → fill `PendingGoogleRegistration` → `/sign-up`; `GOOGLE_SIGN_IN_FAILED` → translated message (add resx texts); verify UI.Tests for the three outcomes with a fake provider

## 10. Sign Up screen

- [ ] 10.1 Add resx texts (pt-BR and en) for Sign Up: title, all field labels, required marker, optional marker, password rule checklist items, mismatch, invalid phone/date/email/name/address, minimum-age message with `{0}` placeholder, unsupported image, Choose/Remove avatar, Sign Up button, back to Sign In; verify the resx key-parity test passes
- [ ] 10.2 Add a `RulesValidator` component for `EditForm` that runs `RegistrationRules`/`PasswordPolicy` and maps field error codes to `Validation_*` resx keys; verify bUnit tests that invalid phone and weak password show translated messages in both languages
- [ ] 10.3 Implement `SignUpViewModel` for email sign-up (fields, minimum age from `PublicSettings`, DOB range 1900-01-01..today, confirm match, submit → `AuthSession.SignInAsync` → Home, API error codes → messages); verify UI.Tests: 17-year-old shows the minimum-age message with 18, configured 21 shows 21, `EMAIL_ALREADY_REGISTERED` shows its message, success navigates Home
- [ ] 10.4 Build `SignUp.razor` with `@layout AuthLayout`: required/optional sections, `InputDate` with min/max, live password checklist, back link to Sign In, redirect to Home when signed in; verify bUnit tests for field presence, required markers and no main menu
- [ ] 10.5 Add the avatar picker (`InputFile` jpeg/png/webp, `RequestImageFileAsync` to ≤512×512 keeping format, preview, Remove, unsupported type message); verify bUnit test with a fake `IBrowserFile` for preview/remove and PDF rejection, and a manual check that a large photo uploads under 1 MB
- [ ] 10.6 Add Google mode: read `PendingGoogleRegistration`, email read-only, name editable, Google picture as avatar (replace/remove sets `useGoogleAvatar`), hide password fields, send the ticket; `GOOGLE_SIGN_IN_FAILED` → message asking to sign in with Google again; clear pending data after use; verify UI.Tests for pre-fill, hidden password fields, and that a fresh view model without pending data shows the email form

## 11. Integration and documentation

- [ ] 11.1 Document in `README.md` the new settings (`JwtSigningKey`, `GoogleClientId`, `Registration:MinimumAgeYears`, `Auth:TokenLifetimeDays`), the user-secrets commands, Google Cloud OAuth client setup (authorized JavaScript origins) and the startup migration; verify the documented commands run as written
- [ ] 11.2 End-to-end manual check against LocalDB in both languages: register by email → Home; restart app → Home; sign in with wrong password → generic error; Google sign-in as new user → pre-filled Sign Up → Home; delete the stored token → `/` shows Sign In; verify `dotnet test AlgoristSecretSanta.slnx` passes and `openspec validate add-sign-in-sign-up --strict` succeeds

## Workflow follow-up

- Done 2026-11-24: Google OAuth client created; `JwtSigningKey` and `GoogleClientId` set in local user secrets (`Algorist.Api`) and as Azure App Service environment variables.
- Archive the change after review and verify the archived specs.
