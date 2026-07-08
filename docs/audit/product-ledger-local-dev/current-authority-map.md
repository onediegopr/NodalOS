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
| `docs/audit/product-ledger-local-dev/operator-review-handoff.md` | E8 operator handoff and E9 operator approval record. It is handoff evidence, not product authority. |
| `docs/audit/product-ledger-local-dev/external-review-handoff.md` | E9 external/manual review handoff. It prepares operator submission only; Codex did not submit externally. |
| `docs/audit/product-ledger-local-dev/operator-submission-packet.md` | E10 operator submission packet. It prepares manual operator submission only; no external review result exists yet. |
| `docs/audit/product-ledger-local-dev/external-review-response-intake.md` | E11 response intake scaffold plus E12 wait-closure record. It records no external response, no external approval and no reviewer findings. |
| `docs/audit/product-ledger-local-dev/internal-continuation-gate-reconciliation.md` | E13 internal continuation gate reconciliation. It recommends a safe internal manual-gate table and creates no product authority. |
| `docs/audit/product-ledger-local-dev/manual-gate-decision-table.md` | E14 manual gate decision table. It clarifies operator/manual gates and creates no product/runtime authority. |
| `docs/audit/product-ledger-local-dev/no-authority-static-scan-contract.md` | E15 no-authority static scan contract. It hardens blocked/future-not-authorized claim interpretation and creates no product authority. |

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

`STOP_AFTER_NO_AUTHORITY_STATIC_SCAN_HARDENED_NO_PRODUCT_AUTHORITY`

E15 hardens no-authority static scans and the focal Safety guard. E15 does not record external approval, does not claim external audit pass and does not authorize runtime/product work.
