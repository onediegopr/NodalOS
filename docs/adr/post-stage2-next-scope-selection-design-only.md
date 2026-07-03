# Post Stage 2 Next-Scope Selection (Design-Only)

## Status

`DESIGN_ONLY` / `NEXT_SCOPE_SELECTION` / `PRODUCT_AUTHORITY_NOT_GRANTED`

This ADR records the next-scope decision after the Durable Stage 2 test-only line closeout.
It does not authorize runtime/product enablement, a new implementation scope, or any live
capability. It is a selection and gating record only.

## Baseline

- HEAD: `ec2ecfcbe02b3f5611543c736694808a5fb3dfd8`
- Branch: `chrome-lab-001-extension-local-ai-bridge`
- Prior closeout: `GO_WITH_FINDINGS_DURABLE_STAGE2_TEST_ONLY_CLOSEOUT_READY`
- Standing stop point: `PAUSE_FOR_MANUAL_GO_BEFORE_RUNTIME_PRODUCT_ENABLEMENT_OR_NEW_SCOPE`

## Options Considered

- A — Stage 2 external hardening continuation (test-only only). Stays inside the authorized
  test-only line; no new scope. Diminishing returns.
- B — Redaction-before-persistence service design-only. Addresses the top P3 (redaction is
  currently only a deterministic caller-attested test-only gate). Design-only, no wiring.
- C — Runtime feature flag architecture design-only.
- D — Checkpoint/WORM/KMS/cloud external limitation audit (design/audit only). Addresses the
  second P3.
- E — Browser/CDP/WCU/OCR/Recipes next boundary audit (audit/design only).
- F — Roadmap release-blockers global audit (docs/audit only).

## Decision

- Primary recommendation: **Option B** — `NODAL_OS_REDACTION_BEFORE_PERSISTENCE_SERVICE_DESIGN_ONLY`.
- Zero-new-scope fallback that can proceed inside the current authorization: **Option A**.

## Gating / Continuation

- Options B, C, D and E each open a **new scope direction**. Per the standing stop point and
  the continuation rule ("expand scope beyond test-only → stop"), they require a fresh manual
  GO before starting.
- Option A is the only option strictly within the already-authorized test-only scope.
- This block therefore closes as a completed audit + selection and does not auto-chain.

## What Is Allowed (when a future block is GO'd)

- Design-only architecture documents.
- Read-only audits.
- Test-only fixture hardening (Option A).

## What Is Prohibited (unchanged)

Runtime/live/product enablement, product ledger path, service registration, command
handlers, command bus wiring, UI product actions, DB/migration, provider/cloud/network,
Browser/CDP live automation, WCU/OCR live action, Recipes live execution, a product
redaction service implementation, external checkpoint/WORM/KMS/cloud trust, Stage 3
implementation, release/commercial readiness and stash modification.

## Non-Goals

This ADR grants no product runtime authority and does not begin any selected option; it only
records the ranked selection and the manual-GO gate.
