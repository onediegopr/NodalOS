# Durable Stage 2 Pre-Implementation Evidence Pack Design-Only

Status: `DESIGN_ONLY / PRE_IMPLEMENTATION_EVIDENCE_PACK / STAGE2_IMPLEMENTATION_NOT_AUTHORIZED`

Baseline HEAD: `21b47e592b01bcb49c4c0702312222ff38f55ffd`

Decision: define the evidence pack required before any future Durable Stage 2 test-only implementation can be considered. This pack is documentation-only and does not authorize Stage 2 implementation, runtime/product/live behavior, service registration, command handlers, UI product actions, product ledger paths, DB/migration, provider/cloud/network, Browser/CDP, WCU/OCR, Recipes live execution, release readiness or commercial readiness.

## Scope Lock

This ADR is a pre-implementation readiness artifact. It converts the Stage 2 planning blockers into required evidence, negative-test inventory and audit gates.

Allowed now:

- document redaction-before-persistence evidence requirements;
- document runtime feature flag fail-closed evidence requirements;
- document negative no-enable scan requirements;
- document replay/read-model, checkpoint/truncation and failure/non-rollback evidence requirements;
- prepare QA, handoff and decision-log traceability.

Prohibited now:

- implementing Stage 2;
- changing source code or tests for Stage 2 behavior;
- adding runtime feature flags;
- creating product ledger paths;
- registering services, handlers, command buses or UI actions;
- adding DB, migration, provider, cloud, network, Browser/CDP, WCU/OCR or Recipes live paths;
- claiming production, WORM, compliance-grade, release-ready or commercial-ready status.

## Evidence Gate Matrix

| Gate ID | Evidence area | Required proof before any implementation | Current status | Severity if bypassed |
| --- | --- | --- | --- | --- |
| EP-G0 | Repo guard | Clean worktree, expected branch, origin `0 0`, stash listed only. | PASS | P1 |
| EP-G1 | Stage 1 baseline | Existing local/test-safe append/write candidate and Safety/Recipes tests remain unchanged. | PASS | P1 |
| EP-G2 | Stage 2 scope | Future Stage 2 remains test-only/dev-sandbox and not product runtime. | PASS_FOR_DESIGN | P0 |
| EP-G3 | Redaction policy reference | A named redaction policy is identified before append construction. | MISSING_FOR_IMPLEMENTATION | P2 |
| EP-G4 | Field classification | Every future persisted field has a safe classification and forbidden-input handling. | MISSING_FOR_IMPLEMENTATION | P2 |
| EP-G5 | Forbidden data classes | Credentials, tokens, API keys, cookies, auth headers, raw approval payloads, raw user files/file contents, raw Browser/CDP/WCU/OCR/Recipe/provider/network payloads, sensitive absolute paths, unreviewed PII and redaction failure payloads are rejected or omitted before persistence. | MISSING_FOR_IMPLEMENTATION | P2 |
| EP-G6 | Proof before append | Future implementation must prove classification/redaction completed before any append call or file write. | MISSING_FOR_IMPLEMENTATION | P2 |
| EP-G7 | Redaction fail-closed | Missing policy, missing classifier, redaction failure or sensitive input causes no append and no ledger creation. | MISSING_FOR_IMPLEMENTATION | P2 |
| EP-G8 | Runtime flag default | Feature flag model defaults off. Missing, malformed, unknown or product-scoped flag values remain off. | MISSING_FOR_IMPLEMENTATION | P2 |
| EP-G9 | Runtime flag boundary | Test/dev flag cannot register services, command handlers, command bus routes, UI product actions or product ledger paths. | MISSING_FOR_IMPLEMENTATION | P2 |
| EP-G10 | Product ledger path | Product ledger path remains absent and prohibited. | PASS_DESIGN | P0/P1 |
| EP-G11 | Service/handler/UI no-enable | Service registration, command handlers and UI product actions remain absent. | PASS_DESIGN | P0/P1 |
| EP-G12 | Provider/network no-enable | DB/migration/provider/cloud/network paths remain absent. | PASS_DESIGN | P0/P1 |
| EP-G13 | Cross-boundary no-enable | Browser/CDP, WCU/OCR and Recipes live writes remain absent. | PASS_DESIGN | P0/P1 |
| EP-G14 | Replay/read model | Replay/read model verifies ledger state only and cannot mutate domain state. | DESIGN_ONLY | P3 |
| EP-G15 | Checkpoint/truncation | Evidence remains local/test-safe; external trust, KMS, WORM and cloud claims remain forbidden. | DESIGN_ONLY | P3 |
| EP-G16 | Failure/non-rollback | Failed append preserves existing evidence; rollback is future append-only compensating evidence, never deletion or mutation. | DESIGN_ONLY | P3 |
| EP-G17 | Negative tests before code | Future implementation block must start from the negative test inventory in this pack. | REQUIRED | P2 |
| EP-G18 | External audit/manual GO | External audit and explicit manual GO remain required before implementation. | REQUIRED | P1/P0 |
| EP-G19 | Release/commercial | Release/commercial readiness remains `0% / NO-GO`. | PASS | P0 |

## Redaction-Before-Persistence Evidence Pack

Future implementation evidence must include:

- a policy reference for the redaction/classification rule set;
- a field classification table for event kind, actor reference, approval reference, evidence references, metadata keys, metadata values, event identifiers, timestamps, hashes, sequence numbers and reject reasons;
- a forbidden data class table covering credentials, tokens, API keys, cookies, auth headers, raw approval payloads, raw user files/file contents, raw Browser/CDP/WCU/OCR/Recipe/provider/network payloads, sensitive absolute paths, unreviewed PII and redaction failure payloads;
- proof that classification and redaction happen before ledger entry construction and before any append/write call;
- fail-closed behavior for missing policy, missing classifier, malformed metadata, redaction failure, raw payload and sensitive input;
- negative tests proving rejected inputs produce `AppendWriteCount = 0`, `PersistedEventCount = 0`, no ledger file creation when applicable and no product counters enabled.

Stage 1 already rejects raw payloads and secret-like content in the isolated local/test-safe candidate. That is not enough for Stage 2 implementation because it is not a complete redaction-before-persistence proof.

## Runtime Feature Flag Fail-Closed Evidence Pack

Future implementation evidence must include:

- default-off behavior;
- missing flag means off;
- malformed flag means off;
- unknown environment means off;
- product-scoped flag remains forbidden;
- test-only flag can only affect focused test/dev-sandbox execution paths;
- no flag can register services, command handlers, command bus routes, UI product actions, DB/migration paths, provider/network paths, Browser/CDP, WCU/OCR or Recipes live execution;
- flag evaluation failure causes no append, no runtime registration and no product authority upgrade.

This pack does not create a flag. It defines the required fail-closed contract only.

## Negative Test Inventory Before Code

A future implementation block must identify or add tests for:

- no product ledger path fragments;
- no service registration;
- no command handler or command bus wiring;
- no UI product action;
- no DB/migration/provider/cloud/network;
- no Browser/CDP live product automation;
- no WCU/OCR live action;
- no Recipes live execution;
- no release/commercial readiness flags;
- missing redaction policy blocks append;
- missing classifier blocks append;
- raw payload, secret-like data and unreviewed PII do not persist;
- malformed redaction result blocks append;
- missing, malformed, unknown and product-scoped feature flags fail closed;
- replay/read-model performs verification only and no mutation;
- checkpoint/truncation evidence cannot claim external trust from local-only evidence;
- failed append preserves existing ledger and never deletes or mutates previous entries.

## Replay, Checkpoint And Failure Evidence

Replay/read-model evidence must be read-only. It may verify entry count, sequence continuity, last hash, hash-chain integrity and error codes. It must not mutate approval state, command state, product state, Browser/CDP state, WCU/OCR state or Recipes state.

Checkpoint/truncation evidence remains local/test-safe. It may distinguish internal hash-chain validity from checkpoint-assisted tail-deletion or rollback suspicion. It must not claim external trust, WORM, KMS, cloud, compliance-grade or production audit authority.

Failure/non-rollback evidence must prove that a failed append keeps previous evidence intact and does not create a compensating deletion. A future business rollback can only be represented as a new audit event after separate authorization.

## Findings

| Severity | Finding |
| --- | --- |
| P0 | None. No product/runtime/live authority is introduced. |
| P1 | None. No source, tests or runtime behavior changed. |
| P2 | Redaction-before-persistence remains missing for implementation. |
| P2 | Runtime feature flag fail-closed proof remains missing for implementation. |
| P2 | Negative tests must be materialized before any Stage 2 code. |
| P3 | Replay/read-model, checkpoint/truncation and failure/non-rollback remain design-only evidence contracts. |
| P4 | Historical docs remain traceability records under latest decision-log canon. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Durable Audit Trail local/test-safe append/write candidate | 92-95% |
| Durable Stage 1 test-only enablement safety | 88-92% |
| Authority boundary external audit and Stage 2 readiness gate | 80-85% |
| Durable Stage 2 planning design-only gate | 82-88% |
| Durable Stage 2 planning external audit | 80-86% |
| Durable Stage 2 pre-implementation evidence pack | 75-82% design-only |
| Stage 2 planning readiness | 80-86% design-only |
| Stage 2 implementation readiness | 0% / BLOCKED |
| Runtime/live product enablement | 0% |
| Execution/mutation broad | 0% |
| Release/commercial readiness | 0% / NO-GO |
| Proyecto usable end-to-end | 20-30% |

## Decision

`GO_WITH_FINDINGS_DURABLE_STAGE2_PRE_IMPLEMENTATION_EVIDENCE_PACK_READY`

The evidence pack is ready for read-only external audit. Stage 2 implementation remains blocked until the pack is audited, required evidence is complete and explicit manual GO is recorded.
