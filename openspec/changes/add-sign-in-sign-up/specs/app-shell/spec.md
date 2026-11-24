# Spec Delta

## ADDED Requirements

### Requirement: Initial screen depends on the session
When the app starts at its root address, it SHALL show the Home screen for a signed-in user and the Sign In screen otherwise. The "initial screen" used as a fallback by other screens SHALL follow the same rule.

#### Scenario: Signed out at root
- **WHEN** a signed-out user opens the app at `/` with a language already configured
- **THEN** the Sign In screen is shown with the translated title "Sign In"

#### Scenario: Signed in at root
- **WHEN** a signed-in user opens the app at `/`
- **THEN** the Home screen is shown

#### Scenario: Language not yet configured
- **WHEN** a signed-out user opens the app with no stored language and detection succeeds
- **THEN** the detected language is applied and the Sign In screen is shown

### Requirement: Screens without the main menu
The Sign In and Sign Up screens SHALL always be shown without the main menu, and the Select Language and "page not found" screens SHALL be shown without it when the user is signed out. These screens SHALL show the app logo and title above a centered content card, with no horizontal scrolling at 375 px width.

#### Scenario: Sign In on a phone
- **WHEN** the Sign In screen is shown in a viewport 375 px wide
- **THEN** no main menu is shown, the logo and title are above the form, and no horizontal scroll is needed

#### Scenario: Select Language while signed in
- **WHEN** a signed-in user opens `/select-language`
- **THEN** the screen is shown with the main menu

#### Scenario: Select Language while signed out
- **WHEN** a signed-out user opens `/select-language`
- **THEN** the screen is shown without the main menu

## MODIFIED Requirements

### Requirement: Unknown route
Navigating to a route that does not exist SHALL show a translated "page not found" message with a link to the initial screen (Home when signed in, Sign In otherwise), inside the main layout when signed in and without the main menu when signed out.

#### Scenario: Unknown address
- **WHEN** a signed-out user navigates to `/does-not-exist`
- **THEN** a translated "page not found" message and a link to Sign In are shown, without the main menu

#### Scenario: Unknown address while signed in
- **WHEN** a signed-in user navigates to `/does-not-exist`
- **THEN** a translated "page not found" message is shown with the main menu and a link to Home

## REMOVED Requirements

### Requirement: Initial screen is Sign In
**Reason**: The initial screen now depends on whether the user is signed in, and Sign In is no longer blank.
**Migration**: See "Initial screen depends on the session" in app-shell and "Sign In screen content" in user-auth.
