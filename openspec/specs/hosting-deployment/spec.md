# hosting-deployment Specification

## Purpose

Defines how the API and the webapp are hosted together on a single server, how the main database connection is configured, and how the app is built, tested and deployed to Azure.

## Requirements

### Requirement: Single host for API and webapp
A single server application SHALL serve both the API and the webapp from the same origin. API endpoints SHALL live under the `/api` path prefix; all other non-file paths SHALL return the webapp so client-side routes work on direct navigation and page reload.

#### Scenario: API request is routed to the API
- **WHEN** a client sends a request to a path beginning with `/api/`
- **THEN** the request is handled by the API and never returns the webapp page

#### Scenario: Deep link loads the webapp
- **WHEN** a browser navigates directly to a webapp route such as `/events`
- **THEN** the server returns the webapp, which then renders the Events page

#### Scenario: Unknown API path
- **WHEN** a client requests a path under `/api/` that matches no endpoint
- **THEN** the server responds with HTTP 404 and does not return the webapp page

### Requirement: Health endpoint
The server SHALL expose an unauthenticated health endpoint at `/api/health` that reports whether the application is running.

#### Scenario: Application is running
- **WHEN** a client sends `GET /api/health`
- **THEN** the server responds with HTTP 200

### Requirement: Main database connection from environment
The server SHALL read the main database connection string from the configuration key `MainDatabaseConnectionString`, supplied by an environment variable in deployed environments and by .NET user secrets in local development. The connection string SHALL NOT be stored in source control.

#### Scenario: Connection string provided
- **WHEN** the server starts with `MainDatabaseConnectionString` set
- **THEN** database access uses that connection string

#### Scenario: Local development uses user secrets
- **WHEN** the server runs locally in the Development environment with `MainDatabaseConnectionString` stored in user secrets and no such environment variable
- **THEN** database access uses the user-secrets connection string

#### Scenario: Connection string missing
- **WHEN** the server starts without `MainDatabaseConnectionString` set
- **THEN** the server fails at startup with an error message naming the missing variable

### Requirement: Static frontend libraries served locally
Bootstrap 5.3.9 and Bootstrap Icons 1.13.2 SHALL be served by the app from its own `wwwroot/lib` folder. The webapp SHALL NOT load any stylesheet, script or font from a CDN or other external origin.

#### Scenario: Page loads without external requests
- **WHEN** the webapp is loaded in a browser
- **THEN** all Bootstrap and Bootstrap Icons assets are requested from the app's own origin

### Requirement: Continuous integration and deployment
A GitHub Actions workflow SHALL build the solution and run all automated tests on every push and pull request to `main`. On a push to `main` where build and tests succeed, the workflow SHALL deploy the server application to Azure App Service.

#### Scenario: Pull request validation
- **WHEN** a pull request targeting `main` is opened or updated
- **THEN** the workflow builds the solution and runs the tests, and does not deploy

#### Scenario: Deployment on main
- **WHEN** a commit is pushed to `main` and build and tests succeed
- **THEN** the workflow publishes the server application and deploys it to Azure App Service

#### Scenario: Failing tests block deployment
- **WHEN** any test fails on a push to `main`
- **THEN** the workflow fails and no deployment happens
