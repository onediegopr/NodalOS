# Durable Stage 2 Test-Only Implementation

Status: `TEST_ONLY_IMPLEMENTED / LOCAL_TEMP_ONLY / RUNTIME_PRODUCT_STILL_NO_GO`

Baseline HEAD: `7c8f9fa6b9d2648955baebe06ed7d1b91ea3eb44`

Decision: implement Durable Stage 2 only as a test-only/local-temp extension of `DurableAuditTrailAppendOnlyMinimal`. Runtime/product/live enablement, product ledger paths, service registration, command handlers, UI product actions, DB/migration, provider/cloud/network, Browser/CDP live automation, WCU/OCR live action, Recipes live execution and release/commercial readiness remain prohibited.

## Implemented Scope

The implementation adds `AppendStage2TestOnly(...)`, an explicit test-only entry point that delegates to the existing local/temp append path only after Stage 2 gates pass.

Implemented:

- explicit test-only feature flag gate;
- fail-closed missing, blank, malformed and product-scoped flag handling;
- explicit redaction-before-persistence proof gate;
- fail-closed missing, incomplete or unsuccessful redaction proof handling;
- product-ledger path fragment rejection even when the root is under temp;
- Stage 2 Safety tests for flag, redaction, product path and secret-like rejection;
- Stage 2 Recipes test for successful local/temp append with no product counters;
- strengthened static no-enable scan coverage through existing Safety guard.

Not implemented:

- product runtime feature flag or product config;
- service registration or hosted runtime integration;
- command handlers or command bus wiring;
- UI product actions;
- product ledger path;
- DB/migration/provider/cloud/network persistence;
- Browser/CDP, WCU/OCR or Recipes live write;
- production, WORM, compliance-grade, release-ready or commercial-ready behavior.

## Gate Behavior

| Gate | Behavior |
| --- | --- |
| Feature flag missing | Rejects with no append/write. |
| Feature flag blank or malformed | Rejects with no append/write. |
| Product-scoped flag | Rejects with no append/write. |
| Valid flag | Requires exact `enabled:test-only` plus explicit fixture marker. |
| Redaction proof missing | Rejects with no append/write. |
| Redaction proof incomplete or failed | Rejects with no append/write. |
| Product ledger path fragment | Rejects with no directory creation. |
| Secret-like content | Rejects through existing pre-write validation. |
| Valid gated request | Appends only to local/temp fixture storage through existing append-only path. |

## Tests And Validation

- `dotnet build OneBrain.slnx /p:UseSharedCompilation=false`: PASS, 0 warnings, 0 errors.
- `dotnet test tests/OneBrain.Safety.Tests/OneBrain.Safety.Tests.csproj --filter DurableAuditTrailAppendOnlyMinimal --no-restore /p:UseSharedCompilation=false`: PASS, 20/20.
- `dotnet test tests/OneBrain.Recipes.Tests/OneBrain.Recipes.Tests.csproj --filter DurableAuditTrailAppendOnlyMinimal --no-restore /p:UseSharedCompilation=false`: PASS, 6/6.
- `git diff --check`: PASS.
- Static scan changed code/tests: PASS; positive hits are guard-string fixtures inside tests only.

The initial parallel validation attempt hit a shared compiler file lock. Validation was retried sequentially with shared compilation disabled and passed.

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No runtime/product/live authority introduced. |
| P1 | None. Scope remains test-only/local-temp. |
| P2 | None for the authorized Stage 2 test-only scope. |
| P3 | Redaction proof is caller-attested test-only evidence, not a product redaction service. |
| P3 | Feature flag is an explicit test-only gate object, not product configuration. |
| P3 | Property/concurrency expansion, replay/read-model and checkpoint/truncation Stage 2 hardening remain future safe macro-blocks. |
| P4 | Historical docs remain traceability records under latest decision-log canon. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Durable Audit Trail local/test-safe append/write candidate | 94-96% |
| Durable Stage 1 test-only enablement safety | 90-93% |
| Durable Stage 2 test-only gates | 82-88% |
| Durable Stage 2 test-only implementation | 78-85% |
| Runtime/live product enablement | 0% |
| Product enablement | 0% |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 22-32% |

## Decision

`GO_WITH_FINDINGS_DURABLE_STAGE2_TEST_ONLY_IMPLEMENTATION_READY`

The next safe macro-block is `NODAL_OS_DURABLE_STAGE2_TEST_ONLY_EXTERNAL_AUDIT_AND_FIXES`, read-only/docs-first with targeted fixes only if it finds non-P0/P1 issues.
