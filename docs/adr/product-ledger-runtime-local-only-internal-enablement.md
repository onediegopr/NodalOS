# Product Ledger Runtime Local-Only Internal Enablement

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_RUNTIME_LOCAL_ONLY_INTERNAL_ENABLEMENT_READY`

## Scope

This block implements the authorized runtime local-only internal enablement window for the Product Ledger Path.

The implementation is Core-only, local-only, internal-only and fail-closed. It introduces a default-off feature flag gate, internal readiness/diagnostics surface and a test-only internal adapter. The adapter can read verified diagnostics or append a safe hash-only entry only by delegating to the already validated bounded local-only writer.

## Implemented

- `ProductLedgerRuntimeLocalOnlyInternalEnablement`.
- Default-off feature flag: `off`.
- Explicit local-only internal enablement value: `enabled:local-only-internal`.
- Internal service wiring readiness booleans without productive DI registration.
- Internal command adapter test-only readiness booleans without public command handlers.
- Internal read-only diagnostics/product-surface preview.
- Bounded append/read delegation through `ProductLedgerPathLocalOnlyActiveWriter`.
- Fail-closed rejection for unsupported internal command kinds and forged feature flag results missing diagnostics/read-only permissions.
- Safety and Recipes tests for default-off behavior, local-only arming, diagnostics, append delegation and no-enable boundaries.

## Boundary

Allowed:

- runtime feature flag local-only and default-off;
- internal-only readiness/diagnostics surface;
- internal command adapter test-only/local-only;
- Product Ledger local-only integration behind policy;
- append/read verification through the bounded writer only.

Not allowed and not implemented:

- public UI action;
- command handler exposed to users;
- runtime enabled by default;
- destructive action outside the bounded local-only writer;
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

- Runtime remains internal-only and requires the explicit local-only flag value.
- Public UI, productive command handlers and productive DI remain future-gated.
- External provider/cloud/network, DB and KMS/WORM trust remain future-gated.

P4:

- Diagnostics are local policy/readiness evidence, not user-facing product authority.
- Append/read verification inherits same-boundary local checkpoint limitations from the bounded writer.

## Next Safe Block

`NODAL_OS_PRODUCT_LEDGER_RUNTIME_LOCAL_ONLY_INTERNAL_ENABLEMENT_EXTERNAL_AUDIT_READ_ONLY`
