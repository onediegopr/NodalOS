# Teach NODAL PR #55 - local closeout

## Decision

`GO_WITH_BASELINE_FINDINGS_TEACH_NODAL_PR55_LOCAL_VALIDATED_READY_FOR_MERGE_REVIEW`

PR #55 implements the Teach NODAL Record -> Review Draft thin slice and is locally validated. GitHub Actions were not used as evidence because credits were unavailable. PR #54 was not touched.

Initial A/B HEAD: `9625adf4769353918e59e90a87c826cd209856d7`.

Compared refs:

- Baseline: `origin/main` at `78f2a1988c68478eea9a65ecb4a04aa82f0d7483`.
- PR #55: `feature/teach-nodal-record-review-thin-slice` at `9625adf4769353918e59e90a87c826cd209856d7`.

## Local validation evidence

Teach NODAL and relevant local regressions were validated on PR #55 before this docs-only closeout:

- `NodalOsTeachNodalProductTests`: PASS `24/24`.
- `OneBrain.Runtime.Tests`: PASS `50/50`.
- `OneBrain.Pilot` build: PASS.
- `OneBrain.Recipes.Tests` build: PASS with one existing warning, `MSTEST0032` in `PilotShellTests.cs`.
- `OneBrain.slnx` build: PASS.

## Product Ledger A/B comparison

Full Recipes comparison:

- `origin/main`: FAIL `1625 passed / 6 failed / 0 skipped / 1631 total`.
- PR #55: FAIL `1649 passed / 6 failed / 0 skipped / 1655 total`.

Focused repetition of the same six failures:

- `origin/main` focal run 1: FAIL `0 passed / 6 failed`.
- `origin/main` focal run 2: FAIL `0 passed / 6 failed`.
- PR #55 focal run 1: FAIL `0 passed / 6 failed`.
- PR #55 focal run 2: FAIL `0 passed / 6 failed`.

The six failing Product Ledger tests are identical in baseline and PR #55:

1. `ProductLedgerPublicUiActionSurfaceTests.PublicUiActionSurface_CompletesBoundedLocalExportRecipe`
2. `ProductLedgerPublicUiActionSurfaceTests.PublicUiActionSurface_CompletesLocalOnlyReadActionsRecipe`
3. `ProductLedgerLocalOnlyOperatorDiagnosticsSurfaceTests.OperatorDiagnosticsSurface_RendersLocalOnlyOperatorSnapshotRecipe`
4. `ProductLedgerPublicUiReadOnlyDisabledPreviewTests.PublicUiReadOnlyDisabledPreview_RendersDisabledMockRecipe`
5. `ProductLedgerInternalOperatorUiPreviewTests.InternalOperatorUiPreview_RendersCockpitRecipeFromDiagnosticsSurface`
6. `ProductLedgerInternalCommandPreviewRouterTests.InternalCommandPreviewRouter_MapsOperatorUiActionsToNoOpReadOnlyPreviewsRecipe`

Failure mode:

- Five tests receive `Rejected` where Product Ledger recipe expectations still expect local preview or local non-destructive completion.
- One diagnostics recipe expects the stale next-step token `EXTERNAL_AUDIT_READ_ONLY_THEN_STATIC_SCAN_HARDENING`, while current runtime readiness metadata reports the local-dev runtime/product readiness action.

## Causal conclusion

`BASELINE_EXISTING_NOT_PR55_REGRESSION`.

Evidence:

- The six failures reproduce on `origin/main`.
- The six failures reproduce on PR #55 with the same focused filter.
- PR #55 diff does not touch `src/OneBrain.Core/Approval/ProductLedger*`.
- PR #55 diff does not touch `tests/OneBrain.Recipes.Tests/ProductLedger*`.
- Cross-scan found no Teach NODAL references inside Product Ledger sources/tests.
- Cross-scan found no Product Ledger references inside Teach NODAL PR #55 sources/tests.

Therefore the Product Ledger failures are baseline state, not caused by the Teach NODAL thin slice.

## Boundaries preserved

- No GitHub Actions evidence used.
- No merge performed.
- No PR #54 change.
- No Product Ledger source or tests modified in this closeout.
- No production runtime/product activation.
- No public Product Ledger exposure.
- No CI enforcement.
- No workflow change.
- No DB, cloud, external provider, KMS, WORM, customer data, release, or commercial path.

## Remaining review item

GitHub review thread resolution was not changed from local Codex. If GitHub authenticated review metadata is unavailable, the merge reviewer must confirm thread state in GitHub before pressing merge.

## Merge recommendation

PR #55 is locally ready for merge review with baseline findings documented. The remaining non-green Recipes signal is a pre-existing Product Ledger baseline issue and should not block PR #55 unless the reviewer chooses to enforce full Recipes green before merge.
