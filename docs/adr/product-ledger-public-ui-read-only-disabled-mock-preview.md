# Product Ledger Public UI Read-Only Disabled Mock Preview

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PUBLIC_UI_READ_ONLY_DISABLED_MOCK_PREVIEW_SAFE_WINDOW_READY`

## Context

The public surface readiness map authorized a safe next step: a public UI read-only mock/preview with all actions disabled. This block implements that preview as a Core presenter and test fixture only. It does not add a public UI route, public/product command handler exposure, destructive execution, provider/cloud/network, DB/migration, KMS/WORM/external trust, live automation or release/commercial readiness.

The preview consumes the existing internal operator UI preview and only renders when a fresh public surface readiness packet is explicitly present.

## Implemented Surface

- `ProductLedgerPublicUiReadOnlyDisabledPreviewPresenter`.
- `ProductLedgerPublicUiReadOnlyDisabledPreviewRequest`.
- `ProductLedgerPublicUiReadOnlyDisabledPreviewResult`.
- `ProductLedgerPublicUiReadOnlyDisabledPreviewViewModel`.
- Disabled public mock action descriptors.
- Safety and Recipes tests for fail-closed and disabled-action invariants.

## Safety Contract

The presenter renders only when:

- explicit read-only disabled mock scope is present;
- internal operator UI preview rendered successfully;
- internal preview is read-only and fail-closed;
- internal preview has no blockers;
- all internal action previews are disabled;
- no productive command id, handler id or callback is present;
- public surface readiness packet exists;
- readiness packet is fresh and consistent;
- no public action, destructive action, product command handler, service registration, provider/cloud/network, DB/migration, KMS/WORM/external trust, live automation, external/cloud export, unbounded physical export, raw payload/secret or release/commercial claim is requested.

Any violation rejects the render and returns a fail-closed disabled fallback model.

## Explicit Non-Capabilities

- No public UI action.
- No public/product command handler exposure.
- No destructive action execution.
- No productive DI/service registration.
- No endpoint/controller/route mapping.
- No physical write/export authority.
- No external/cloud export.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust.
- No Browser/CDP/WCU/OCR/Recipes live execution.
- No external telemetry/sync.
- No billing/licensing cloud.
- No raw payload/secret display.
- No release/commercial readiness.

## Readiness

- Public UI read-only disabled mock preview: 100%.
- Public UI action readiness: 0%.
- Public/product command handler exposure: 0%.
- Destructive action readiness: 0%.
- External/cloud export: 0%.
- Provider/cloud/network: 0%.
- DB/migration: 0%.
- KMS/WORM/external trust: 0%.
- Browser/CDP/WCU/OCR/Recipes live: 0%.
- Release/commercial: 0%.

## Stop Packet

`PUBLIC_UI_ACTION_OR_PRODUCT_COMMAND_HANDLER_EXPOSURE_REQUIRES_NEW_EXPLICIT_GO`

Allowed before that GO:

- read-only audit;
- test/property/corpus hardening;
- static scan hardening;
- docs/ADR/QA/handoff updates;
- public command/action exposure test-plan-only.

Requires new explicit GO:

- public UI action real;
- public/product command handler exposure;
- destructive user-facing action;
- unbounded physical export/write;
- external/cloud export;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- live Browser/CDP/WCU/OCR/Recipes;
- release/commercial readiness.

## Decision

`GO_WITH_FINDINGS_PRODUCT_LEDGER_PUBLIC_UI_READ_ONLY_DISABLED_MOCK_PREVIEW_SAFE_WINDOW_READY`.
