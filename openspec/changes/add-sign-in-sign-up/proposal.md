# Proposal

## Why

Amigo Presente has no users yet: Sign In is a blank placeholder and every screen is open to anyone. Events, gift lists and friends belong to a person, so the app needs accounts, sign-in (email/password and Google) and a persistent session before those features can be built.

## What Changes

- User accounts in the main database: email, name, date of birth, optional phone, address and avatar; password hash for email accounts; Google identity link for Google accounts.
- API endpoints for email/password sign-in, Google sign-in, sign-up, session renewal and public registration settings, reporting errors as error codes.
- Sign In screen (no main menu): email/password form, "Sign In", "Sign in with Google", "Don't have an account?" + "Sign Up", and "Change Language".
- Sign Up screen (no main menu): required Email, Name, Date of birth (plus Password and Confirm password for email sign-ups); optional Phone number, Address, Avatar. From Google sign-in, Email, Name and Avatar are pre-filled and no password is asked.
- Only adults can register. The minimum age is server configuration (default 18), enforced by the API and validated by the client.
- Strong-password policy shared by client and server.
- The authentication token is stored on the device so the user stays signed in across restarts.
- App start goes to Home when signed in, otherwise Sign In. Home, Events, Friends and Profile require a signed-in user.
- **BREAKING** (spec-level): the initial screen is no longer always Sign In; Select Language is linked from Sign In; the webapp may load Google's sign-in script.

Out of scope: sign-out UI, password reset, email verification, profile editing, showing avatars outside Sign Up, registration of minors.

## Capabilities

### New Capabilities
- `user-auth`: signing in with email/password or Google, issuing and storing the authentication token, restoring the session at app start, and restricting signed-in-only screens.
- `user-registration`: the Sign Up screen and account creation, including fields, Google pre-fill, strong passwords, configurable minimum age and avatar upload.

### Modified Capabilities
- `app-shell`: initial screen depends on the session; Sign In, Sign Up, and Select Language / not-found when signed out, are shown without the main menu.
- `language-selection`: Select Language is reachable from Sign In; its "initial screen" fallback follows the session.
- `hosting-deployment`: Google Identity Services script allowed as an external script; new required server configuration.

## Impact

- **Server layers**: `User` and `UserAvatar` entities, sign-in/sign-up use cases, password hashing, JWT issuing, Google ID token validation, first EF Core migration.
- **API**: `/api/auth/*` and `/api/public-settings` endpoints, JWT bearer authentication, rate limiting, new error codes in `Algorist.Contracts`.
- **RCL**: Sign In and Sign Up pages, menu-less auth layout, session and authorization routing, bearer-token HTTP handler, `IGoogleSignInProvider` platform interface, `.resx` texts.
- **Webapp**: Google Identity Services implementation, startup session restore.
- **Packages**: JwtBearer, Google.Apis.Auth, Microsoft.Extensions.Identity.Core, Components.Authorization, EF Core Design/Sqlite (tests).
- **Deployment**: new App Service settings `JwtSigningKey` and `GoogleClientId`; a Google Cloud OAuth client must be created.
