# M588+M589+M590 - Mission Control Research OS Layout

Decision target: `MISSION_CONTROL_RESEARCH_OS_LAYOUT_READY`.

## Scope

This block implements Mission Control visual Phase 2 as a static, fixture-backed Research OS layout. It advances from the Phase 1 shell preview into a concrete Mission Control composition without connecting runtime, operational access, provider activity, cloud, productive consent, or capability enablement.

## M588 - Mission Control Research OS Layout

Created `artifacts/agent-operations/m590/mission-control-research-os-layout.json` and `artifacts/agent-operations/m590/mission-control-research-os-preview.html`.

The layout includes:

- Warm editorial background using the Phase 1 palette.
- Minimal sidebar with Mission Control, Timeline, Missions, Workspace, Consent, Evidence, Decisions, Advisor, Models, Agents, Runtime, and Settings.
- Mission-first central canvas.
- Top current mission context.
- Three primary secondary panels.
- Activity feed with human-readable mission events.
- Explicit blocked runtime and capability status.

The layout intentionally avoids generic dashboard grids, dense tables, chat bubble advisor patterns, raw console center surfaces, pure white backgrounds, heavy gradients, and dark default control-center styling.

## M589 - Current Mission Hero

Created `artifacts/agent-operations/m590/current-mission-hero.json`.

The hero presents:

- Current Mission: Build Local AI Workspace.
- Progress: 72% Complete.
- Current Phase: Consent Governance.
- Mission Status: Governance baseline ready / real access blocked.
- Pending Decisions: 3.
- Advisor Warnings: 2.
- Evidence Artifacts: 12.
- Current Commit: e912da91.

Hero actions are no-op preview actions only:

- Review current phase.
- View evidence.
- Review blocked capabilities.
- Open advisor notes.
- Export preview summary.

The blocked explanation includes why access is blocked, what is missing, what evidence is required, what user action is needed, and what remains intentionally disabled.

## M590 - Mission Status Panels

Created `artifacts/agent-operations/m590/mission-status-panels.json`.

The three visible primary panels are:

- Consent / Capability Status as a Governance Console.
- Evidence Summary as a Research Archive.
- Advisor Note as an advisory card, not a chat interface.

The preview also includes a compact Activity Feed because it supports mission comprehension without becoming a fourth primary panel.

## Validation

Validation completed at block close:

- Restore: passed.
- Build: passed with inherited warnings only.
- Build without restore: passed.
- Filtered tests: 8 passed, 0 skipped, 0 failed.
- Full suite: 4303 passed, 37 skipped, 0 failed.
- Guard checks over new docs, tests, previews, artifacts, and roadmap diffs passed.

## Guardrail Confirmation

This block does not implement runtime behavior, operational access, productive consent, cloud, provider activity, LLM context, capability enablement, path jail, broad frontend rewrite, or productive persistence.

## Progress Estimate

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 94%
- Approval foundation: 91%
- Redaction/Safety foundation: 98%
- Productization foundation: 91%
- Mission Control UX: 92%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 74%
- Cloud optional: 10%

## Decision

`M588+M589+M590 CERRADO / MISSION_CONTROL_RESEARCH_OS_LAYOUT_READY`.
