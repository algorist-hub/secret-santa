# Spec Delta

## Purpose

Defines the webapp's responsive layout, main menu, navigation between the first screens, the initial screen, and the shared visual styling foundation that later features build on.

## ADDED Requirements

### Requirement: Initial screen is Sign In
When the app starts at its root address, it SHALL show the Sign In screen. In this change Sign In is a blank screen showing only its title, at the route `/sign-in`.

#### Scenario: App opened at root
- **WHEN** the user opens the app at `/` with a language already configured
- **THEN** the Sign In screen is shown with the translated title "Sign In"

#### Scenario: Language not yet configured
- **WHEN** the user opens the app with no stored language and detection succeeds
- **THEN** the detected language is applied and the Sign In screen is shown

### Requirement: Main menu items
The main menu SHALL contain, in order, the items Home, Events, Friends and Profile. Each item SHALL show an icon and a translated label and SHALL navigate to its screen: Home `/home`, Events `/events`, Friends `/friends`, Profile `/profile`.

#### Scenario: Navigate from the menu
- **WHEN** the user selects the Events item
- **THEN** the Events screen is shown

#### Scenario: Active item highlighted
- **WHEN** the current screen is one of the menu screens
- **THEN** that screen's menu item is visually marked as active and the others are not

### Requirement: Placeholder menu screens
The Home, Events, Friends and Profile screens SHALL each be blank except for their translated title.

#### Scenario: Placeholder content
- **WHEN** the user opens the Friends screen
- **THEN** the screen shows only the translated title "Friends"

### Requirement: Lateral menu on wide screens
When the viewport is at least 768 px wide, the main menu SHALL be a vertical menu on the left side of the screen, with the app logo and app title at its top, followed by the Home, Events and Friends items, and the Profile item placed at the bottom of the menu. Screen content SHALL be shown to the right of the menu.

#### Scenario: Desktop layout
- **WHEN** the webapp is shown in a viewport 1280 px wide
- **THEN** the menu is on the left, logo and title are above the items, and Profile is at the bottom of the menu

### Requirement: Top menu on narrow screens
When the viewport is narrower than 768 px, the app logo and app title SHALL be shown at the top of the screen, with all four menu items shown as buttons in a row directly below them and above the screen content. No horizontal page scrolling SHALL be required.

#### Scenario: Mobile browser layout
- **WHEN** the webapp is shown in a viewport 375 px wide
- **THEN** logo and title are at the top, the four menu buttons are in one row below them, and the content is below the buttons with no horizontal scroll

#### Scenario: Resizing the window
- **WHEN** the viewport width crosses 768 px
- **THEN** the layout switches between lateral and top menu without reloading the page

### Requirement: App branding
The app title "Amigo Presente" and a gift logo SHALL be shown with the main menu in both layouts.

#### Scenario: Branding visible
- **WHEN** any screen with the main menu is shown
- **THEN** the gift logo and the title "Amigo Presente" are visible

### Requirement: Unknown route
Navigating to a route that does not exist SHALL show a translated "page not found" message inside the app layout with a link to the initial screen.

#### Scenario: Unknown address
- **WHEN** the user navigates to `/does-not-exist`
- **THEN** a translated "page not found" message and a link to Sign In are shown

### Requirement: Themeable styling
Colors, spacing radii and shadows used by the webapp SHALL be defined as CSS custom properties in the root stylesheet and referenced by all component styles, so the visual theme can be changed in one place.

#### Scenario: Changing a theme color
- **WHEN** a developer changes the value of a color variable in the root stylesheet
- **THEN** every element using that color reflects the new value without other style edits
