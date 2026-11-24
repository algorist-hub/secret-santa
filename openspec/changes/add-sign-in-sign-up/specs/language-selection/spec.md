# Spec Delta

## MODIFIED Requirements

### Requirement: Select Language screen access
The Select Language screen SHALL be reachable at the route `/select-language`. It SHALL be opened from the "Change Language" button on the Sign In screen, by URL, or automatically when first-run language detection fails. It SHALL NOT be an item of the main menu.

#### Scenario: Open by URL
- **WHEN** the user navigates to `/select-language`
- **THEN** the Select Language screen is shown

#### Scenario: Open from Sign In
- **WHEN** the user presses "Change Language" on the Sign In screen
- **THEN** the Select Language screen is shown

#### Scenario: Not in the main menu
- **WHEN** the main menu is shown
- **THEN** it contains no item that opens the Select Language screen

### Requirement: Cancel language selection
The screen SHALL have a "Cancel" button. Pressing it SHALL discard the selection without changing or storing the current language and return to the previous screen, or to the initial screen when there is no previous screen in the app.

#### Scenario: Cancel after changing the selection
- **WHEN** the user selects a different language and presses Cancel
- **THEN** the current language and stored value are unchanged and the previous screen is shown

#### Scenario: Cancel with no previous screen
- **WHEN** the screen was opened directly by URL and the user presses Cancel
- **THEN** the initial screen is shown: Home when signed in, Sign In otherwise

#### Scenario: Cancel after first-run fallback
- **WHEN** the screen was shown because language detection failed and the user presses Cancel
- **THEN** the app continues in `pt-BR` without storing a language, so the next app start runs detection again
