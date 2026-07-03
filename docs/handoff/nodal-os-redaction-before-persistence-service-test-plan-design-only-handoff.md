# NODAL OS - Redaction Before Persistence Service Test Plan Design Handoff

Decision: `GO_WITH_FINDINGS_REDACTION_BEFORE_PERSISTENCE_SERVICE_TEST_PLAN_DESIGN_ONLY_READY`

Date: 2026-07-03

## Result

The pre-implementation test plan is designed as documentation only. It defines future corpus classes, RBP-T0 through RBP-T19 gates, evidence schema requirements, forbidden evidence content and static scan requirements.

## Non-Goals Preserved

No source code, tests, runtime wiring, service registration, command handlers, UI product actions, product ledger paths, DB/cloud/network, Browser/CDP/WCU/OCR/Recipes live execution, release/commercial readiness or stash state changed.

## Carry Forward

- Version `redaction-before-persistence-corpus.v1` or an equivalent corpus before implementation.
- Implement future tests before service code in any authorized implementation macro-block.
- Keep append blocked for missing, failed, stale, after-the-fact or candidate-hash-mismatched evidence.
- Keep logs/errors free of raw rejected values.
- Run static no-registration/no-runtime/no-product scans in any implementation block.

## Stop Point

`PAUSE_FOR_MANUAL_GO_REDACTION_BEFORE_PERSISTENCE_SERVICE_IMPLEMENTATION_OR_TESTS`

The next material step is adding tests or implementing the service. That requires explicit manual GO.
