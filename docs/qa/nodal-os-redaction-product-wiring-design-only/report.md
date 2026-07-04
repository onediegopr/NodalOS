# NODAL OS - Redaction Product Wiring Design-Only

Decision: `GO_WITH_FINDINGS_REDACTION_PRODUCT_WIRING_DESIGN_ONLY_READY`

Date: 2026-07-04

## Scope

Docs-only design for future redaction-before-persistence product wiring. No source, tests or runtime behavior changed.

## Repo Guard

| Field | Value |
| --- | --- |
| Repo | `C:/DESARROLLO/NodalOS/Codigo-m12-audit` |
| Branch | `chrome-lab-001-extension-local-ai-bridge` |
| Initial HEAD | `0195c13feda41bdd4466591290fda9afefefdb47` |
| Worktree initial | clean |
| Origin sync initial | `0 0` |
| Stash | listed only, not touched |

## Product Wiring Requirements

- Product policy id/version separate from test-only.
- Corpus ownership and update cadence.
- Product evidence schema.
- No raw values in errors, telemetry or evidence.
- Product ledger dependency.
- Product feature flag dependency.
- Command/UI authority dependency.
- Fail-closed behavior with no fallback append.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No product redaction wiring or runtime/product enablement added. |
| P1 | 0 | No DI registration, command handler, UI action, product ledger path or provider/network call added. |
| P2 | 0 | No blocker in this design-only block. |
| P3 | 4 | Product policy versioning, corpus governance, product evidence schema and no-raw logging policy remain future work. |
| P4 | 1 | Current test-only service remains finite deterministic detection, not full product redaction. |

## Validations

| Validation | Result |
| --- | --- |
| Repo guard | PASS |
| `git diff --check` | PASS |
| JSON validation | PASS |
| Static scan changed files | PASS - no TRUE_RISK; hits are explicit prohibited/non-goal wording |
| Build/tests | Not run; docs-only block |

## Percentages

| Track | Status |
| --- | --- |
| Redaction-before-persistence test-only | 91-95% |
| Redaction product wiring design | 55-65% |
| Redaction product implementation | 0% / NO-GO |
| Runtime/product enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |

## Next Macro-Block

`NODAL_OS_DURABLE_RUNTIME_ENABLEMENT_DESIGN_ONLY_NO_CODE`
