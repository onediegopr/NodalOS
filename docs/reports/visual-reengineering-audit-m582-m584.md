# M582+M583+M584 - Visual Reengineering Audit

Decision target: `VISUAL_REENGINEERING_FOUNDATION_READY`.

## Scope

This report audits current visual surfaces and defines the foundation for a Research & Mission Operating System. It is audit/design/plan-first and does not reimplement productive UI.

## Current Inventory

Located surfaces:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`.
- `browser-extension/onebrain-chrome-lab/sidepanel.css`.
- `browser-extension/onebrain-chrome-lab/sidepanel.js`.
- `docs/ui/sidepanel-operator-cabinet-integration-map-m163-m165.md`.
- Static previews under `artifacts/agent-operations/m491` through `m581`.
- Governance docs under `docs/architecture`, `docs/reports`, and `docs/roadmap`.

No full application UI was found under `src`; current visual product evidence is mostly the extension sidepanel and static previews.

## Visual Problems

- The current sidepanel behaves like a technical control panel, not a mission operating system.
- Surfaces use many equal-weight cards, which weakens hierarchy.
- The current dark Mission Control preview feels closer to a technical dashboard than an editorial research product.
- The sidepanel uses compact operational tabs: Operate, Learn, Recipes, Runtime. These are useful, but they do not frame mission governance as the core object.
- Technical logs and runtime fields dominate too much visual attention.
- Current cards mix operational state, target resolution, verification, and logs without a strong narrative hierarchy.
- Consent and handoff surfaces exist, but they read as modal utility panels rather than a governance console.

## UX Problems

- The user cannot understand the active mission, phase, blockers, approvals, evidence, and risk in three seconds.
- Mission Control is present conceptually, but the current UI gives equal prominence to tools, logs, and technical state.
- Timeline exists, yet it feels like an operational stepper rather than a mission journal and evidence line.
- Evidence is represented as refs and logs, but not as a serious research archive.
- Advisor behavior is not established as strategic review; a future implementation could drift into chatbot patterns.
- Runtime state exposes many fields, but the safety meaning is not visually dominant.
- Blocked states say what is unavailable, but the anatomy should explain why, what is missing, what evidence is required, and what action is next.

## Product Perception Risks

- Risk of looking like a browser automation panel instead of a local-first AI mission infrastructure.
- Risk of looking like a generic SaaS dashboard if cards remain the primary pattern.
- Risk of looking like an admin console if tables/log panes dominate.
- Risk of users assuming blocked capabilities are implementation gaps rather than intentional governance.
- Risk of losing trust when consent, evidence, and runtime are not visually differentiated.

## Screen-Specific Audit

- Mission Control: should become the dominant operating surface, not a collection of widgets.
- Timeline: should become a mission journal with evidence and decisions attached.
- Mission Detail: needs a single current-mission hero and a clear phase narrative.
- Consent / Permissions: should become Governance Console with capability cards and risk.
- Evidence: should feel like case files, ADRs, matrices, reports, and test records.
- Decisions: should feel executive and auditable, with recommendation, risk, evidence, and options.
- Advisor: should be Advisor Notes, not chat bubbles.
- Models: should show policy and readiness, not a settings dump.
- Agents: should show role, authority boundary, status, and evidence generated.
- Runtime: should communicate local-first safety before details.
- Settings: should be grouped by policy and environment.
- Activity Feed: should replace raw logs as the primary human-readable mission feed.
- Readiness Gates: should explain requirements and blockers.
- Error / Blocked State: should include why, missing requirement, evidence, action, and disabled capability.
- Empty States: should guide mission creation or next safe step.

## Gaps Against New Direction

- Needs warm editorial background instead of control-center dark as default.
- Needs serif editorial headings and clean sans functional text.
- Needs fewer visible blocks with stronger hierarchy.
- Needs mission-centered navigation.
- Needs evidence-first and approval-first language.
- Needs blocked states that feel trustworthy and intentional.
- Needs a visual system that combines Subquadratic clarity, Anthropic seriousness, Linear precision, Arc calmness, and Stripe Press editorial confidence without copying any one source.

## Acceptance Criteria

- NODAL OS is not generic SaaS dashboard.
- The mission is centered.
- The system state is understandable in three seconds.
- Permissions and blocked capabilities are clear.
- Evidence feels serious and traceable.
- Advisor reads as strategic architecture guidance, not a chatbot.
- Runtime communicates local-first safety.
- Minimalism does not remove critical operational context.
- Migration is phased before productive redesign.

## Decision

Closed after validation: `VISUAL_REENGINEERING_FOUNDATION_READY`.
