# Product Ledger Local Route Live Ledger Read-Model Test-Safe

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_ROUTE_LIVE_LEDGER_READ_MODEL_TEST_SAFE_READY`

## Context

The Product Ledger operator route already had fixture-safe render evidence, an approval-to-action preview loop and HTTP loopback route response tests. The remaining gap was route evidence against a controlled live ledger read model without introducing arbitrary filesystem input or write/export/command authority.

## Decision

Add `ProductLedgerOperatorSurfaceReadModelProvider` with two explicit modes:

- `FixtureSafeReadModel`: the existing default, with no ledger file read.
- `TestSafeLiveLedgerReadModel`: a test-safe read-only source injected by tests from an already activated local-only ledger.

The route mapper accepts an explicit `ProductLedgerOperatorSurfaceReadModelSource` overload for tests. The HTTP request does not carry a path, does not bind query params and does not scan the filesystem. If no explicit source is provided, the route falls back to `FixtureSafeReadModel`.

## Scope

Implemented:

- explicit read-model source and provider;
- fixture-safe fallback;
- test-safe live ledger snapshot from `ProductLedgerPathLocalOnlyActiveWriter.ReadVerified`;
- route DOM fields for read-model mode, redacted path classification, entry count, head sequence and hash prefixes;
- HTTP Development 200 tests for fixture-safe and test-safe live ledger;
- Production 404 test;
- arbitrary path query ignored/not leaked test;
- read-only/no-mutation assertions over ledger and checkpoint file hashes;
- Safety static source guards;
- QA/ADR/handoff/roadmap/decision-log evidence.

Not implemented:

- arbitrary path input;
- directory scanning;
- route append/write/export;
- product command execution;
- public UI actions;
- public deploy;
- external network/provider/cloud;
- DB/migration;
- KMS/WORM/external trust;
- Browser/CDP/WCU/OCR/Recipes live execution;
- Pilot `/run`;
- release/commercial readiness.

## Boundary Confirmation

The live ledger is created and appended only by tests before the route starts. The route receives only an explicit in-memory source and performs read/verify only. Ledger paths are classified/redacted in rendered output and are not accepted from HTTP input.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Live read-model remains test-safe and injected; it is not a user-facing product ledger selector.
- Future approval execution remains design-only unless separately authorized.
- Browser pixel evidence remains separate from route/read-model proof.

P4:

- Hashes are rendered as prefixes only.
- `HttpClient` appears only in Recipes loopback test-only code.

TRUE_RISK: 0

## Readiness

- Product Ledger local-only core: 94-96%.
- Approval/Human Review: 90-94%.
- Evidence/Timeline/Audit Trail: 84-90%.
- Runtime/Command/Execution: 46-55%.
- UI/Operator Surface: 55-65%.
- Local-only internal product: 65-73%.
- Usable end-to-end local product: 40-50%.
- External/cloud: 0%.
- Release/commercial: 0%.

## Next Safe Block

Recommended next macro-block: `NODAL_OS_LOCAL_APPROVAL_EXECUTION_DESIGN_ONLY_BOUNDARY`.

It must remain design-only/read-only unless a separate GO authorizes bounded local execution.
