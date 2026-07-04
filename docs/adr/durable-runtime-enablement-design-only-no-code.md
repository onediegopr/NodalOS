# Durable Runtime Enablement Design-Only No-Code

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_DURABLE_RUNTIME_ENABLEMENT_DESIGN_ONLY_NO_CODE_READY`

## Scope

Docs-only runtime enablement plan for future Durable Stage 2 product adoption.

This ADR does not implement runtime/product behavior, register services, add command handlers, add UI product actions, create a product ledger path, add DB/migration/provider/cloud/network integration, enable Browser/CDP/WCU/OCR/Recipes live behavior, or claim release/commercial readiness.

## Current Canon

- Durable Stage 2 remains test-only/local-temp.
- Redaction-before-persistence remains isolated Core/test-only.
- Runtime feature flag remains exact test-only only and is not a product rollout system.
- Product ledger path remains `0% / NO-GO`; the existing path guard is a test-only fragment heuristic.
- Checkpoint trust remains local-only/no-provider/test-only.
- Runtime/product/release/commercial readiness remains `0% / NO-GO`.

## Required Product Enablement Gates

Future runtime/product enablement requires all gates below before any implementation:

| Gate | Requirement | Current Status |
| --- | --- | --- |
| G1 | Product ledger root policy, canonicalization and containment | Design-only threat model exists; implementation `0% / NO-GO` |
| G2 | Product redaction wiring with product policy id/version and no raw logging | Design-only wiring exists; implementation `0% / NO-GO` |
| G3 | Product runtime feature flag with ownership, kill switch and dependency gates | Design-only readiness exists; implementation `0% / NO-GO` |
| G4 | Product service registration authority | Not authorized |
| G5 | Command handler and command bus authority | Not authorized |
| G6 | UI product action authority | Not authorized |
| G7 | Checkpoint/trust policy selected for product boundary | Local-only/no-provider/test-only only |
| G8 | Replay/read-model and schema compatibility evidence | Test-only/local-temp evidence only |
| G9 | Failure, rollback and non-rollback policy | Design evidence only |
| G10 | External audit plus manual GO | Required before implementation |

## Enablement Order

1. Implement product ledger policy only after manual GO.
2. Implement product redaction wiring only after product ledger policy and manual GO.
3. Implement product runtime feature flag only after dependency gates and manual GO.
4. Add service registration only after authority boundary review and manual GO.
5. Add command/UI product actions only after command and UI authority review and manual GO.
6. Run external audit before any runtime/product activation.

## Required Future Negative Tests

- Product append rejects missing product ledger policy.
- Product append rejects path traversal, UNC, symlink/junction escape and non-canonical paths.
- Product append rejects missing, stale or test-only redaction result.
- Product append rejects test-only runtime feature flag in product mode.
- Product append rejects product flag enablement without dependency gates.
- Product append rejects service registration before manual GO evidence.
- Command/UI scans prove no unauthorized product action path.
- Provider/cloud/network scans prove no unintended external dependency.
- Browser/CDP/WCU/OCR/Recipes live metadata remains blocked unless separately authorized.
- Rollback emits compensating evidence and never mutates prior entries.

## Test-Only Scaffold Implemented

Follow-up implementation block `NODAL_OS_DURABLE_RUNTIME_IMPLEMENTATION_SAFETY_SCAFFOLD_TEST_ONLY_MACROBLOCK` added `DurableRuntimeEnablementSafetyScaffold` as an isolated Core evaluator.

Implemented scope:

- test-only readiness preview models and typed blockers;
- fail-closed top-level runtime/product/release guard;
- product ledger path readiness scaffold with local boundary checks and provider/cloud/WORM/KMS overclaim blockers;
- redaction product wiring scaffold requiring redaction result, exact candidate hash, explicit policy id, evidence and no raw secret markers;
- runtime feature flag product-readiness scaffold that remains blocked by default and requires ledger, redaction, authority, replay/failure and human GO evidence;
- authority wiring scaffold that blocks missing human approval, local/test operator identity, reason, evidence, scope overrun, live automation and provider/cloud/KMS/WORM attempts;
- replay/failure evidence scaffold that blocks missing replay/failure evidence, missing limitation acknowledgments and durable recovery overclaims;
- status text `NO_PRODUCT_RUNTIME_ENABLEMENT`.

Still not implemented:

- runtime/product enablement;
- active product ledger path;
- productive service registration;
- command handlers;
- UI product actions;
- DB/migration/provider/cloud/network;
- Browser/CDP/WCU/OCR/Recipes live behavior;
- KMS/WORM/cloud/external trust;
- release/commercial readiness.

## Stop Point

The next high-value step after this plan would introduce implementation/runtime/product enablement authority. That requires explicit manual GO.

Stop point after the scaffold: `NODAL_OS_DURABLE_RUNTIME_IMPLEMENTATION_SAFETY_SCAFFOLD_EXTERNAL_AUDIT_READ_ONLY`.

Product/runtime enablement remains blocked after that audit until a separate explicit manual GO.

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No runtime/product enablement or source/test behavior change added. |
| P1 | 0 | No service registration, command handler, UI product action, product ledger implementation or provider/cloud/network integration added. |
| P2 | 0 | No blocker in this design-only block; the next step is intentionally manual-GO blocked. |
| P3 | 5 | Product ledger policy, redaction product wiring, product runtime flag, authority wiring and replay/failure evidence remain future implementation work. |
| P4 | 1 | Percentages remain conservative planning estimates. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Durable Stage 2 test-only/local-temp | 92-96% |
| Product ledger path design | 60-70% |
| Redaction product wiring design | 55-65% |
| Runtime feature flag product-readiness design | 55-65% |
| Durable runtime enablement design | 70-78% |
| Durable runtime test-only scaffold | 35-45% |
| Runtime/product implementation | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |
