# Spec Delta

## Purpose

Defines which languages and cultures the app supports, how the current language is chosen, detected and stored on the client, and how user-visible texts, API errors and server-generated content are translated.

## ADDED Requirements

### Requirement: Supported languages
The app SHALL support exactly these languages, in this order:
- `pt-BR`: shown as the Brazil flag + "Português do Brasil"; formatting culture `pt-BR`. This is the default language.
- `en`: shown as the United Kingdom flag + "English"; formatting culture `en-GB`.

Each language's display name SHALL always be shown in that language itself, whatever the current language.

#### Scenario: Language list content
- **WHEN** any screen lists the available languages
- **THEN** it shows "Português do Brasil" with the Brazil flag first and "English" with the UK flag second

#### Scenario: Default language
- **WHEN** a text has no translation in the current language
- **THEN** the `pt-BR` text is shown

### Requirement: UI language and formatting culture applied together
Applying a language SHALL set both the UI language (translated texts) and the formatting culture (dates, numbers, currency) to that language's values in one step.

#### Scenario: Applying English
- **WHEN** the language `en` is applied
- **THEN** texts are shown in English and dates and numbers are formatted with the `en-GB` culture

#### Scenario: Applying Portuguese
- **WHEN** the language `pt-BR` is applied
- **THEN** texts are shown in Brazilian Portuguese and dates and numbers are formatted with the `pt-BR` culture

### Requirement: Current language stored locally
The current language SHALL be stored only on the client device and SHALL persist across app restarts. The main database SHALL NOT store the current language.

#### Scenario: Language persists after restart
- **WHEN** the user has applied `en` and later reopens the app on the same device
- **THEN** the app starts in English without asking or detecting again

#### Scenario: Stored value is not supported
- **WHEN** the locally stored language is not one of the supported languages
- **THEN** the app treats it as not configured and runs first-run detection

### Requirement: First-run language detection
When no current language is stored, the app SHALL detect the client's preferred language. Any Portuguese variant SHALL result in `pt-BR`; any other recognized language SHALL result in `en`. The detected language SHALL be applied and stored as the current language.

#### Scenario: Portuguese client
- **WHEN** no language is stored and the client's preferred language is `pt`, `pt-BR` or `pt-PT`
- **THEN** the app applies and stores `pt-BR`

#### Scenario: Non-Portuguese client
- **WHEN** no language is stored and the client's preferred language is a recognized non-Portuguese language such as `en-US`, `es` or `fr-FR`
- **THEN** the app applies and stores `en`

### Requirement: Detection failure falls back to language selection
If detecting the client's language fails, returns no value, or returns a value that is not a valid language tag, the app SHALL show the Select Language screen and SHALL NOT store any language until the user applies one.

#### Scenario: Detection error
- **WHEN** no language is stored and language detection throws an error or returns nothing
- **THEN** the app shows the Select Language screen with `pt-BR` preselected

#### Scenario: Unknown language value
- **WHEN** no language is stored and detection returns an unrecognizable value
- **THEN** the app shows the Select Language screen and no language is stored

### Requirement: No hardcoded user-visible text
Every user-visible text in the webapp SHALL come from translated resources available in all supported languages.

#### Scenario: Switching language changes all texts
- **WHEN** the user switches the current language
- **THEN** every label, title, menu item and button on the visible screen is shown in the new language

### Requirement: API errors as error codes
The API SHALL report errors with a stable, language-independent error code in the response body and SHALL NOT rely on the client reading a translated message. The client SHALL translate each error code into the current language and SHALL show a generic translated message for unknown codes.

#### Scenario: Known error code
- **WHEN** the API returns an error with a code the client knows
- **THEN** the client shows that code's message in the current language

#### Scenario: Unknown error code
- **WHEN** the API returns an error code the client does not know
- **THEN** the client shows a generic translated error message

### Requirement: Server-side localized content
Content produced by the server for people, such as emails, SHALL be localized on the server using the language sent by the client with each API request. Requests without a supported language SHALL use `pt-BR`.

#### Scenario: Request carries the current language
- **WHEN** the webapp calls the API
- **THEN** the request carries the current language so the server can localize its content

#### Scenario: Request without a supported language
- **WHEN** the server receives a request with no language or an unsupported one
- **THEN** server-side content for that request uses `pt-BR`
