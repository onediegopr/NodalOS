# Product Ledger Path Canonicalization Reparse And Authority Test Plan Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_CANONICALIZATION_REPARSE_AND_AUTHORITY_TEST_PLAN_ONLY_READY`

## Scope

Test-plan-only/design-only safety plan before any future product ledger path implementation.

This document does not implement a product ledger path, canonicalization service, reparse-point enforcement, authority service, writer, DI registration, command handler, UI product action, DB/migration/provider/cloud/network integration, KMS/WORM/external trust, Browser/CDP/WCU/OCR/Recipes live behavior or release/commercial readiness.

## Canonicalization Test Plan

| Case | Expected future product behavior | Required evidence | Product blocker |
| --- | --- | --- | --- |
| null path | reject fail-closed | blocker result and no write attempt | yes |
| empty/whitespace path | reject fail-closed | blocker result and no write attempt | yes |
| relative path | reject unless a future policy explicitly resolves it under a fixed root | normalized input plus rejection reason | yes |
| absolute local path | accept only after canonical realpath and jail verification | input path, canonical path, jail root | yes |
| path traversal | reject before and after canonicalization | raw input, normalized path, canonical path | yes |
| mixed separators | reject or normalize under policy, then verify canonical boundary | separator policy proof | yes |
| Windows reserved devices | reject | reserved-name detection evidence | yes |
| environment variable expansion attempts | reject, do not expand user text | raw input preserved as unexpanded evidence | yes |
| UNC/network path | reject | scheme/UNC detection evidence | yes |
| drive-relative path | reject | drive-relative detection evidence | yes |
| long path prefix | reject until explicitly supported and canonicalized | prefix handling evidence | yes |
| casing normalization mismatch | verify case-insensitive boundary on Windows and preserve canonical output | original/canonical casing evidence | yes |
| Unicode normalization mismatch | reject or canonicalize under documented platform policy | normalization form evidence | yes |
| trailing dots/spaces on Windows | reject | Windows normalization evidence | yes |
| symlink | reject unless future policy proves target remains inside jail | link target and final canonical path evidence | yes |
| junction | reject unless future policy proves target remains inside jail | junction target and final canonical path evidence | yes |
| mount point | reject unless future policy explicitly permits and verifies target | mount target evidence | yes |
| reparse point | reject unresolved reparse-point risk | reparse tag and target evidence | yes |
| hardlink | reject or constrain by inode/file-id ownership policy | file-id evidence | yes |
| canonical path differs from displayed path | use canonical path for all decisions and audit both | displayed and canonical path pair | yes |
| initially inside jail but resolves outside | reject | pre/post-resolution boundary evidence | yes |
| initially outside but string-normalized to appear inside | reject | string-normalization and canonical evidence | yes |
| local-temp path | allowed only in test-only/local-temp scopes | explicit test-only scope evidence | yes for product |
| product ledger path local-temp | reject unless explicitly classified non-product | non-product classification evidence | yes |

## Reparse Symlink Threat Model

| Threat | Severity | Likelihood | Required mitigation | Required test | Product blocker | Evidence required |
| --- | --- | --- | --- | --- | --- | --- |
| Symlink escape | high | medium | resolve final target and verify jail after resolution | inside symlink to outside target | yes | link target and final canonical path |
| Junction escape | high | medium | detect junction/reparse and verify target | junction under jail to outside root | yes | reparse metadata and target |
| Reparse-point confusion | high | medium | reject unknown reparse tags | unknown reparse tag fixture | yes | tag id and rejection |
| TOCTOU validation/write gap | high | medium | open/write through safe handle strategy or revalidate immediately | swap target after validation | yes | validation/write timing trace |
| Attacker swaps path after validation | high | medium | atomic open and final path verification | directory swap fixture | yes | final path evidence |
| Canonicalization race | high | low-medium | single source of truth for canonical path | concurrent rename fixture | yes | canonical path at decision time |
| Path casing confusion | medium | medium | platform-aware case rules | case-variant jail tests | yes | comparison mode evidence |
| Alternate data streams | medium | low-medium | reject ADS syntax where applicable | `file:stream` path case | yes | ADS rejection evidence |
| Hardlink aliasing | medium | low | file-id ownership policy or reject hardlinks | hardlink alias fixture | yes | file-id evidence |
| Temp file rename risk | high | medium | atomic append policy; no temp rename into ledger unless proven safe | temp rename attack | yes | write strategy evidence |
| Filesystem permission denial | medium | medium | fail closed with redacted error | denied ACL fixture | yes | redacted failure evidence |
| Partial write | high | medium | append integrity marker and readback validation | interrupted append fixture | yes | integrity marker evidence |
| Tail deletion | high | medium | checkpoint/readback detection; no WORM overclaim | truncate tail fixture | yes | checkpoint limitation evidence |
| Ledger truncation | high | low-medium | head/tail checkpoint comparison | truncate file fixture | yes | before/after head evidence |
| Append interleaving | high | medium | concurrency strategy and ordering evidence | concurrent append fixture | yes | ordering evidence |
| Clock/timestamp trust | medium | medium | do not use wall-clock as sole ordering/trust signal | clock skew fixture | yes | monotonic/event id evidence |

## Authority Wiring Test Plan

| Case | Expected future product behavior | Required evidence | Product blocker |
| --- | --- | --- | --- |
| no human approval | reject | missing approval blocker | yes |
| test-only human GO flag mistaken as product authority | reject | overclaim blocker | yes |
| missing operator identity | reject | identity blocker | yes |
| missing local operator session | reject | session blocker | yes |
| stale approval | reject | approval timestamp/expiry evidence | yes |
| approval for different scope | reject | requested scope vs approval scope | yes |
| approval for different ledger path | reject | approved canonical path vs requested path | yes |
| approval for different runtime flag | reject | approved flag id/value vs requested flag | yes |
| approval replay | reject | nonce/id reuse evidence | yes |
| approval tampering | reject | integrity check evidence | yes |
| approval after risk changes | reject until re-approved | risk snapshot id mismatch | yes |
| approval missing evidence refs | reject | evidence reference blocker | yes |
| approval authorizes provider/cloud/KMS/WORM/external trust | reject in local-only product ledger scope | over-scope blocker | yes |
| approval authorizes Browser/CDP/WCU/OCR/Recipes live | reject in durable runtime ledger scope | live authority blocker | yes |
| approval authorizes release/commercial | reject in product ledger path scope | release/commercial blocker | yes |
| approval downgrade/upgrade ambiguity | reject | explicit version/scope comparison | yes |

## Product Ledger Path Acceptance Criteria

A future product ledger path can move from NO-GO to candidate only when all are true:

- canonical realpath is verified by product code;
- jail boundary is verified after canonicalization;
- symlink/junction/reparse unresolved risk is rejected or proven contained;
- redaction product wiring is enforced before persistence;
- retention policy is present;
- failure/replay evidence is present;
- rollback/non-rollback policy is present;
- append-only limitations are explicit;
- tail deletion limitation is explicit;
- no WORM/KMS/cloud claim is made from local-only evidence;
- no external trust claim is made without explicit provider/security decision;
- product authority is explicit, scoped and not confused with test-only human GO;
- static no-enable scan is clean before registration;
- adversarial path and authority tests pass;
- manual GO packet names allowed surfaces and stop conditions.

## Future Disabled Implementation Scaffold Map

Candidate future class/interface names:

- `IDurableProductLedgerPathPolicy`
- `DurableProductLedgerPathCandidate`
- `DurableProductLedgerPathDecision`
- `DurableProductLedgerPathBlocker`
- `DurableProductLedgerPathCanonicalizationEvidence`
- `IDurableProductAuthorityPolicy`
- `DurableProductAuthorityDecision`
- `DurableRuntimeProductEnablementDisabledScaffold`

Candidate future test names:

- `ProductLedgerPathPolicy_Rejects_Null_Empty_Whitespace_And_Relative`
- `ProductLedgerPathPolicy_Rejects_Traversal_EnvVars_Unc_And_DriveRelative`
- `ProductLedgerPathPolicy_Blocks_Symlink_Junction_MountPoint_And_ReparseEscape`
- `ProductLedgerPathPolicy_Blocks_CanonicalPathOutsideJail`
- `ProductLedgerPathPolicy_Blocks_LocalTempForProductScope`
- `ProductAuthorityPolicy_Rejects_TestOnlyHumanGoAsProductAuthority`
- `ProductAuthorityPolicy_Rejects_Stale_Replayed_Tampered_OrWrongScopeApproval`
- `ProductEnablementDisabledScaffold_HasNoDiHandlersUiDbCloudOrLiveAutomation`

Negative guard names:

- `NoProductLedgerPathActive`
- `NoProductRuntimeEnablement`
- `NoProductDiRegistration`
- `NoProductCommandHandlers`
- `NoUiProductActions`
- `NoDbMigrationProviderCloudNetwork`
- `NoKmsWormExternalTrust`
- `NoBrowserCdpWcuOcrRecipesLive`
- `NoReleaseCommercialClaim`

Docs to update in that future block:

- ADR for disabled implementation scaffold;
- QA MD/JSON report;
- handoff;
- decision-log;
- roadmap vNext;
- static scan catalog or test-plan evidence.

Expected safety assertions:

- scaffold remains disabled by default;
- no product writes;
- no DI registration;
- no command handler;
- no UI action;
- no DB/cloud/provider/KMS/WORM/external trust;
- every missing dependency fails closed;
- every overclaim returns a blocker.

## External Audit Checklist

- Path/canonicalization: Does every decision use canonical realpath, not display text?
- Reparse/symlink/junction: Are unresolved reparse risks rejected?
- Authority: Is product authority scoped, fresh, tamper-resistant and separate from test-only human GO?
- Redaction: Is redaction enforced before persistence, logging and errors?
- Replay/failure: Are replay evidence and read-model snapshots current and bound together?
- Rollback: Are rollback-safe and non-rollback cases explicit?
- Overclaims: Are WORM/KMS/cloud/external trust and release/commercial claims blocked?
- Runtime boundary: Is product runtime still disabled until explicit manual GO?
- Registration boundary: Are DI, handlers and UI product actions absent until a later approved scope?

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No product ledger implementation or runtime/product enablement added. |
| P1 | 0 | No scope leak found. |
| P2 | 0 | No blocking inconsistency found in the test plan. |
| P3 | 5 | Real canonicalization, reparse enforcement, product authority, rollback policy and disabled implementation scaffold remain future work. |
| P4 | 2 | Unicode/hardlink/ADS coverage may need platform-specific refinement; percentages remain conservative. |

## Readiness Matrix

| Area | Current readiness | Status |
| --- | ---: | --- |
| Product ledger path policy | 12-18% | NO-GO |
| Canonicalization/reparse test plan | 45-55% | GO for test-plan only |
| Product authority test plan | 40-50% | GO for test-plan only |
| Product implementation scaffold disabled | 0-10% | NO-GO until manual scope |
| Runtime/live product enablement | 0% | NO-GO |
| Release/commercial readiness | 0% | NO-GO |

## Recommendation

Recommended next safe macro-block:

`NODAL_OS_PRODUCT_LEDGER_PATH_CANONICALIZATION_REPARSE_AUTHORITY_TEST_PLAN_EXTERNAL_AUDIT_READ_ONLY`

Do not proceed to product implementation scaffold or product enablement without a new explicit manual GO.
