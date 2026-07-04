# Product Ledger Path Canonicalization Reparse Authority Test Plan External Audit Read-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_CANONICALIZATION_REPARSE_AUTHORITY_TEST_PLAN_EXTERNAL_AUDIT_READY`

## Scope

External-audit-read-only review of the product ledger path canonicalization/reparse/authority test plan.

This audit does not implement code, product ledger path activation, writer behavior, DI registration, command handlers, UI product actions, DB/migration/provider/cloud/network integration, KMS/WORM/external trust, Browser/CDP/WCU/OCR/Recipes live behavior or release/commercial readiness.

## Audited Inputs

- `docs/adr/product-ledger-path-canonicalization-reparse-authority-test-plan-only.md`
- `docs/qa/nodal-os-product-ledger-path-canonicalization-reparse-authority-test-plan/report.md`
- `docs/qa/nodal-os-product-ledger-path-canonicalization-reparse-authority-test-plan/report.json`
- `docs/handoff/nodal-os-product-ledger-path-canonicalization-reparse-authority-test-plan-handoff.md`
- `docs/roadmap/nodal-os-roadmap-vnext.md`
- `docs/decision-log.md`

## Coverage Assessment

| Area | Audit result | Notes |
| --- | --- | --- |
| Canonicalization test plan | sufficient for disabled scaffold planning | Covers null/empty/relative/absolute/traversal/mixed separators/env vars/UNC/drive-relative/long prefix/casing/Unicode/trailing dots-spaces/local-temp. |
| Windows paths | sufficient for next disabled scaffold | Includes reserved devices, UNC/network paths, drive-relative paths, long path prefix, trailing dots/spaces and ADS. |
| Symlink/junction/reparse | sufficient for next disabled scaffold | Requires target/final canonical evidence and rejects unresolved risk. |
| TOCTOU | sufficient for next disabled scaffold | Includes validation/write gap, swaps, races and final path evidence. |
| Hardlinks/mount points | sufficient for next disabled scaffold | Present as future test cases; platform-specific refinement remains P4. |
| Test-only vs product path | sufficient | Local-temp is explicitly non-product unless scoped test-only/non-product. |
| Product authority | sufficient for next disabled scaffold | Covers stale, replayed, tampered, wrong-scope, wrong-path and over-scoped approvals. |
| Human GO vs product authority | sufficient | Test-only human GO is explicitly rejected as product authority. |
| Replay/failure/read-model | sufficient for dependency planning | Requires replay/failure evidence and read-model binding as dependency, not implementation. |
| Rollback/non-rollback | sufficient for dependency planning | Identifies rollback policy as required before product candidate. |
| Overclaim prevention | sufficient | The wording consistently frames real canonicalization/reparse/authority as future work. |

## Wording Audit

No TRUE_RISK wording found.

The plan does not claim:

- canonicalization is implemented or solved;
- reparse/symlink/junction risk is solved;
- product ledger path is ready or active;
- product authority is real;
- test-only human GO is real product approval;
- runtime/product is ready;
- WORM/KMS/cloud/external trust exists;
- release/commercial readiness exists.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No critical issue found. |
| P1 | 0 | No scope leak or product enablement found. |
| P2 | 0 | No blocking test-plan insufficiency found for a disabled/test-only scaffold. |
| P3 | 4 | Disabled implementation scaffold still needs code-level fail-closed contracts, fixture-safe path abstractions, authority fixtures and static no-enable guards. |
| P4 | 3 | Unicode normalization, ADS and hardlink behavior need platform-specific fixture details during scaffold work. |

## Verdict

OPTION 3 — GO to product ledger path implementation scaffold disabled/test-only.

This verdict is limited to a future disabled/test-only/no-product-write scaffold. It does not approve product ledger activation, DI registration, command handlers, UI product actions, runtime enablement or release/commercial readiness.

OPTION 4 — limited product enablement remains NO-GO.

## Readiness Matrix

| Area | Previous status | Audited status |
| --- | ---: | ---: |
| Product ledger path policy | 12-18% / NO-GO | 14-20% / NO-GO |
| Canonicalization/reparse test plan | 45-55% / test-plan only | 55-65% / sufficient for disabled scaffold |
| Product authority test plan | 40-50% / test-plan only | 50-60% / sufficient for disabled scaffold |
| Disabled implementation scaffold | 0-10% / NO-GO until manual scope | 10-15% / GO only if disabled/test-only/no-write |
| Runtime/live product enablement | 0% / NO-GO | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO | 0% / NO-GO |

## Next Macro-Block

Recommended next macro-block:

`NODAL_OS_PRODUCT_LEDGER_PATH_CANONICALIZATION_REPARSE_AUTHORITY_IMPLEMENTATION_SCAFFOLD_DISABLED_TEST_ONLY`

Allowed scope for that future block must be disabled/test-only/no-product-write only.
