# M597+M598+M599 - Consent / Runtime / Models Research OS Visual Implementation

Decision target: `CONSENT_RUNTIME_MODELS_RESEARCH_OS_READY`.

## Scope

This block implements Consent, Runtime, and Models visual systems as static Research OS surfaces. It creates fixture-backed visual artifacts, a standalone preview, and static governance QA. It does not connect productive consent, capability enablement, runtime behavior, provider activity, cloud, operational access, or evidence verification.

## M597 - Consent Governance Console Visual System

Created `artifacts/agent-operations/m599/consent-governance-console.json`.

Consent is framed as:

- Governance Console.
- Capability control room.
- Risk and scope review.
- Audit-first consent ledger.
- Human authority layer.

Capability cards include:

- Capability.
- Status.
- Scope.
- Evidence.
- Risk.
- User Action.
- Policy.

Required capability examples represented:

- Path Canonicalization.
- Directory Listing.
- File Read.
- File Hash.
- Secret Detection.
- Exclusion Enforcement.
- Indexing.
- Embeddings.
- LLM Context Build.
- Cloud Sync.
- Provider Call.
- Runtime Execution.

The console states that productive consent is not active, capability enablement needs future approval, and File Read consent does not imply indexing, Embeddings, or LLM context.

## M598 - Runtime Local-First Safety Visual System

Created `artifacts/agent-operations/m599/runtime-local-first-safety.json`.

Runtime status includes:

- Runtime Status: Local-only.
- Network: Disabled.
- File Access: Blocked.
- Indexing: Disabled.
- Embeddings: Disabled.
- OCR: Synthetic-only / Not productive.
- Provider Calls: Disabled.
- Execution: Blocked.
- Runtime Authority: None in this preview.

Runtime safety panels cover:

- Local boundary.
- Network policy.
- Filesystem policy.
- Provider policy.
- Execution policy.
- Evidence emission policy.
- Kill switch / disable strategy as future-only.

Every blocked runtime state includes why, missing requirements, evidence required, user action needed, and intentionally disabled surfaces.

## M599 - Models Policy Visual System + Static Governance QA

Created:

- `artifacts/agent-operations/m599/models-policy-visual-system.json`.
- `artifacts/agent-operations/m599/static-consent-runtime-models-qa-pack.json`.
- `artifacts/agent-operations/m599/consent-runtime-models-research-os-summary.json`.
- `artifacts/agent-operations/m599/consent-runtime-models-research-os-preview.html`.

Models are framed as policy, not a provider selector. Model cards include:

- Primary Model.
- Fallback.
- Status.
- Policy.
- Data Exposure.
- Budget.
- Provider Calls.

Required model examples represented:

- Primary model configured as planned but not active.
- Fallback model defined but not active.
- Provider Calls Disabled.
- BYOK not implemented.
- Managed AI not enabled.
- Local model future.
- No cloud activity without approval.
- No workspace context sent.

QA confirms:

- Consent looks like Governance Console, not settings.
- Runtime communicates local-first safety.
- Models communicate policy, not just selection.
- All blocked states include why/missing/evidence/action/disabled.
- No card implies real capability.
- No provider/cloud/runtime is active.
- No productive consent is active.
- No source-of-truth promotion.
- Static/no-op/fixture-only.

## Validation

Command validation completed at block close:

- `dotnet restore .\OneBrain.slnx`: passed.
- `dotnet build .\OneBrain.slnx`: passed with inherited warnings only.
- `dotnet build .\OneBrain.slnx --no-restore`: passed.
- Filtered tests: 7 passed, 0 skipped, 0 failed.
- Full suite: 4326 passed, 37 skipped, 0 failed.
- Guard checks over new docs, tests, previews, artifacts, and roadmap diffs passed.

## Guardrail Confirmation

This block does not implement consent authorization, productive consent, consent persistence, capability enablement, runtime behavior, provider activity, cloud activity, model execution, BYOK, evidence verification, operational access, indexing, Embeddings, LLM context construction, source-of-truth behavior, or broad frontend rewrite.

## Progress Estimate

- NODAL OS global: 99.98%
- Agent Operations / Automation Layer: 99.4%
- Core Runtime: 76%
- Evidence/Timeline foundation: 96%
- Approval foundation: 95%
- Redaction/Safety foundation: 98%
- Productization foundation: 94%
- Mission Control UX: 95%
- Workspace Local: 84%
- Project Understanding foundation: 93%
- LLM/Assignment: 75%
- Cloud optional: 10%

## Decision

`M597+M598+M599 CERRADO / CONSENT_RUNTIME_MODELS_RESEARCH_OS_READY`.
