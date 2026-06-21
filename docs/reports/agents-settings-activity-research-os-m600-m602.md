# M600+M601+M602 - Agents / Settings / Activity Feed Research OS Visual Implementation

Decision target: `AGENTS_SETTINGS_ACTIVITY_RESEARCH_OS_READY`.

## Scope

This block implements Agents, Settings, and Activity Feed visual systems as static Research OS surfaces. It creates fixture-backed visual artifacts, a standalone preview, and static integration QA. It does not connect real agents, productive settings, runtime behavior, provider activity, cloud, operational access, productive consent, capability enablement, or evidence verification.

## M600 - Agents Research OS Visual System

Created `artifacts/agent-operations/m602/agents-research-os-visual-system.json`.

Agents are framed as:

- Capability team.
- Supervised operators.
- Mission roles.
- Non-autonomous profiles.
- Policy-bound advisory surfaces.
- Nothing executes without gates.

Agent cards include:

- Agent.
- Role.
- Status.
- Authority.
- Inputs.
- Outputs.
- Policy.
- Evidence.
- Risk.

Required agent examples represented:

- Research Planner.
- Evidence Curator.
- Consent Auditor.
- Runtime Sentinel.
- Model Policy Reviewer.
- Advisor Analyst.
- Mission Reporter.

The visual system states that agents are not autonomous executors, cannot run tasks, cannot use Provider Call, cannot access filesystem, cannot approve capability, cannot mutate state, and have no runtime authority.

## M601 - Settings Governance Visual System

Created `artifacts/agent-operations/m602/settings-governance-visual-system.json`.

Settings are framed as:

- Governance configuration.
- Policy surface.
- Local-first control room.
- Defaults review without accidental enablement.

Settings sections include:

- Local-first defaults.
- Workspace policy.
- Consent policy.
- Model policy.
- Runtime policy.
- Evidence policy.
- Redaction policy.
- Cloud policy.
- Update/download policy.
- Accessibility / appearance.

Required examples represented:

- Local-only mode enabled.
- Network disabled.
- Provider Calls disabled.
- Productive consent disabled.
- Filesystem access blocked.
- Evidence/timeline required.
- Redaction required.
- Cloud sync disabled.
- Runtime execution blocked.
- Appearance: Research OS warm editorial.

The surface discloses that settings are preview-only, productive settings are not persisted, no provider key is stored, no capability is enabled, and no runtime behavior changes.

## M602 - Mission Activity Feed Visual System + Static Integration QA

Created:

- `artifacts/agent-operations/m602/mission-activity-feed-visual-system.json`.
- `artifacts/agent-operations/m602/static-agents-settings-activity-qa-pack.json`.
- `artifacts/agent-operations/m602/agents-settings-activity-research-os-summary.json`.
- `artifacts/agent-operations/m602/agents-settings-activity-research-os-preview.html`.

Mission Activity Feed is framed as human-readable mission activity, not raw logs. Feed rows show:

- Time.
- Actor.
- Event.
- Summary.
- Linked Evidence.
- Linked Commit.
- Status.

Required feed examples represented:

- 14:32 — System — Full suite passed — 4326 passed, 37 skipped.
- 14:35 — Advisor — Warning generated — Productive consent still blocked.
- 14:38 — Evidence — Artifact linked — Consent Runtime Models Research OS.
- 14:41 — Runtime — Capability blocked — Provider Calls Disabled.
- 14:44 — Decision — Governance baseline accepted — visual static/no-op only.
- 14:49 — Tests — Guardrails confirmed — no runtime/filesystem/LLM/cloud.

QA confirms:

- Agents are not autonomous bots.
- Settings are governance config, not generic forms.
- Activity Feed is mission-readable, not raw logs.
- No card implies runtime/capability/model/provider active.
- No productive settings or consent are persisted.
- No source-of-truth promotion.
- Static/no-op/fixture-only.
- No runtime/filesystem/evidence verification/LLM/cloud/productive consent.

## Validation

Command validation completed at block close:

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed with inherited warnings only.
- `dotnet build .\OneBrain.slnx --no-restore`: passed.
- Filtered tests: 7 passed, 0 skipped, 0 failed.
- Full suite: 4333 passed, 37 skipped, 0 failed.
- Guard checks over new docs, tests, previews, artifacts, and roadmap diffs passed.

## Guardrail Confirmation

This block does not implement real agents, autonomous execution, productive settings, settings persistence, productive consent, capability enablement, runtime behavior, Provider Calls, LLM-backed agents, BYOK, evidence verification, operational access, LLM context construction, source-of-truth behavior, or broad frontend rewrite.

## Progress Estimate

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 95%
- Mission Control UX: 96%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## Decision

`M600+M601+M602 CERRADO / AGENTS_SETTINGS_ACTIVITY_RESEARCH_OS_READY`.
