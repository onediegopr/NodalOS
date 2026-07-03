# Durable Stage 2 Planning Design-Only Gate

Status: `DESIGN_ONLY / STAGE2_PLANNING / STAGE2_IMPLEMENTATION_STILL_PROHIBITED`

Baseline HEAD: `87e8b66dd251c7af24127d7e4b926063ec2008dc`

Decision: define the allowed Stage 2 planning scope without implementing Stage 2. Stage 2 implementation, product/runtime enablement, product ledger paths, service registrations, command handlers, UI product actions, DB/migration, provider/cloud/network, Browser/CDP, WCU/OCR, Recipes live execution and release/commercial readiness remain prohibited.

## Purpose

This ADR converts the prior Stage 2 readiness gate into a concrete design-only planning packet. It identifies what a future Durable Stage 2 test-only macro-block would need to prove, while preserving the current canonical blockers.

This ADR is documentation-only. It does not change `DurableAuditTrailAppendOnlyMinimal`, tests, `Program.cs`, endpoints, service registration, command handlers, runtime flags, product UI, product ledger paths, DB/migration, providers, network calls, Browser/CDP, WCU/OCR, Recipes or release/commercial status.

## Stage 2 Planning Scope

Allowed now:

- Define Stage 2 test-only acceptance gates.
- Define local/temp/dev-sandbox-only storage constraints.
- Define redaction-before-persistence preconditions.
- Define runtime feature flag fail-closed preconditions.
- Define no-product-ledger, no-service-registration and no-command-handler negative requirements.
- Define append-only/property/concurrency/replay/read-model/checkpoint test plan requirements.
- Define external audit and manual GO requirements before implementation.

Prohibited now:

- Implementing Stage 2.
- Adding or changing source code/tests for Stage 2 behavior.
- Creating runtime feature flags.
- Creating product ledger paths.
- Registering services or command handlers.
- Adding UI product actions.
- Enabling DB, migration, provider, cloud or network paths.
- Connecting Durable Audit Trail to Browser/CDP, Pilot, Nexa, WCU/OCR or Recipes live execution.
- Claiming production, release, commercial, WORM or compliance-grade readiness.

## Candidate Stage 2 Definition

Stage 2, if later explicitly authorized, may only mean a test-only/dev-sandbox expansion of the existing local/test-safe Durable Audit Trail candidate. It must not mean product runtime enablement.

Candidate Stage 2 may include, only after a future explicit GO:

- a test-only redaction-before-persistence proof layer;
- a test-only fail-closed feature-flag evaluator;
- stronger local/temp path boundary assertions;
- schema compatibility tests for future event envelope versions;
- append-only property tests and higher-volume concurrency stress tests;
- replay/read-model verification that never mutates domain state;
- checkpoint/truncation evidence tests limited to local/test-safe trust boundaries;
- negative scans proving no service registration, command handler, product ledger path, DB/migration, network/provider, Browser/CDP, WCU/OCR or Recipes live write.

Candidate Stage 2 must keep all writes under local/temp or explicitly fixture-created dev sandbox paths and must return product/runtime authority counters as false or zero.

## Gate Matrix

| Gate ID | Area | Required condition before Stage 2 implementation | Current status | Blocker severity |
| --- | --- | --- | --- | --- |
| DS2-G0 | Baseline | Repo clean, branch expected, origin `0 0`, stash listed only. | PASS | None |
| DS2-G1 | Stage 1 evidence | Stage 1 local/test-safe append/write evidence accepted. | PASS_WITH_FINDINGS | P2/P3 future hardening |
| DS2-G2 | Scope lock | Stage 2 limited to test-only/dev-sandbox, not product runtime. | PASS_FOR_PLANNING | P0 if violated |
| DS2-G3 | Redaction-before-persistence | Redaction proof exists before any new persistence behavior. | BLOCKED_FOR_IMPLEMENTATION | P2 blocker |
| DS2-G4 | Runtime feature flag | Fail-closed test/dev flag model exists before enablement. | BLOCKED_FOR_IMPLEMENTATION | P2 blocker |
| DS2-G5 | Product ledger path | Product ledger path remains absent and prohibited. | PASS | P0/P1 if violated |
| DS2-G6 | Service registration | No service registration or hosted runtime integration. | PASS | P0/P1 if violated |
| DS2-G7 | Command handlers | No command handler or command bus integration. | PASS | P0/P1 if violated |
| DS2-G8 | UI product action | No UI product action or approval button wiring. | PASS | P0/P1 if violated |
| DS2-G9 | Storage boundary | All future writes constrained to local/temp or fixture-created dev sandbox. | PARTIAL_DESIGN_ONLY | P2 |
| DS2-G10 | Replay/read model | Replay/read model remains read-only and cannot mutate domain state. | PARTIAL_DESIGN_ONLY | P2 |
| DS2-G11 | Checkpoint/truncation | Checkpoint/truncation evidence stays local/test-safe; external trust boundary is future only. | PARTIAL_DESIGN_ONLY | P3 |
| DS2-G12 | Cross-boundary | No Browser/CDP, Pilot, Nexa, WCU/OCR or Recipes live cross-enable. | PASS_WITH_FINDINGS | P2/P3 |
| DS2-G13 | Tests | Future implementation requires negative tests before code and focused Safety/Recipes tests after code. | DESIGN_ONLY | P2 |
| DS2-G14 | External audit | External audit required before Stage 2 implementation. | REQUIRED_BEFORE_IMPLEMENTATION | P2 |
| DS2-G15 | Manual GO | Explicit manual GO required before Stage 2 implementation. | REQUIRED_BEFORE_IMPLEMENTATION | P1/P0 if bypassed |
| DS2-G16 | Release/commercial | Release/commercial remains `0% / NO-GO`. | PASS | P0 if overclaimed |

## Required Future Test Plan

Before any Stage 2 code change, a future macro-block must add or identify tests for:

- missing redaction-before-persistence blocks append;
- raw payload, secret-like data and unreviewed PII do not persist;
- missing, malformed or unknown feature flag fails closed;
- product/runtime flag cannot be used as a test/dev flag;
- storage outside local/temp/dev-sandbox boundary is rejected;
- product ledger path fragments are rejected or absent;
- service registration scan remains negative;
- command handler scan remains negative;
- UI product action scan remains negative;
- DB/migration/provider/network scan remains negative;
- Browser/CDP, WCU/OCR and Recipes live write scans remain negative;
- replay/read-model produces verification only and no mutation;
- checkpoint/truncation evidence cannot claim external trust when only local evidence exists;
- release/commercial readiness flags remain false.

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No product/runtime/live authority is introduced. |
| P1 | None. No source, tests or runtime behavior changed. |
| P2 | Redaction-before-persistence remains unresolved and blocks implementation. |
| P2 | Runtime feature flag remains design-only/missing and blocks implementation. |
| P2 | Future Stage 2 needs negative tests before code for service registration, handlers, product ledger, UI action, DB/migration, provider/network and live cross-boundary paths. |
| P3 | Replay/read-model and checkpoint/truncation evidence remain design-only and local/test-safe only. |
| P4 | Historical Durable docs remain traceability records under the latest decision-log canon. |

## Decision

`GO_WITH_FINDINGS_DURABLE_STAGE2_PLANNING_DESIGN_ONLY_GATE_READY`

Stage 2 planning is now more specific, but Stage 2 implementation remains prohibited. The next macro-block must not implement Stage 2 unless an external audit and explicit manual GO first authorize a test-only implementation scope.
