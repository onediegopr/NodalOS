# NODAL OS Visual Migration Plan

## Migration Rule

Do not reimplement the UI massively in one step. Move through foundation, Mission Control, internal screens, Advisor/feed, and polish.

## Phase 1 - Visual Foundation

Scope:

- Tokens.
- Layout base.
- Typography.
- Warm background.
- Sidebar.
- Buttons.
- Cards.
- Badges.

Future files likely touched:

- `browser-extension/onebrain-chrome-lab/sidepanel.css`.
- Future app shell files if a product UI is added under `src` or `apps`.
- Design docs under `docs/design`.

Validation:

- Contrast review.
- Keyboard focus review.
- Static preview review.
- Guard checks.

## Phase 2 - Mission Control

Scope:

- Home.
- Current mission.
- Progress.
- Timeline summary.
- Warnings.
- Pending decisions.
- Evidence summary.

Acceptance:

- Mission is dominant.
- System state is understood in three seconds.
- Maximum three secondary blocks above the fold.

## Phase 3 - Core Internal Screens

Scope:

- Timeline.
- Evidence.
- Decisions.
- Consent.
- Runtime.

Acceptance:

- Timeline reads as mission journal.
- Evidence reads as case archive.
- Decisions read as executive decision room.
- Consent reads as Governance Console.
- Runtime communicates local-first safety.

## Phase 4 - Advisor And Activity Feed

Scope:

- Advisor notes.
- Risk cards.
- Recommendation cards.
- Mission feed.
- Human-readable operational records.

Acceptance:

- Advisor is not a chatbot.
- Feed explains what happened, who did it, evidence generated, decision changed, validation run, and commit/ref.

## Phase 5 - Polish And Consistency

Scope:

- Empty states.
- Blocked states.
- Responsive behavior.
- Accessibility.
- Contrast.
- Keyboard navigation.
- Visual QA.

Acceptance:

- Blocked states explain why, missing requirements, evidence needed, action needed, and intentionally disabled capability.
- Minimalism does not hide important governance information.

## Acceptance Criteria

- NODAL OS is not generic SaaS dashboard.
- NODAL OS is mission centered.
- The active mission, phase, blockers, approvals, evidence, and risk are clear in three seconds.
- Permissions and blocked capabilities are clear.
- Evidence feels serious and traceable.
- Advisor is strategic, not chat-first.
- Runtime communicates local-first safety.
- The visual direction matches Subquadratic, Anthropic, Linear, Research OS, and Local Mission Control influences without copying them.
- Important information is not lost through minimalism.
- Migration is phased before implementation.
- Future files to touch are documented.
- Future validations are documented.

## Next Safe Step

Implement Phase 1 as a small visual foundation change with tokens and static preview validation only.
