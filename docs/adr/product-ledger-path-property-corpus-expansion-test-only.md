# Product Ledger Path Property Corpus Expansion Test-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_PROPERTY_CORPUS_EXPANSION_TEST_ONLY_READY`

## Scope

Test-only expansion of the disabled product ledger path readiness scaffold corpus.

This block adds string-level readiness blockers and adversarial tests only. It does not implement real filesystem canonicalization, real symlink/junction/reparse enforcement, an active product ledger path, a writer, productive DI/service registration, command handlers, UI product actions, DB/migration/provider/cloud/network integration, KMS/WORM/external trust, Browser/CDP/WCU/OCR/Recipes live behavior, runtime product enablement or release/commercial readiness.

## Implemented

- Unicode readiness preview blockers for non-normalized and confusable-script path segments.
- ADS readiness blockers for `file.txt:stream`, suspicious colon usage and `::$DATA` style corpus while preserving drive-letter distinction.
- Reparse evidence blockers for stale and conflicting reparse evidence.
- Canonical boundary confusion blocker for paths that appear outside but string-normalize inside the boundary.
- TOCTOU stale evidence signal.
- Evidence reference blockers for malformed, duplicate, stale, wrong request, wrong risk version, inconsistent, live/product wording and raw payload/secret-marker refs.
- Expanded Safety tests for canonicalization, ADS, Unicode, reparse, authority, evidence refs and no-enable wording.
- Expanded Recipes tests proving adversarial corpus rejects while all product capability booleans remain false.

## Boundary

The checks remain string-level readiness previews. They are intentionally conservative and do not claim product-grade canonicalization or reparse enforcement.

Human GO remains test-only evidence. It is never product authority and cannot authorize product write, runtime enablement, external trust, WORM/KMS/cloud, live automation or release/commercial readiness.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product enablement or product ledger writer added. |
| P1 | 0 | No scope leak found. |
| P2 | 0 | Focused corpus tests pass and no TRUE_RISK was found in static scans. |
| P3 | 4 | Real canonicalization enforcement, real reparse enforcement, real product authority and product write integration remain future work. |
| P4 | 2 | String-level Unicode/confusable detection is conservative; platform-specific hardlink/mount fixtures remain preview-only. |

## Readiness Matrix

| Area | Updated readiness | Status |
| --- | ---: | --- |
| Product ledger path policy | 20-28% | NO-GO |
| Canonicalization/reparse scaffold | 35-45% | disabled/test-only only |
| Authority scaffold | 34-44% | disabled/test-only only |
| Property/corpus coverage | 45-55% | test-only only |
| Disabled implementation scaffold | 32-42% | GO only as no-write preview |
| Runtime/live product enablement | 0% | NO-GO |
| Release/commercial readiness | 0% | NO-GO |

## Next Macro-Block

`NODAL_OS_PRODUCT_LEDGER_PATH_PROPERTY_CORPUS_EXPANSION_EXTERNAL_AUDIT_READ_ONLY`
