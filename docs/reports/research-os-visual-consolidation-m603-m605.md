# M603+M604+M605 - Research OS Visual Consolidation / Acceptance / Migration Readiness

Decision target: `RESEARCH_OS_VISUAL_CONSOLIDATION_READY`.

## Scope

This block consolidates the Research OS visual baseline across M582-M602, creates a cross-surface acceptance pack, and prepares migration readiness for future product UI planning. It does not implement product UI migration, product state, runtime, filesystem access, evidence verification, LLM provider activity, cloud, productive consent, capability enablement, or source-of-truth promotion.

## M603 - Research OS Visual Consolidation

Created:

- `artifacts/agent-operations/m605/research-os-visual-consolidation.json`.
- `artifacts/agent-operations/m605/research-os-consolidated-preview.html`.

Consolidated surfaces:

- Mission Control.
- Timeline.
- Evidence.
- Decisions.
- Advisor.
- Consent.
- Runtime.
- Models.
- Agents.
- Settings.
- Activity Feed.
- Blocked States.
- Readiness Gates.
- Empty States.

Cross-surface hierarchy:

- Mission is the primary focus.
- Timeline is the mission journal.
- Evidence is the research archive.
- Decisions are the executive room.
- Advisor is the technical/architecture note system.
- Consent is the governance console.
- Runtime is local-first safety.
- Models are policy surfaces.
- Agents are supervised roles, not bots.
- Settings are governance configuration.
- Activity Feed is the human-readable mission feed.

Consolidated visual rules:

- Warm editorial background.
- Serif headings.
- Clean sans UI.
- Mission-first layout.
- Max one dominant focus plus three secondary blocks.
- Blocked-state anatomy everywhere.
- No generic dashboard cards.
- No raw console main UI.
- No chatbot advisor.
- No settings-as-forms dominance.
- No evidence-as-attachments.
- No timeline-as-checklist.

## M604 - Cross-Surface Acceptance Pack

Created:

- `artifacts/agent-operations/m605/cross-surface-acceptance-pack.json`.
- `artifacts/agent-operations/m605/research-os-visual-acceptance-summary.json`.

Acceptance dimensions include:

- Mission-centered clarity.
- Governance clarity.
- Evidence traceability.
- Decision authority.
- Advisor non-chatbot pattern.
- Consent non-settings pattern.
- Runtime local-first safety.
- Models policy-first pattern.
- Agents non-autonomous pattern.
- Settings governance config.
- Activity feed human-readable.
- Blocked-state explanation.
- No source-of-truth promotion.
- No runtime coupling.
- No filesystem coupling.
- No LLM/provider/cloud coupling.
- No productive consent.

Acceptance decision:

- `ResearchOsVisualBaselineReady=true`.
- `ReadyForProductUiMigrationPlanning=true`.
- `ReadyForProductiveRuntime=false`.
- `ReadyForFilesystemAccess=false`.
- `ReadyForProductiveConsent=false`.
- `ReadyForLlmProviderCalls=false`.
- `ReadyForCloud=false`.
- `ReadyForSourceOfTruthPromotion=false`.

## M605 - Product UI Migration Readiness

Created:

- `artifacts/agent-operations/m605/product-ui-migration-readiness.json`.
- `artifacts/agent-operations/m605/product-ui-migration-readiness-preview.html`.

Readiness declares:

- `IsReadinessOnly=true`.
- `CanModifyProductUi=false`.
- `CanConnectRuntime=false`.
- `CanConnectFilesystem=false`.
- `CanConnectLlmProvider=false`.
- `CanPersistProductiveState=false`.
- `CanEnableCapabilities=false`.

Migration phases:

- Phase A: Token integration into product UI. No runtime.
- Phase B: Shell/Sidebar product UI migration. No runtime.
- Phase C: Mission Control product UI migration. Fixture-only.
- Phase D: Timeline/Evidence product UI migration. Fixture-only, no source-of-truth promotion.
- Phase E: Decisions/Advisor product UI migration. No approval mutation, no LLM advisor.
- Phase F: Consent/Runtime/Models product UI migration. No capability/provider/runtime activation.
- Phase G: Agents/Settings/Activity Feed product UI migration. No agents/settings productively active.
- Phase H: Visual integration QA + accessibility + regression pack.

Recommended next milestone:

- Product UI Entry Point Audit.

Direct broad UI rewrite is not recommended.

## Validation

Command validation completed at block close:

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed with inherited warnings only.
- `dotnet build .\OneBrain.slnx --no-restore`: passed.
- Filtered tests: 6 passed, 0 skipped, 0 failed.
- Full suite: 4339 passed, 37 skipped, 0 failed.
- Guard checks over new docs, tests, previews, artifacts, and roadmap diffs passed.

## Guardrail Confirmation

This block does not implement product UI migration, broad UI rewrite, real agents, autonomous execution, productive settings, settings persistence, productive consent, capability enablement, runtime behavior, Provider Calls, LLM-backed surfaces, BYOK, evidence verification, operational access, LLM context construction, source-of-truth behavior, or broad frontend rewrite.

## Progress Estimate

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 96%
- Mission Control UX: 97%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## Decision

`M603+M604+M605 CERRADO / RESEARCH_OS_VISUAL_CONSOLIDATION_READY`.
