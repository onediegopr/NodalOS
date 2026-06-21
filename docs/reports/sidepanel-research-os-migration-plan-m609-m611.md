# Sidepanel Research OS Migration Plan - M609-M611

Decision target: `SIDEPANEL_RESEARCH_OS_MIGRATION_PLAN_READY`.

This pack plans the future Research OS migration of the real Chrome extension sidepanel. It does not modify the product sidepanel, does not migrate product UI, and does not connect any visual surface to runtime, filesystem, Provider Calls, cloud, productive consent, or capability enablement.

## M609 - Sidepanel Structure Audit

Inspected files:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js`
- `browser-extension/onebrain-chrome-lab/manifest.json`

Sidepanel HTML structure:

- Sticky app header with `NODAL OS`, connection status, and global stop button.
- Four top tabs: Operar, Aprender, Recetas, Runtime.
- Human intervention banner.
- Operate panel with instruction textarea, run controls, handoff surface, consent surface, status cards, target resolution, verification, and operational timeline.
- Learn panel with recipe learning controls, learning timeline, and recipe draft editor.
- Recipes panel with saved recipe list, import control, recipe editor, deterministic recipe runner controls, and recipe timeline.
- Runtime panel with connection controls, credential setup surface, advanced credential controls, local engine status, IA status, extension status, run status, and log details.

Sidepanel CSS structure:

- Existing palette uses muted green/gray surfaces with grid texture.
- Global primitives: `body`, `button`, `input`, `textarea`, `.surface`, `.grid`, `.tabs`.
- Operational primitives: `.human-banner`, `.handoff-surface`, `.consent-surface`, `.timeline-*`, `.candidate-*`, `.recipe-*`, `.log-*`.
- Research OS migration should first introduce scoped tokens, then gradually replace visual hierarchy without changing behavior.

Sidepanel JS responsibilities:

- Holds local UI state for tabs, connection status, run state, operator timeline, learning, recipes, runtime diagnostics, handoff, consent, and logs.
- Binds tab buttons and action buttons.
- Sends sidepanel messages through a single `post(...)` path.
- Manages runtime connection UI, credential setup, learning actions, recipe actions, handoff actions, consent actions, and diagnostic rendering.
- Contains high-risk controls because several buttons can initiate or resume behavior through message passing.

Manifest relevant references:

- Manifest V3 extension.
- Side panel default path points to `sidepanel.html`.
- Permissions include active tab, scripting, storage, tabs, side panel, and scheduled alarm support.
- Host permissions are broad and must be treated as high risk during visual migration.
- Content security policy allows local extension scripts and local transport endpoints.

Current risks:

- Operate and Runtime tabs expose action labels that can look executable.
- Runtime surface can appear active even when future Research OS visuals should communicate blocked/local-first status.
- Consent surface currently has approval/denial buttons that must not be visually promoted as productive consent in the Research OS migration.
- Logs and diagnostic details can dominate the UI and make the sidepanel feel like a raw console.
- Recipe actions and learning controls can look like autonomous execution unless future copy and blocked-state anatomy are explicit.

## Migration Target

Phase 1 - Tokens + shell + background + typography:

- Introduce Research OS warm editorial tokens.
- Add serif heading strategy and clean sans UI strategy.
- Keep existing IDs and behavior stable.
- Preserve all safety surfaces and stop/handoff controls.

Phase 2 - Sidebar + Mission Control hero:

- Replace top-tab dominance with a mission-first shell plan.
- Keep tab behavior unchanged until a future scoped implementation milestone.
- Add no-op Mission Control hero fixture for visual comparison.

Phase 3 - Timeline/Evidence previews:

- Reframe operational timeline as mission journal.
- Reframe verification/evidence blocks as research archive previews.
- Preserve diagnostic data boundaries.

Phase 4 - Decisions/Advisor previews:

- Introduce executive decision cards and advisory note patterns.
- Advisor must not become a chatbot.
- Action options must be no-op unless a future milestone authorizes behavior.

Phase 5 - Consent/Runtime/Models previews:

- Reframe consent as Governance Console.
- Reframe runtime as local-first safety surface.
- Reframe model information as policy surface.
- Keep Provider Calls and runtime execution visually blocked.

Phase 6 - Agents/Settings/Activity Feed previews:

- Reframe agents as supervised roles, not autonomous bots.
- Reframe settings as governance configuration, not generic forms.
- Reframe logs as mission-readable Activity Feed.

Phase 7 - Blocked states + empty states + accessibility polish:

- Enforce blocked-state anatomy everywhere.
- Add empty states that explain why features remain blocked.
- Validate contrast, focus states, responsive sidepanel constraints, and keyboard flow.

## Sidepanel Migration Rules

Allowed:

- CSS tokens.
- Layout shell.
- Semantic HTML structure.
- Static fixture sections.
- No-op buttons with explicit labels.
- Blocked-state anatomy.
- Local-only visual indicators.
- Accessibility attributes.
- Visual hierarchy.
- Copy and microcopy.
- Scoped class naming cleanup.

Forbidden:

- Runtime calls.
- Filesystem calls.
- Provider Calls.
- Network calls.
- Cloud calls.
- Capability enablement.
- Productive consent.
- Source-of-truth promotion.
- Approval mutation.
- Event routing changes.
- Async dispatch orchestration.
- Direct browser executor references.
- Broad JS behavior rewrite.
- Removing existing safety guards.
- Hiding blocked-state explanations.

## M610 - Rollback / Safety Strategy

Future files expected to be touched:

- `browser-extension/onebrain-chrome-lab/sidepanel.html`
- `browser-extension/onebrain-chrome-lab/sidepanel.css`
- `browser-extension/onebrain-chrome-lab/sidepanel.js` only if a separate behavior audit approves a scoped no-op visual hook.
- `browser-extension/onebrain-chrome-lab/manifest.json` should not be touched by visual migration unless a separate extension review authorizes it.

Rollback requirements:

- Start from clean working tree.
- Commit visual tokens separately from structural HTML changes.
- Commit behavior-adjacent changes separately, or avoid them.
- Keep pre-migration static preview baseline.
- Keep post-migration static preview baseline.
- Preserve stop, handoff, consent, and safety-related affordances.
- Revert the single migration commit if visual or safety QA fails.

Safety gates before future product UI modification:

- Working tree clean.
- Branch confirmed.
- Full suite green.
- Sidepanel files audited.
- Visual baseline captured.
- Rollback plan present.
- No-runtime-coupling tests present.
- Static/no-op markers present.
- Source-of-truth boundaries present.

Stop conditions:

- Sidepanel JS calls runtime unexpectedly.
- Provider/cloud/network calls introduced.
- Filesystem APIs introduced.
- Capability enablement implied.
- Productive consent persisted.
- Approval mutation introduced.
- Previews become source-of-truth.
- Blocked-state anatomy removed.
- Advisor becomes chatbot.
- Activity Feed becomes raw log console.
- Models appears to enable Provider Calls.
- Runtime appears active when blocked.

## M611 - Visual Regression Fixtures / QA Baseline

Fixture surfaces:

- Mission Control hero.
- Sidebar.
- Timeline journal.
- Evidence archive.
- Decision room.
- Advisor notes.
- Consent governance.
- Runtime safety.
- Models policy.
- Agents roles.
- Settings governance.
- Activity feed.
- Blocked states.
- Empty states.
- Readiness gates.

Expected assertions:

- Required copy is explicit and human-readable.
- Blocked-state anatomy includes why, missing requirement, evidence, user action, and intentionally disabled surfaces.
- No-op action labels are visible.
- Source-of-truth status remains preview-only.
- Runtime coupling status remains blocked.
- Provider/cloud coupling status remains blocked.
- Filesystem coupling status remains blocked.
- Consent/capability status remains blocked.
- Visual acceptance notes reference Research OS warm editorial direction.

Visual QA baseline:

- Research OS warm editorial style.
- Mission-first hierarchy.
- No generic SaaS dashboard.
- No dark technical control center as default.
- No raw console main UI.
- No chatbot advisor.
- No generic settings forms.
- No autonomous agents.
- No provider selector implying calls.
- No runtime active implication.
- No file access availability implication.

## Closeout

M609-M611 is plan-ready only. Sidepanel product UI remains unchanged. Product UI migration, runtime coupling, filesystem feature behavior, evidence verification, LLM/provider/cloud, productive consent, capability enablement, and source-of-truth promotion remain blocked.
