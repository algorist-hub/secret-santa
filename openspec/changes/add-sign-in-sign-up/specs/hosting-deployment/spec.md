# Spec Delta

## MODIFIED Requirements

### Requirement: Static frontend libraries served locally
Bootstrap 5.3.9 and Bootstrap Icons 1.13.2 SHALL be served by the app from its own `wwwroot/lib` folder. The webapp SHALL NOT load any stylesheet, script or font from a CDN or other external origin, except Google's sign-in client script (`https://accounts.google.com/gsi/client`), which SHALL be loaded only when the Sign In screen needs it and Google sign-in is configured.

#### Scenario: Page loads without external requests
- **WHEN** the webapp is loaded in a browser on any screen other than Sign In
- **THEN** all Bootstrap and Bootstrap Icons assets are requested from the app's own origin and no external script is loaded

#### Scenario: Google script on Sign In
- **WHEN** the Sign In screen is shown and Google sign-in is configured
- **THEN** the only external script loaded is Google's sign-in client script

## ADDED Requirements

### Requirement: Authentication configuration
The server SHALL read the token signing key from the configuration key `JwtSigningKey` (environment variable when deployed, user secrets locally, never in source control) and SHALL fail at startup naming the key when it is missing or shorter than 32 characters. The Google client ID SHALL be read from `GoogleClientId`; when absent, Google sign-in is disabled.

#### Scenario: Signing key missing
- **WHEN** the server starts without `JwtSigningKey`
- **THEN** startup fails with an error naming `JwtSigningKey`

#### Scenario: Google client ID missing
- **WHEN** the server starts without `GoogleClientId`
- **THEN** the server starts and reports Google sign-in as unavailable to clients

### Requirement: Registration and session settings
The minimum registration age (`Registration:MinimumAgeYears`, default 18, must be 1 or more) and the token lifetime (`Auth:TokenLifetimeDays`, default 60, must be 1 or more) SHALL be server configuration values. An invalid value SHALL make startup fail with an error naming the key.

#### Scenario: Defaults
- **WHEN** neither setting is configured
- **THEN** the minimum age is 18 years and tokens expire 60 days after issue

#### Scenario: Invalid minimum age
- **WHEN** `Registration:MinimumAgeYears` is set to 0
- **THEN** startup fails with an error naming `Registration:MinimumAgeYears`

### Requirement: Database schema kept up to date
When the server starts in a deployed or development environment, it SHALL apply any pending database schema migrations to the main database before serving requests.

#### Scenario: First deployment with users
- **WHEN** this version is deployed to a database without the user tables
- **THEN** the tables are created at startup and sign-up works without manual database steps
