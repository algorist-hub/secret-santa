# Spec Delta

## Purpose

Lets people sign in to Amigo Presente with email and password or with Google, keeps them signed in on their device, and restricts signed-in-only screens and API endpoints to authenticated users.

## ADDED Requirements

### Requirement: Sign In screen content
The Sign In screen at `/sign-in` SHALL show, with translated texts: the title "Sign In", an Email field, a Password field (masked), a "Sign In" button, a "Sign in with Google" button, the label "Don't have an account?" followed by a "Sign Up" button, and a "Change Language" button. The main menu SHALL NOT be shown.

#### Scenario: Screen layout
- **WHEN** a signed-out user opens `/sign-in`
- **THEN** the title, Email and Password fields, Sign In, Sign in with Google, "Don't have an account?" with Sign Up, and Change Language are shown, and the main menu is not

#### Scenario: Go to Sign Up
- **WHEN** the user presses the Sign Up button
- **THEN** the Sign Up screen is shown

#### Scenario: Change language
- **WHEN** the user presses the Change Language button
- **THEN** the Select Language screen is shown, and Apply or Cancel there returns to Sign In

### Requirement: Sign In form validation
Before contacting the server, the Sign In form SHALL require a non-empty Email in a valid email format and a non-empty Password, showing a translated message next to each invalid field. The Sign In button SHALL be disabled while a sign-in request is in progress.

#### Scenario: Empty fields
- **WHEN** the user presses Sign In with Email and Password empty
- **THEN** both fields show a "required" message and no request is sent

#### Scenario: Invalid email format
- **WHEN** the user enters `ana@` as Email and presses Sign In
- **THEN** the Email field shows an "invalid email" message and no request is sent

### Requirement: Email and password sign-in
The system SHALL sign in a user whose email (compared case-insensitively, ignoring surrounding spaces) and password match an account that has a password. On success the client SHALL store the issued authentication token and show the Home screen.

#### Scenario: Correct credentials
- **WHEN** a registered user enters their email in any letter case and their correct password and presses Sign In
- **THEN** the user is signed in, the token is stored on the device and the Home screen is shown

### Requirement: Failed sign-in does not reveal accounts
When the email is not registered, the password is wrong, or the account has no password (Google-only account), the API SHALL respond with the error code `INVALID_CREDENTIALS`, and the client SHALL show the same translated "email or password is incorrect" message in every case and keep the entered email.

#### Scenario: Wrong password
- **WHEN** a registered email is entered with a wrong password
- **THEN** the message "email or password is incorrect" is shown and the user stays on Sign In

#### Scenario: Unknown email
- **WHEN** an unregistered email is entered
- **THEN** the same "email or password is incorrect" message is shown

#### Scenario: Google-only account with a password
- **WHEN** the email of an account created through Google is entered with any password
- **THEN** the same "email or password is incorrect" message is shown

### Requirement: Sign-in attempt limiting
The API SHALL limit sign-in and sign-up attempts per client address. When the limit is exceeded it SHALL respond with HTTP 429 and the error code `TOO_MANY_ATTEMPTS`, and the client SHALL show a translated "too many attempts, try again later" message.

#### Scenario: Repeated failures
- **WHEN** a client exceeds the allowed number of sign-in attempts within the limiting window
- **THEN** further attempts receive `TOO_MANY_ATTEMPTS` until the window ends

### Requirement: Google sign-in for registered users
Pressing "Sign in with Google" SHALL start Google's sign-in. The server SHALL verify the Google identity token. When the Google account is already linked to a user, or its verified email matches an existing account (which then becomes linked), the user SHALL be signed in, the token stored, and Home shown.

#### Scenario: Linked Google account
- **WHEN** a user whose account was created with Google signs in with the same Google account
- **THEN** the user is signed in and the Home screen is shown

#### Scenario: Email account signs in with Google
- **WHEN** a user registered with email and password signs in with a Google account whose verified email is the same
- **THEN** the Google account is linked to that user, the user is signed in, and later password sign-in keeps working

#### Scenario: Invalid Google token
- **WHEN** the server receives a Google token that is invalid, expired, issued for another app, or has an unverified email
- **THEN** it responds with the error code `GOOGLE_SIGN_IN_FAILED` and the client shows a translated error on Sign In

### Requirement: Google sign-in for unregistered users
When the Google account matches no user, the system SHALL NOT create an account yet. The client SHALL show the Sign Up screen pre-filled with the email, name and profile picture obtained from Google and ask for the remaining information.

#### Scenario: New Google user
- **WHEN** a person whose Google account matches no user completes Google sign-in
- **THEN** the Sign Up screen is shown with Email, Name and Avatar filled from Google, and no account exists until Sign Up is completed

#### Scenario: Google sign-in cancelled
- **WHEN** the user closes or cancels Google's sign-in
- **THEN** the user stays on Sign In and no error message is shown

### Requirement: Google sign-in availability
The "Sign in with Google" button SHALL be shown only when Google sign-in is configured on the server and the client can load Google's sign-in component; otherwise the button SHALL be hidden and email sign-in SHALL keep working.

#### Scenario: Google not configured
- **WHEN** the server has no Google client configured
- **THEN** the Sign In screen shows no Google button and email sign-in works

### Requirement: Persistent session on the device
After any successful sign-in or sign-up, the authentication token SHALL be stored locally on the device and SHALL survive app restarts. The token SHALL identify the user and SHALL expire after a server-configured lifetime.

#### Scenario: Reopen the app
- **WHEN** a signed-in user closes the app and opens it again on the same device
- **THEN** the user is still signed in without entering credentials

### Requirement: Session restore at app start
When the app starts at its root address, it SHALL show Home if a stored token is accepted by the server, and Sign In if no token is stored or the server rejects it (the rejected token SHALL be removed). Each accepted start SHALL replace the stored token with a newly issued one, so a user who opens the app within the token lifetime stays signed in indefinitely.

#### Scenario: Valid stored token
- **WHEN** the app starts with a stored token the server accepts
- **THEN** the token is renewed and the Home screen is shown

#### Scenario: No stored token
- **WHEN** the app starts with no stored token
- **THEN** the Sign In screen is shown

#### Scenario: Expired or rejected token
- **WHEN** the app starts with a stored token the server rejects
- **THEN** the token is removed and the Sign In screen is shown

#### Scenario: Server unreachable at start
- **WHEN** the app starts with a stored token and the server cannot be reached
- **THEN** the token is kept and the Home screen is shown

### Requirement: Signed-in-only screens
The Home, Events, Friends and Profile screens SHALL be available only to signed-in users. A signed-out user navigating to any of them SHALL be shown the Sign In screen. A signed-in user navigating to Sign In or Sign Up SHALL be shown the Home screen.

#### Scenario: Signed-out deep link
- **WHEN** a signed-out user navigates directly to `/events`
- **THEN** the Sign In screen is shown

#### Scenario: Signed-in user opens Sign In
- **WHEN** a signed-in user navigates to `/sign-in`
- **THEN** the Home screen is shown

### Requirement: Authenticated API requests
The client SHALL send the stored token with every API request. API endpoints that require a user SHALL respond with HTTP 401 and the error code `UNAUTHORIZED` when the token is missing, invalid or expired; when the client receives this response it SHALL remove the stored token and show the Sign In screen.

#### Scenario: Expired token during use
- **WHEN** the API answers a request from a signed-in client with `UNAUTHORIZED`
- **THEN** the client removes the stored token and shows the Sign In screen

#### Scenario: Request without a token
- **WHEN** a client calls an endpoint that requires a user without sending a token
- **THEN** the API responds with HTTP 401 and the error code `UNAUTHORIZED`
