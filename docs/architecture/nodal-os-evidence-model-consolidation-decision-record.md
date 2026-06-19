# NODAL OS Evidence Model Consolidation Decision Record

Decision: `NODAL_OS_EVIDENCE_MODEL_CONSOLIDATION_DECIDED`

## Context

AUDIT-A found two evidence reference families in the repository:

- `NodalOsEvidenceBridgeRef`, used by current NODAL OS Agent Operations, approval, timeline, handoff and observability foundations.
- `NexaEvidenceRef`, used by older compatibility-oriented workboard/run-report contracts.

The new M465-M476 foundation is safe because exportable surfaces use the NODAL OS evidence bridge model, but future UI binding must not accidentally bind legacy evidence refs.

## Decision

`NodalOsEvidenceBridgeRef` is the canonical NODAL OS evidence reference model for future UI, exports, handoff packs, observability reports, timeline entries, approval previews and new Agent Operations surfaces.

`NexaEvidenceRef` is legacy/compatibility only. It is not an operational NODAL OS evidence model and must not be used by future UI binding.

## Binding Rule

Future UI must bind only to NODAL OS evidence models, primarily `NodalOsEvidenceBridgeRef` or a future explicitly named `NodalOs*` successor.

Future UI must not bind `NexaEvidenceRef` directly.

## Export Rule

Exports, handoff data packs and observability reports must serialize only NODAL OS evidence references. They must not serialize `NexaEvidenceRef` as an operational model.

## Payload Rule

Evidence remains ref-only by default:

- screenshot evidence is reference-only;
- DOM evidence is redacted-only;
- network evidence is metadata-redacted-only;
- raw payloads, cookies, headers, bodies, secrets and tokens are forbidden.

## Migration Plan

Future migration should be scoped:

- keep compatibility tests passing;
- avoid broad rename across unrelated legacy surfaces;
- introduce explicit aliases or adapters only where needed;
- move UI/export binding to NODAL OS evidence contracts first;
- quarantine or archive unused legacy evidence types only after dependency proof.

## Non-Goals

- No broad rename in M477-M479.
- No deletion of legacy contracts in M477-M479.
- No runtime implementation.
- No UI implementation.

## Acceptance Criteria

- ADR exists before UI real work.
- Tests assert this ADR declares NODAL OS as canonical.
- Tests assert `NexaEvidenceRef` is legacy/compatibility only.
- Tests assert future UI must not bind `NexaEvidenceRef`.
- Tests assert exports/handoff/observability use NODAL OS evidence refs.
