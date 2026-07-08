# Current Authority Map

## Current Authority

| Artifact | Role |
| --- | --- |
| `docs/architecture/nodal-os-product-ledger-local-dev-safety-backlog-canon.md` | Current Product Ledger local/dev canon for blocked-state interpretation. |
| `docs/architecture/nodal-os-product-ledger-local-dev-next-action-plan.md` | Current next-action selection logic and safe-lane ordering. |
| `docs/architecture/nodal-os-product-ledger-local-dev-stale-entrypoint-crosslink-index.md` | Index of high-risk historical entrypoints cross-linked back to current authority. |
| `tests/OneBrain.Safety.Tests/ProductLedgerLocalDevCanonGuardTests.cs` | Test-only guard proving the canon still carries required blocked-state wording. |

## Review Evidence

| Artifact | Role |
| --- | --- |
| `docs/audit/product-ledger-local-dev/audit-review-result.md` | E7 read-only review result for the E6 packet. It is review evidence, not product authority. |

## Historical / Block-Specific Evidence

Older QA, handoff, roadmap and ADR files remain traceability. They should not be read alone as current product posture, especially when they mention active writer, runtime local-only enablement, public UI/actions, public surface readiness, latest pointer, read precedence or product authority.

## Current Posture

- Local/dev only.
- Evidence-only/internal.
- Safety/Recipes remain authoritative evidence.
- Tier 1 remains manual/discovery-only.
- CI enforcement remains `0%`.
- Runtime/product enablement remains `0%`.
- Release/commercial readiness remains `0% / NO-GO`.

## Next Recommended Block

`NODAL_OS_BLOCK_E8_EXTERNAL_AUDIT_PACKET_OPERATOR_REVIEW_HANDOFF_READ_ONLY`

E7 recommends but does not authorize E8.
