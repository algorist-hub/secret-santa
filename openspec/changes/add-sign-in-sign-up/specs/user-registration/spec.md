# Spec Delta

## Purpose

Lets adults create an Amigo Presente account, either with email and password or with their Google identity, collecting the required and optional profile information under a configurable minimum age.

## ADDED Requirements

### Requirement: Sign Up screen
The Sign Up screen SHALL be at `/sign-up`, show the translated title "Sign Up", and SHALL NOT show the main menu. It SHALL have a "Sign Up" submit button and a link back to Sign In.

#### Scenario: Open Sign Up
- **WHEN** a signed-out user opens `/sign-up`
- **THEN** the Sign Up screen is shown without the main menu

#### Scenario: Back to Sign In
- **WHEN** the user presses the link back to Sign In
- **THEN** the Sign In screen is shown

### Requirement: Email registration form
When Sign Up is not reached from Google sign-in, the form SHALL ask for Email, Name, Date of birth, Password and Confirm password (all required), and Phone number, Address and Avatar (optional). Required fields SHALL be visibly marked.

#### Scenario: Form fields
- **WHEN** a signed-out user opens `/sign-up` directly
- **THEN** the form shows required Email, Name, Date of birth, Password and Confirm password, and optional Phone number, Address and Avatar

#### Scenario: Missing required field
- **WHEN** the user presses Sign Up with Name empty
- **THEN** the Name field shows a translated "required" message and no account is created

### Requirement: Registration field rules
The client and the API SHALL apply the same rules: Email in valid format, at most 254 characters, stored lowercase and trimmed; Name 1–100 characters after trimming; Phone number, if given, only digits, spaces, `+`, `(`, `)`, `-` with 8–15 digits; Address, if given, at most 300 characters. Violations SHALL show translated messages per field; the API SHALL reject them with `VALIDATION_FAILED`.

#### Scenario: Invalid phone
- **WHEN** the user enters `12ab` as Phone number
- **THEN** the Phone number field shows an "invalid phone number" message

#### Scenario: API rejects invalid data
- **WHEN** a sign-up request with a 101-character name reaches the API
- **THEN** the API responds with HTTP 400 and the error code `VALIDATION_FAILED` and creates no account

### Requirement: Strong password
A password SHALL have 8 to 128 characters and contain at least one uppercase letter, one lowercase letter, one digit and one symbol. The form SHALL show these rules, SHALL require Confirm password to match, and the API SHALL reject a weak password with the error code `WEAK_PASSWORD`. Passwords SHALL be stored only as a salted one-way hash.

#### Scenario: Weak password
- **WHEN** the user enters `password1` as Password
- **THEN** the Password field shows which rules are not met and no request is sent

#### Scenario: Confirmation mismatch
- **WHEN** Password and Confirm password differ
- **THEN** the Confirm password field shows a "passwords do not match" message

#### Scenario: Strong password accepted
- **WHEN** the user enters `Gift#2026box` in both password fields
- **THEN** no password message is shown

### Requirement: Configurable minimum age
Only people whose age in whole years on the current date is at least the minimum age SHALL be able to register. The minimum age SHALL be a server configuration value, defaulting to 18 when not set, and SHALL be readable by the client without signing in. It SHALL NOT be hardcoded in the client.

#### Scenario: Adult registers
- **WHEN** the minimum age is 18 and the user was born exactly 18 years before today
- **THEN** the date of birth is accepted

#### Scenario: Under minimum age
- **WHEN** the minimum age is 18 and the user is 17 years old
- **THEN** the client shows a translated message stating that the minimum age is 18, and the API, if called, rejects the request with the error code `UNDERAGE`

#### Scenario: Minimum age changed by configuration
- **WHEN** the server is configured with a minimum age of 21 and a 20-year-old tries to register
- **THEN** registration is refused and the message states the minimum age 21, with no code change

### Requirement: Date of birth validity
Date of birth SHALL be a calendar date that is not in the future and not before 1900-01-01, entered with a date picker and displayed in the current language's date format.

#### Scenario: Future date
- **WHEN** the user picks a date after today
- **THEN** the Date of birth field shows an "invalid date of birth" message

### Requirement: Unique email
Each email SHALL belong to at most one account. Registering an email already in use SHALL be rejected with the error code `EMAIL_ALREADY_REGISTERED`, and the client SHALL show a translated message suggesting Sign In.

#### Scenario: Email in use
- **WHEN** the user tries to register `Ana@Mail.com` and `ana@mail.com` is already registered
- **THEN** the account is not created and the "email already registered" message is shown

### Requirement: Optional avatar
The user MAY choose a JPEG, PNG or WebP image as Avatar, see a preview, and remove it before submitting. The client SHALL reduce the image to at most 512×512 pixels before sending. The API SHALL reject avatars that are not JPEG, PNG or WebP or are larger than 1 MB with the error code `INVALID_AVATAR`.

#### Scenario: Choose an avatar
- **WHEN** the user selects a 3000×2000 JPEG photo
- **THEN** a preview is shown and the uploaded image is at most 512×512 pixels

#### Scenario: Unsupported file
- **WHEN** the user selects a PDF file as Avatar
- **THEN** a translated "unsupported image" message is shown and no avatar is kept

### Requirement: Google registration
When Sign Up is reached from Google sign-in, Email SHALL be pre-filled from Google and read-only, Name SHALL be pre-filled and editable, the Google profile picture SHALL be shown as Avatar (replaceable with an uploaded image or removable), and Password fields SHALL NOT be shown. Date of birth SHALL be required; optional fields stay optional. The account SHALL be linked to the Google account and have no password.

#### Scenario: Complete Google registration
- **WHEN** a new Google user enters an adult date of birth and presses Sign Up
- **THEN** the account is created with the Google email, the entered name and the Google picture, linked to that Google account, and the user is signed in

#### Scenario: Google data no longer valid
- **WHEN** the Google sign-up is submitted after the Google sign-in data expired (more than 30 minutes) or was tampered with
- **THEN** the API responds with `GOOGLE_SIGN_IN_FAILED` and the client asks the user to sign in with Google again

#### Scenario: Page reloaded during Google sign-up
- **WHEN** the user reloads the Sign Up screen after arriving from Google sign-in
- **THEN** the Google data is no longer present and the email registration form is shown

### Requirement: Successful registration signs the user in
After an account is created, the system SHALL sign the user in, store the authentication token on the device, and show the Home screen.

#### Scenario: Email registration completes
- **WHEN** the user submits a valid email registration form
- **THEN** the account is created, the token is stored and the Home screen is shown
