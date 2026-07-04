# NODAL OS - Product Ledger Path Threat Model Design-Only

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_THREAT_MODEL_DESIGN_ONLY_READY`

Date: 2026-07-04

## Scope

Docs-only threat model for a future Durable Audit Trail product ledger path. No source, tests or runtime behavior changed.

## Repo Guard

| Field | Value |
| --- | --- |
| Repo | `C:/DESARROLLO/NodalOS/Codigo-m12-audit` |
| Branch | `chrome-lab-001-extension-local-ai-bridge` |
| Initial HEAD | `49c2772de425396d7cf11e63de4d28deea2f4824` |
| Worktree initial | clean |
| Origin sync initial | `0 0` |
| Stash | listed only, not touched |

## Audit Notes

| Item | Result |
| --- | --- |
| Current product ledger implementation | ABSENT / PROHIBITED |
| Current Stage 2 storage | TEST_ONLY / LOCAL_TEMP |
| Current `IsProductLedgerPath` | Test-only fragment guard, not product policy |
| Product root policy | Missing, blocker |
| Canonicalization/containment policy | Missing, blocker |
| Redaction product wiring | Missing, blocker |
| Product runtime flag policy | Missing, blocker |

## Threat Summary

The future product ledger path must defend against temp fallback, repo/workspace writes, user-profile leakage, network/cloud-sync ambiguity, symlink/junction escape, path normalization bypass, raw path disclosure, concurrent writers, crash recovery gaps, tail deletion and unauthorized runtime/handler writes.

## Required Future Negative Tests

- Empty/whitespace path rejection.
- Temp root rejection for product mode.
- Repo/workspace path rejection.
- User documents/downloads/desktop rejection.
- Symlink/junction/reparse escape rejection.
- Network share and UNC path rejection.
- Cloud-sync-looking root rejection.
- Relative traversal rejection.
- Mixed casing/separator rejection.
- Missing redaction proof rejection.
- Live Browser/CDP/WCU/OCR/Recipes metadata rejection.
- Release/commercial flag rejection.
- Missing manual GO rejection.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No product ledger implementation or runtime/product authority added. |
| P1 | 0 | No service registration, handler, UI action, DB/provider/cloud/network or KMS/WORM path added. |
| P2 | 0 | No immediate blocker in this design-only block. |
| P3 | 4 | Product ledger root policy, canonicalization/containment, crash/concurrency behavior and product redaction wiring remain blockers. |
| P4 | 1 | Current `IsProductLedgerPath` remains a useful test-only guard but not a product policy. |

## Validations

| Validation | Result |
| --- | --- |
| Repo guard | PASS |
| `git diff --check` | PASS |
| JSON validation | PASS |
| Static scan changed files | PASS; no TRUE_RISK |
| Build/tests | Not run; docs-only block |

## Percentages

| Track | Status |
| --- | --- |
| Product ledger path threat model | 65-75% |
| Product ledger implementation | 0% / NO-GO |
| Product path canonicalization/containment | 0% / NO-GO |
| Product redaction wiring | 0% / NO-GO |
| Product runtime enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |

## Next Macro-Block

`NODAL_OS_RUNTIME_FEATURE_FLAG_PRODUCT_READINESS_DESIGN_ONLY`
