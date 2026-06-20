# NODAL OS Research Mission Design System

## Direction

Build NODAL OS as a Research & Mission Operating System local-first, with the clarity of Subquadratic, the seriousness of Anthropic, the precision of Linear, the calmness of Arc, and the editorial confidence of Stripe Press.

This system must not look like a generic SaaS dashboard, ERP, Jira clone, admin console, classic IDE, or technical console.

## Design Principles

- Research OS: every screen should feel like a governed research environment.
- Local Mission Control: the active mission is the center of gravity.
- Editorial premium: warm surfaces, serif headings, quiet detail.
- Governance-first: capability, consent, readiness, and approval are first-class.
- Evidence-first: evidence is treated as case material, not attachments.
- Calm precision: reduced noise with enough information to orient quickly.
- Trustworthy blocked states: blocked is a designed state, not a dead end.
- Dense enough but never cluttered: one dominant focus and up to three secondary blocks.

## Color Tokens

Warm editorial backgrounds:

- `--color-bg-main: #F5F3EE`
- `--color-bg-alt: #F2F0EA`
- `--color-bg-raised: #EFECE6`

Text:

- `--color-text-main: #171717`
- `--color-text-muted: #5C5C5C`

Borders:

- `--color-border: #DDD8D0`

Accent:

- `--color-accent: #5B6CFF`
- `--color-accent-deep: #7265FF`

States:

- `--color-success: #3D8F5A`
- `--color-warning: #E67E22`
- `--color-warning-deep: #D97706`
- `--color-danger: #C2410C`

Rules:

- Do not use pure white as a default surface.
- Use state colors sparingly.
- Accent marks interaction and policy focus, not decoration.

## Typography

- Headings: serif editorial premium. Preferred fallback: Georgia.
- UI and functional text: clean sans. Preferred fallback: Inter, Geist, SF Pro, system sans.
- Headings should be larger, quieter, and more editorial than current control-panel labels.
- Body text should remain highly readable in dense mission states.

## Spacing, Radius, Borders

- Spacing scale: 4, 8, 12, 16, 24, 32, 48, 64.
- Radius: 10 for controls, 16 for cards, 24 for hero panels.
- Borders: `1px solid #DDD8D0`.
- Dividers: thin, warm, low contrast.
- Shadows: rare and soft, only to separate elevated mission panels.

## Surfaces

- Editorial canvas: warm background.
- Mission hero: largest surface on Mission Control.
- Governance panel: muted raised surface with policy badges.
- Evidence case file: paper-like card with metadata rail.
- Runtime safety panel: calm surface with clear blocked states.

## Focus Rings

- Focus ring: `0 0 0 3px rgba(91, 108, 255, 0.24)`.
- Use focus styles on all buttons, navigation, cards that open detail, and review options.

## Badges And States

- Blocked: warm danger text with clear explanation.
- Draft: muted border and neutral text.
- Ready: success accent, never over-bright.
- Approved: success with evidence ref.
- Requires review: warning accent.

## Component System

### Shell Layout

Two-column desktop layout with minimal sidebar and mission-centered content. Right rail is optional and reserved for decisions, advisor notes, or readiness gates.

### Sidebar

Minimal labels: Mission Control, Timeline, Missions, Workspace, Consent, Evidence, Decisions, Advisor, Models, Agents, Runtime, Settings.

### Top Mission Header

Shows active mission, current phase, progress, blocked count, approval count, evidence count, and risk summary.

### Current Mission Hero

Dominant surface. Includes mission title, progress, current phase, one-sentence system interpretation, and next safe action.

### Timeline Step

Mission journal entry with phase, status, decision ref, evidence ref, readiness gate, and blocked reason if any.

### Mission Phase Card

Shows phase name, percentage, decision status, open blockers, evidence generated, and next gate.

### Decision Card

Executive decision format: decision, recommendation, risk, evidence, options, and current status.

### Evidence Card

Case-file format: artifact name, type, status, linked mission, commit/ref, validation result, and redaction state.

### Advisor Note

Strategic note format: concern, potential impact, recommendation, critical question, and evidence refs. Do not render as chatbot bubble.

### Readiness Gate

Shows capability, gate status, missing requirements, evidence required, owner/action, and whether it blocks use.

### Blocked State Panel

Anatomy: why blocked, what is missing, evidence required, user action, intentionally disabled capabilities, next allowed step.

### Activity Feed Row

Human-readable event row: time, actor, action, evidence generated, decision changed, validation run, commit/ref.

### Permission / Capability Card

Shows capability, status, scope, evidence, risk, user action, and policy boundaries.

### Runtime Status Card

Shows local-only state, network state, content access, indexing, representation build, OCR mode, provider activity, and safety reason.

### Model Policy Card

Shows primary model, fallback, configuration status, policy, cloud restriction, and approval requirement.

### Agent Card

Shows agent role, authority boundary, current assignment, allowed actions, blocked actions, and evidence produced.

### Settings Section

Grouped by policy, local environment, model policy, evidence storage, and visual/accessibility preferences.

## Component Anti-Patterns

- No generic dashboard cards everywhere.
- No pure white panels.
- No heavy gradients.
- No childish icons.
- No ERP tables as main UI.
- No chat bubbles for Advisor.
- No raw technical console as main surface.
- No control-center dark default.

## Mandatory Visual Direction

- Warm editorial background.
- Serif headings.
- Clean sans UI.
- Spacious but informative layout.
- Mission-centered hierarchy.
- Subdued premium accent.
- Evidence, consent, and runtime must each have distinct visual language.
