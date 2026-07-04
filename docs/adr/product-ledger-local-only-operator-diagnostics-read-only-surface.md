# Product Ledger Local-Only Operator Diagnostics Read-Only Surface

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_ONLY_OPERATOR_DIAGNOSTICS_READ_ONLY_SURFACE_READY`

## Scope

This block adds a Core-only operator diagnostics presenter for the Product Ledger local-only runtime window.

The presenter is local-only, internal-only, read-only and fail-closed. It consumes already-computed runtime flag, active-path policy and runtime diagnostics results; it does not write ledger entries, register services, expose command handlers, call providers, open DB paths or enable release/commercial readiness.

## Implemented

- `ProductLedgerLocalOnlyOperatorDiagnosticsPresenter`.
- `ProductLedgerLocalOnlyOperatorDiagnosticsRequest`, `Result`, `Section` and `ActionPreview` models.
- Required read-only sections:
  - Runtime Local-Only Gate.
  - Product Ledger Path Policy.
  - Bounded Writer Status.
  - Checkpoint / Head Status.
  - Evidence Gates.
  - Disabled Actions.
  - Safe Next Step.
- Disabled action previews with no productive command id, handler name or callback name.
- Fail-closed rejection for missing runtime flag, unsafe activation result, unsafe diagnostics result, evidence gaps, stale/malformed evidence references and public/product/external/release claims.
- Safety and Recipes coverage for happy-path operator snapshot, default-off/enabled flag states, corrupt/unknown fail-closed behavior and no-enable static guards.

## Boundary

Allowed:

- local-only internal diagnostics;
- read-only operator status projection;
- fail-closed blockers and warnings;
- disabled action previews;
- static scan/property hardening;
- docs/QA/handoff/audit pack.

Not allowed and not implemented:

- public UI action;
- destructive user-facing action;
- productive command handler;
- productive DI registration;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- Browser/CDP/WCU/OCR/Recipes live execution;
- release/commercial readiness.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- The surface is an internal diagnostic projection only; public UI or user-facing action remains future-gated.
- Product command handlers and productive DI remain absent.
- External provider/cloud/network, DB and KMS/WORM trust remain future-gated.

P4:

- Checkpoint/head status inherits same-boundary local checkpoint limitations from the Product Ledger local-only writer.
- Action previews are labels/evidence lists only; there is no execution authority.

## Next Frontier

`PUBLIC_UI_OR_EXTERNAL_PROVIDER_DB_KMS_LIVE_AUTOMATION_RELEASE_OR_DESTRUCTIVE_USER_FACING_ACTION_REQUIRES_NEW_EXPLICIT_GO`
