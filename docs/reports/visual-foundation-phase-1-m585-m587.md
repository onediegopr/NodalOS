# M585+M586+M587 - Visual Foundation Phase 1

Decision target: `VISUAL_FOUNDATION_PHASE_1_READY`.

## Scope

This block starts Phase 1 visual implementation by adding visual tokens, a Research OS shell preview, and static visual QA. It does not rewrite productive UI.

## M585 - Visual Foundation Tokens

Created `docs/design/nodal-os-visual-tokens-phase-1.md` with:

- Warm editorial background tokens.
- Text, border, accent, and state tokens.
- Serif heading strategy with Georgia fallback.
- Clean sans UI strategy with no remote font dependency.
- Layout tokens for shell, sidebar, rails, panels, cards, focus, timeline, and badges.
- Component primitives for shell, sidebar, mission hero, timeline step, evidence card, decision card, advisor note, readiness gate, permission card, activity feed row, blocked state panel, runtime status card, and model policy card.

## M586 - Research OS Shell Preview

Created `artifacts/agent-operations/m587/research-os-shell-preview.html`.

The preview includes:

- Warm editorial background.
- Minimal left sidebar.
- Current Mission hero.
- Three secondary mission panels.
- Timeline summary.
- Consent / Capability card.
- Evidence card.
- Decision card.
- Advisor note.
- Runtime local-first status.
- Mission Activity Feed.
- Blocked state anatomy with why, missing, evidence, action, and disabled capabilities.

## M587 - Static Visual QA Pack

Created:

- `artifacts/agent-operations/m587/static-visual-qa-pack.json`.
- `artifacts/agent-operations/m587/visual-foundation-phase-1-summary.json`.

QA confirms:

- Not generic SaaS dashboard.
- Mission-centered hierarchy.
- Warm editorial background.
- Serif headings and clean sans UI.
- One primary focus plus maximum three secondary panels.
- Consent reads as governance console.
- Evidence reads as research archive.
- Advisor is a note card, not chat-first.
- Runtime communicates local-first safety.
- No pure white background.
- No heavy gradient.
- No raw console as main UI.
- Phase 1 only; no broad UI rewrite.

## Validation

Command validation completed at block close:

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed with inherited warnings only.
- `dotnet build .\OneBrain.slnx --no-restore`: passed with inherited warnings only.
- Filtered tests: 8 passed, 0 skipped, 0 failed.
- Full suite: 4295 passed, 37 skipped, 0 failed.
- Guard checks over new docs, tests, previews, artifacts, and roadmap diffs passed.

## Guardrail Confirmation

This block does not implement productive redesign, runtime changes, operational access, productive consent, capability enablement, LLM context, provider activity, cloud, or storage behavior.

## Progress Estimate

- NODAL OS global: 99.97%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 94%
- Approval foundation: 91%
- Redaction/Safety foundation: 98%
- Productization foundation: 90%
- Mission Control UX: 90%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 74%
- Cloud optional: 10%

## Decision

`M585+M586+M587 CERRADO / VISUAL_FOUNDATION_PHASE_1_READY`.
