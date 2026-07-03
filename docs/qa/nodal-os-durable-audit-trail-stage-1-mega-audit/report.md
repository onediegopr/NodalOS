# NODAL OS Durable Audit Trail Stage 1 — Claude Mega-Audit And Controlled Fixes Report

## Decision

`GO_CLAUDE_MEGA_AUDIT_DURABLE_AUDIT_TRAIL_STAGE_1_FIXES_READY`

This block audited the full Durable Audit Trail Stage 1 line and applied controlled,
test-only/local-safe fixes. It does not enable product runtime, service registration,
command handlers, UI product actions, product ledger paths, DB/migration,
provider/cloud/network, Browser/CDP, WCU/OCR, recipes live writes, release readiness or
commercial readiness.

## Repo Guard

- Repo: `C:/DESARROLLO/NodalOS/Codigo-m12-audit`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Initial HEAD: `f557b574ccf5850a92b9202b338cc10f9ad4f164`
- Origin sync initial: `0 0`
- Worktree initial: clean
- Stash: listed only, not touched (`stash@{0}: On chrome-lab-001-extension-local-ai-bridge: pre-m11-legacy-state`)

## Scope Of This Block

Permitted: bug fixes, additional local/test-safe hardening, validation improvements,
Safety/Recipes test hardening, static-guard improvements and documentation coherence
corrections inside Stage 1 test-only scope.

Prohibited (unchanged): product runtime, service registration, command handlers,
UI product actions, product ledger path, DB/migration, cloud/network/provider,
Browser/CDP/WCU/OCR/recipes live writes, Stage 2 dev sandbox, release/commercial
readiness, stash modification and any write outside temp/local-test.

## Findings

### P0 / P1

None.

### P2 — Null-input fail-closed violation (fixed)

- `ContainsSecretLikeContent` called `value.ToLowerInvariant()` with no null guard.
- It was invoked on `ActorReference`, `ApprovalReference`, each `EvidenceReferences`
  element and each metadata key/value without an early return after the `Missing*`
  checks, so a null reference field, a null evidence element, a null evidence list
  (`request.EvidenceReferences.Count`) or a null metadata value threw
  `NullReferenceException` instead of returning a clean `Rejected` result.
- A null metadata value that reached persistence would have serialized as JSON `null`
  and poisoned the ledger, because the read-side `ValidateEntryShape` treats a null
  metadata value as an integrity failure, blocking all future appends.
- Impact: fail-closed contract broken on malformed input. Real-world reachability is
  limited by C# nullable-reference-type contracts on the request record, but a
  safety-critical audit trail must fail closed rather than throw.

Fix:
- `ContainsSecretLikeContent(string?)` now returns `false` for null/empty input.
- The secret scan over `EvidenceReferences` and `Metadata` is null-safe
  (`?.Any(...) ?? false`).
- `EvidenceReferences` null list is now treated as `MissingEvidenceReference`.
- Added a new `MalformedMetadata` reject reason for null/whitespace metadata keys and
  null metadata values, mirroring the existing read-side shape validation.

### P3 — VerifyFile / Append boundary asymmetry (documented, not changed — intentional seam)

- `Append` honors `AllowLocalTestStorageOnly = false` and will write outside the temp
  boundary, but `VerifyFile` unconditionally fails closed outside the temp boundary.
- `AllowLocalTestStorageOnly = false` is a documented future-approved-caller seam (see
  the append-only implementation report and the Stage 1 ADR). It is never exercised in
  Stage 1 and the default (`true`) keeps writes inside the temp/local-test boundary.
- Left unchanged because altering this flag touches a future-enablement design seam, not
  Stage 1 test-only behavior. Recorded as a remnant risk for the external audit / Stage 2
  planning: a future caller could create a ledger it cannot later verify with `VerifyFile`.

### P4 — Documentation overclaim: build warnings (fixed in docs)

- The prior Stage 1 QA report/json and handoff state the solution build produced
  `0 warnings`. The actual `dotnet build OneBrain.slnx` emits 33 pre-existing warnings
  from unrelated legacy files (historical OCR/ONNX diagnostics and MSTEST analyzer
  suggestions). Zero warnings originate from the Durable Audit Trail Stage 1 files.
- Corrected the wording to "0 errors; 33 pre-existing unrelated warnings, 0 from Stage 1
  files".

### P4 — Static per-path lock map growth (documented, not changed)

- `LedgerLocks` is a process-static `ConcurrentDictionary` keyed by full ledger path and
  never evicts entries. In test-only use with per-test temp GUID roots the map grows for
  the lifetime of the test process. Impact is negligible in Stage 1 and eviction would
  add lock-lifetime complexity; recorded as a remnant, not fixed.

## Fixes Applied

- `src/OneBrain.Core/Approval/DurableAuditTrailAppendOnlyMinimal.cs`
  - Added `MalformedMetadata` reject reason.
  - Null-total write-side validation (null evidence list, null/whitespace metadata key,
    null metadata value).
  - Null-safe `ContainsSecretLikeContent` and null-safe secret scan over collections.
- `tests/OneBrain.Safety.Tests/DurableAuditTrailAppendOnlyMinimalSafetyTests.cs`
  - Added `MinimalLedger_FailsClosedForNullReferenceFieldsAndMalformedMetadataWithoutThrowing`
    proving null actor/approval/evidence-element/evidence-list/metadata-value and blank
    metadata key all fail closed with no side effects and no ledger file creation.

## Tests Executed

- `dotnet build OneBrain.slnx`
  - Result: PASS — 0 errors; 33 pre-existing unrelated warnings (legacy OCR/ONNX +
    MSTEST analyzer), 0 from Stage 1 files.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter "FullyQualifiedName~DurableAuditTrailAppendOnlyMinimal" --no-build`
  - Result: PASS — 16 passed (was 15; +1 new null-safety test).
  - Writes: temp/local-test only.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --filter "FullyQualifiedName~DurableAuditTrailAppendOnlyMinimal" --no-build`
  - Result: PASS — 5 passed.
  - Writes: temp/local-test only.

## Additional Validations

- `git diff --check`: PASS (only informational LF/CRLF normalization notices).
- Static enablement-token scan over changed and Durable Audit Trail files: no TRUE_RISK.
  - Classification: capability-denied `*Allowed/*Ready: false` fields and `!policy.Enabled`
    gate are negative assertions / accepted test-safe wording; the forbidden-fragment
    arrays are test-guard literals; the `...WithoutProductAction...` /
    `...DoesNotRegister...` names are negative assertions. None of the added lines
    introduced an enablement token.
- JSON validation for this report's `report.json`: PASS.

## Architecture / Scope Confirmation

- Implemented-not-enabled: preserved.
- Test-only / temp-local-test ledger only: preserved (default policy).
- No runtime, no service registration, no command handlers, no UI product actions, no
  product ledger path, no DB/cloud/provider/network, no Browser/CDP/WCU/OCR/recipes live
  writes: preserved.
- No release/commercial readiness: preserved (`NO-GO`).
- No scope leak detected.

## Remaining Risks

- P3 boundary asymmetry seam (`AllowLocalTestStorageOnly = false` vs `VerifyFile`) — for
  external audit / Stage 2.
- P4 static lock-map growth — negligible in Stage 1.
- All product-enablement blockers from the Stage 1 ADR remain open.

## What Was NOT Enabled

Nothing new was enabled. No product runtime, service registration, command handler, UI
product action, product ledger path, DB/migration, provider/cloud/network,
Browser/CDP/WCU/OCR/recipes live write, Stage 2 dev sandbox or release/commercial
readiness was introduced. The stash was not modified.

## Percentages

- Durable audit trail local/test-safe append/write candidate: `92-95%`
- Stage 1 test-only enablement safety: `85-90%` (null-input fail-closed gap closed; now at
  the upper part of the range)
- Product enablement: `0%`
- Runtime/live: `0%`
- Execution/mutation broad: `0%`
- Release/commercial readiness: `0% / NO-GO`
- Project usable end-to-end estimate: `20-30%`

## Next Recommended Macro-Block

`NODAL_OS_DURABLE_AUDIT_TRAIL_STAGE_1_TEST_ONLY_EXTERNAL_AUDIT_READ_ONLY`

Read-only external audit of this Stage 1 hardening (including the documented P3 boundary
asymmetry seam) before any Stage 2 planning. No enablement.
