# M594+M595+M596 - Decisions / Advisor Research OS Visual Implementation

Decision target: `DECISION_ADVISOR_RESEARCH_OS_READY`.

## Scope

This block implements Decisions and Advisor visual Phase 4 as static Research OS surfaces. It creates fixture-backed visual artifacts, a standalone preview, and static Decision/Advisor QA. It does not connect productive decisions, approval mutation, Advisor runtime, operational access, productive consent, provider activity, cloud, or evidence verification.

## M594 - Decision Room Research OS

Created `artifacts/agent-operations/m596/decision-room-research-os.json`.

The Decision Room is framed as:

- Executive decision room.
- Governance record.
- Risk evaluation board.
- Evidence-backed decision system.
- Human authority surface.

Decision cards include:

- Decision.
- Recommendation.
- Risk.
- Status.
- Evidence.
- Linked Mission.
- Commit / Artifact Ref.
- User Options.

Required decision examples represented:

- Enable productive consent storage?
- Enable real filesystem access?
- Allow File Read capability?
- Build LLM context from workspace?
- Promote synthetic baseline to real scan?
- Accept Visual Reengineering Foundation?
- Keep Advisor warning active?

All user options are no-op and preview-only.

## M595 - Advisor Notes Visual System

Created `artifacts/agent-operations/m596/advisor-notes-visual-system.json`.

Advisor Notes are framed as:

- Technical advisor.
- Architecture critic.
- Risk auditor.
- Technical cofounder.
- Recommendation system.
- Contradiction and debt reader.

Advisor Notes include:

- Concern.
- Potential Impact.
- Recommendation.
- Evidence.
- Severity.
- Category.
- Status.

Required Advisor examples represented:

- Path jail is still design-only.
- Productive consent remains blocked.
- Evidence previews are not source-of-truth.
- Visual layer must not imply runtime authority.
- Cloud/provider activity remains disabled.
- Mission Control should not become a generic dashboard.
- Timeline must not become a simple checklist.
- Advisor must not become a chatbot.

Advisor actions are no-op and preview-only. The visual system uses editorial notes, not chat bubbles.

## M596 - Static Decision / Advisor QA

Created:

- `artifacts/agent-operations/m596/static-decision-advisor-qa-pack.json`.
- `artifacts/agent-operations/m596/decision-advisor-research-os-summary.json`.
- `artifacts/agent-operations/m596/decision-advisor-research-os-preview.html`.

QA confirms:

- decisions are not generic approval modals.
- decisions show recommendation/risk/evidence/options.
- decisions explain blocked/high-risk states.
- decisions link to mission/evidence/commit/test refs.
- user options are no-op in this block.
- decisions do not authorize runtime/capability/filesystem/LLM/cloud.
- advisor is not chatbot.
- no chat bubbles.
- advisor actions are no-op.
- advisor does not call LLM/provider.
- advisor does not mutate state.
- no source-of-truth promotion.

## Validation

Command validation completed at block close:

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed with inherited warnings only.
- `dotnet build .\OneBrain.slnx --no-restore`: passed.
- Filtered tests: 8 passed, 0 skipped, 0 failed.
- Full suite: 4319 passed, 37 skipped, 0 failed.
- Guard checks over new docs, tests, previews, artifacts, and roadmap diffs passed.

## Guardrail Confirmation

This block does not implement decision authorization, approval mutation, Advisor runtime, LLM-backed Advisor, provider-backed Advisor, evidence verification, productive source-of-truth behavior, runtime behavior, operational access, productive consent, capability enablement, cloud, or broad frontend rewrite.

## Progress Estimate

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 94%
- Redaction/Safety foundation: 98%
- Productization foundation: 93%
- Mission Control UX: 94%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 74%
- Cloud optional: 10%

## Decision

`M594+M595+M596 CERRADO / DECISION_ADVISOR_RESEARCH_OS_READY`.
