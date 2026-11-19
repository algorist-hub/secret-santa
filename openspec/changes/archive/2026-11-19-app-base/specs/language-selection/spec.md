# Spec Delta

## Purpose

Provides the Select Language screen, where the user picks one of the supported languages and applies it as the current language of the app on this device.

## ADDED Requirements

### Requirement: Select Language screen access
The Select Language screen SHALL be reachable at the route `/select-language`. In this change it SHALL NOT be linked from the main menu or any other screen; it SHALL only be opened by URL or automatically when first-run language detection fails.

#### Scenario: Open by URL
- **WHEN** the user navigates to `/select-language`
- **THEN** the Select Language screen is shown

#### Scenario: Not in the main menu
- **WHEN** the main menu is shown
- **THEN** it contains no item that opens the Select Language screen

### Requirement: Language options as radio buttons
The screen SHALL list every supported language as a radio button showing its flag and display name. Exactly one option SHALL be selected at a time. When the screen opens, the option for the current language SHALL be selected; if no language is stored yet, `pt-BR` SHALL be selected.

#### Scenario: Current language preselected
- **WHEN** the current language is `en` and the screen opens
- **THEN** the "English" option is selected and "Português do Brasil" is not

#### Scenario: No stored language
- **WHEN** no language is stored and the screen opens
- **THEN** the "Português do Brasil" option is selected

#### Scenario: Changing the selection does not apply it
- **WHEN** the user selects a different option without pressing Apply
- **THEN** the texts of the app stay in the current language and nothing is stored

### Requirement: Apply selected language
The screen SHALL have an "Apply" button. Pressing it SHALL apply the selected language (texts and formatting culture), store it locally as the current language, and return to the previous screen, or to the initial screen when there is no previous screen in the app.

#### Scenario: Apply a new language
- **WHEN** the current language is `pt-BR`, the user selects "English" and presses Apply
- **THEN** all texts switch to English, `en` is stored locally, and the app returns to the previous screen

#### Scenario: Apply after first-run fallback
- **WHEN** the screen was shown because language detection failed and the user presses Apply
- **THEN** the selected language is applied and stored, and the initial screen is shown

### Requirement: Cancel language selection
The screen SHALL have a "Cancel" button. Pressing it SHALL discard the selection without changing or storing the current language and return to the previous screen, or to the initial screen when there is no previous screen in the app.

#### Scenario: Cancel after changing the selection
- **WHEN** the user selects a different language and presses Cancel
- **THEN** the current language and stored value are unchanged and the previous screen is shown

#### Scenario: Cancel with no previous screen
- **WHEN** the screen was opened directly by URL and the user presses Cancel
- **THEN** the initial screen (Sign In) is shown

#### Scenario: Cancel after first-run fallback
- **WHEN** the screen was shown because language detection failed and the user presses Cancel
- **THEN** the app continues in `pt-BR` without storing a language, so the next app start runs detection again
