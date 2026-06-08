# Feature Specification: Frontend Demo Mode

**Feature Branch**: `002-frontend-demo-mode`

**Created**: 2026-06-07

**Status**: Draft — Ready for planning

## Overview

The Frontend Demo Mode feature enables public visitors to explore the admin interface of the Clinical Conductor API Mock System without requiring authentication. When a user is not signed in, all admin feature pages display a read-only demo experience backed by realistic pre-defined data, with a clear indication that the session is in demo mode and a prompt to authenticate for full access. The feature supports the project's goal of serving as a public showcase of what is achievable with generative AI-assisted engineering.

## User Scenarios & Testing

### User Story 1 — Browse the Admin Interface Without Authentication (Priority: P1)

A visitor arrives at the publicly deployed admin interface without any credentials. Rather than seeing a blank screen or an access-denied message, they can immediately browse the Auth Settings, Monitoring, and Test Data pages — each populated with realistic-looking data — to understand the system's capabilities. Every page they view is clearly labeled as "Demo Mode" so they understand they are seeing a simulated state, not live data.

**Why this priority**: The entire purpose of demo mode is to allow unauthenticated public visitors to explore the system. Without this, the feature does not exist. It must be implemented before any other story to deliver any value.

**Independent Test**: A visitor can open the application, navigate to each of the three admin feature pages (Auth Settings, Monitoring, Test Data), and see realistic content on all of them — with no credentials entered and no error states — within the first 30 seconds of arriving.

**Acceptance Scenarios**:

1. **Given** a visitor arrives at the application without a valid admin session, **When** they navigate to the Auth Settings page, **Then** the page displays a realistic pre-defined authentication configuration with a visible "Demo Mode" indicator.
2. **Given** a visitor arrives at the application without a valid admin session, **When** they navigate to the Monitoring page, **Then** the page displays a realistic set of mock request logs and statistics with a visible "Demo Mode" indicator.
3. **Given** a visitor arrives at the application without a valid admin session, **When** they navigate to the Test Data page, **Then** the page displays realistic patient counts and data management controls with a visible "Demo Mode" indicator.
4. **Given** a visitor is viewing any demo mode page, **When** they look at the page, **Then** the "Demo Mode" label is visible without scrolling.

---

### User Story 2 — Understand How to Access the Real System (Priority: P1)

A visitor exploring the demo interface wants to go further and actually interact with the system. On each demo page they see a clear, prominent message explaining that the current view is read-only demo data and that they can authenticate via the Admin Access section to interact with the live system.

**Why this priority**: Without this guidance, visitors who want real access have no path forward. This is a prerequisite to converting interested visitors into actual users of the system.

**Independent Test**: A visitor on any demo page can immediately identify the instruction to authenticate without searching for it, and can navigate to the Admin Access section directly from that prompt.

**Acceptance Scenarios**:

1. **Given** a visitor is viewing any demo mode page, **When** they look at the demo mode indicator, **Then** they see a clear prompt explaining how to access the real system (e.g., directing them to the Admin Access tab).
2. **Given** a visitor clicks the call-to-action in the demo mode indicator, **When** they are navigated to the Admin Access tab, **Then** the Admin Access page is displayed in its normal functional state.
3. **Given** a visitor has not authenticated, **When** they visit the Admin Access tab directly, **Then** it is displayed normally — it is never shown in demo mode.

---

### User Story 3 — Explore Interactive Controls in the Demo (Priority: P2)

A visitor wants to understand the workflow and interaction patterns of the admin interface. They can click buttons, interact with form controls, and navigate between sections on all demo pages. Although no real operations are performed, the controls are present and clickable so the visitor understands the intended user experience.

**Why this priority**: Static screens alone communicate layout, but interactive controls communicate the intended workflow. This is a secondary enhancement to the core demo experience.

**Independent Test**: A visitor can click every visible button on a demo page (e.g., "Save," "Generate," "Reset," "Filter") without the app crashing, throwing errors, or navigating away unexpectedly.

**Acceptance Scenarios**:

1. **Given** a visitor is on any demo mode page, **When** they click any button or interactive control, **Then** the application does not produce errors, does not navigate away unexpectedly, and does not make any backend requests.
2. **Given** a visitor interacts with form inputs on a demo page, **When** they type or select values, **Then** the input responds normally to user interaction (text appears, selections register) even though no operation is submitted.

---

### User Story 4 — Transition to Live Mode After Authentication (Priority: P2)

A visitor decides to authenticate and enters their admin key on the Admin Access page. After a successful session is established, all pages immediately show live data from the real system — no page refresh required.

**Why this priority**: Seamless transition from demo to live mode completes the visitor journey. Without it, authenticated users might still see stale demo data, which would be confusing and undermine trust.

**Independent Test**: After authenticating, a visitor can navigate to any admin feature page and see live system data instead of the pre-defined demo content, without refreshing the browser.

**Acceptance Scenarios**:

1. **Given** a visitor has authenticated successfully, **When** they navigate to the Auth Settings page, **Then** the real system configuration is displayed with no demo mode indicator.
2. **Given** a visitor has authenticated successfully, **When** they navigate to the Monitoring page, **Then** live request logs are displayed with no demo mode indicator.
3. **Given** a visitor has an active session that expires, **When** they next interact with an admin feature page, **Then** the page reverts to demo mode automatically without requiring a full page reload.

---

### Edge Cases

- What is shown on a demo page if the pre-defined mock data fails to load due to a code error? The page must still render rather than showing a blank or error state.
- If the admin key is not configured (open local dev mode), demo mode must not activate — admin pages must behave as they do today (fully functional with no auth required).
- If a visitor's session expires while they are actively viewing a page, the page must revert to demo mode without a full reload and without displaying an unhandled error.
- If the visitor opens multiple browser tabs — one authenticated, one not — each tab must independently reflect its own authentication state.
- If the backend is completely offline, demo pages must still render with mock data. An authentication attempt on the Admin Access page will fail, but that failure must not break the demo experience on other pages.

## Requirements

### Functional Requirements

- **FR-001**: When a visitor is not authenticated, all admin feature pages (Auth Settings, Monitoring, and Test Data) MUST automatically display in demo mode without requiring any user action or configuration.
- **FR-002**: Each demo mode page MUST display a visually distinct, always-visible indicator containing a "Demo Mode" label and a prompt directing the visitor to the Admin Access section to authenticate.
- **FR-003**: Demo mode pages MUST display pre-defined, realistic mock data in all data-bearing fields, sections, and charts — no fields may display placeholder text (e.g., "N/A," "—," or "0") where real data would normally appear, except where zero is a plausible real value.
- **FR-004**: Demo mode MUST NOT make any requests to the backend that read from or write to the database. All data shown in demo mode MUST be sourced from locally defined mock data within the frontend.
- **FR-005**: All interactive controls (buttons, dropdowns, form inputs, toggles) on demo mode pages MUST be rendered and visually consistent with their live-mode counterparts.
- **FR-006**: Clicking buttons or submitting forms in demo mode MUST NOT produce errors, navigate to error pages, or trigger backend requests.
- **FR-007**: The Admin Access page MUST always be displayed in its normal, fully functional state regardless of authentication status — it is never shown in demo mode.
- **FR-008**: When a visitor successfully authenticates, all admin feature pages MUST transition from demo mode to live mode without requiring a full browser page refresh.
- **FR-009**: When a visitor's admin session expires, all admin feature pages MUST revert to demo mode automatically without requiring a full browser page refresh.
- **FR-010**: Demo mode MUST be suppressed when the system is running without an admin key configured (open/local dev mode), preserving the zero-friction local development experience.
- **FR-011**: The Monitoring page in demo mode MUST display a realistic set of mock request log entries covering a range of HTTP methods, paths, status codes, and timestamps.
- **FR-012**: The Test Data page in demo mode MUST display a realistic patient count and populate all visible data fields with plausible content; all action buttons (Generate, Reset, Look Up) MUST be present and clickable.
- **FR-013**: The Auth Settings page in demo mode MUST display a realistic pre-defined authentication configuration showing a specific auth mode and associated credential fields.
- **FR-014**: Demo mode MUST function fully even when the backend is offline or unreachable — all demo pages MUST render with their pre-defined mock data regardless of backend availability.
- **FR-015**: When the backend is offline and a visitor attempts to authenticate via the Admin Access page, the authentication attempt MUST fail gracefully with a clear error message; demo mode on all other pages MUST remain unaffected.

### Key Entities

- **DemoSession**: The client-side state representing an unauthenticated visit where demo mode is active. Not persisted; derived from the absence of a valid admin session combined with the system being in admin-key-protected mode.
- **MockDemoData**: A set of pre-defined, static data values used to populate demo mode pages. Defined entirely within the frontend; never fetched from the backend.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A first-time visitor can view all three demo pages with realistic content within 10 seconds of opening the application, with no credentials entered and no loading errors.
- **SC-002**: The demo mode indicator is visible on every admin feature page without scrolling, on both standard desktop and mobile viewports in current versions of Chrome, Firefox, and Safari.
- **SC-003**: Every data-bearing field on every demo page shows a realistic, non-placeholder value — verifiable by visual inspection of each page.
- **SC-004**: Zero backend requests (beyond the session-minting endpoint on the Admin Access page) are generated when visiting demo mode pages, as verified by network inspection.
- **SC-005**: After successful authentication, all pages display live data within 3 seconds without a full page reload.
- **SC-006**: After session expiry, all pages revert to demo mode within 3 seconds of the expiry event, without a full page reload.

## Assumptions

- Demo mode is a purely frontend concern — no backend changes are required to support it. Because all demo data is locally defined, demo pages remain functional even if the backend is completely offline.
- The Admin Access page (where the admin key is entered) is never subject to demo mode; it remains fully functional at all times.
- Mock data for demo mode is static and pre-defined within the frontend — it does not change between page visits, refreshes, or browser sessions.
- Demo mode activation is determined entirely by whether the current browser session holds a valid admin session token. No additional server-side configuration or feature flag is needed.
- When no admin key is configured on the server (local dev mode with open admin routes), demo mode is not active; unauthenticated visitors in this mode see live admin pages as before.
- The Auth Settings demo page displays CCAPIKey as the pre-defined auth mode, as this is the primary CC authentication mechanism and most representative of real-world use.
- The Monitoring demo page displays approximately 20–30 mock request log entries spanning a realistic recent time window, with a representative mix of success and error responses.
- The Test Data demo page displays a realistic patient count (e.g., 47 patients) and populates all visible fields with plausible values; action buttons (Generate, Reset, Look Up) are present and clickable but have no visible effect.
- Demo mode must render correctly at both desktop and mobile viewport sizes; the demo experience is considered incomplete if any demo page is broken or unreadable on a standard mobile screen.
