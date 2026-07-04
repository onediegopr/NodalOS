# Product Ledger Public UI Actions Command Handler Local-Only Non-Destructive

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PUBLIC_UI_ACTIONS_COMMAND_HANDLER_LOCAL_ONLY_NON_DESTRUCTIVE_READY`

## Context

The public read-only disabled mock preview and public command/action test plan established the boundary for safe public-facing actions. This block implements a Core-only public action surface for local-only, non-destructive Product Ledger actions. It does not add endpoint/controller/route mapping, provider/cloud/network, DB/migration, KMS/WORM/external trust, live automation, telemetry/sync/billing cloud or release/commercial readiness.

The surface delegates allowed actions through the existing `ProductLedgerInternalCommandPreviewRouter` and `ProductLedgerInternalCommandHandler`. Bounded local report export remains isolated in the already validated `ProductLedgerLocalReportExportService`.

## Implemented

- `ProductLedgerPublicUiActionSurface`.
- `ProductLedgerPublicUiActionRequest`.
- `ProductLedgerPublicUiActionResult`.
- `ProductLedgerPublicUiActionDecision`.
- `ProductLedgerPublicUiActionBlocker`.
- local-only/non-destructive action button model with dangerous actions rendered blocked/disabled.
- Safety and Recipes tests.

## Allowed Actions

- `ViewDiagnostics`.
- `ViewLedgerReadiness`.
- `ViewRuntimeGateStatus`.
- `ViewCheckpointHeadStatus`.
- `ViewEvidenceGates`.
- `StaticScanPreview`.
- `RequestExternalAuditPreview`.
- `LocalReportPhysicalExportBoundedInternal`.

The bounded export action is allowed only through the existing bounded local export service and requires canonical boundary evidence, safe content, redaction-before-export evidence and post-write hash verification.

## Blocked Actions And Claims

- `EnablePublicUi`.
- `ExecuteAction`.
- `DestructiveWrite`.
- `RegisterCommandHandler`.
- `RegisterProductDI`.
- `ConnectProvider`.
- `EnableCloud`.
- `RunMigration`.
- `EnableKms`.
- `EnableWorm`.
- `EnableExternalTrust`.
- `RunBrowserCdp`.
- `RunWcu`.
- `RunOcr`.
- `RunRecipesLive`.
- `Release`.
- `CommercialLaunch`.
- `SyncExternal`.
- `TelemetryExternal`.
- `BillingLicensingCloud`.
- `UnboundedExport`.
- `ExternalExport`.
- `Delete`.
- `OverwriteUnsafe`.
- raw payload/secret claims.

## Safety Contract

The public action surface renders and completes only when:

- explicit public local-only/non-destructive scope is present;
- the public read-only disabled mock preview rendered safely;
- the action is one of the allowed local-only actions;
- the action maps to an allowed internal router command;
- the internal router preview is accepted;
- the internal command handler completes;
- bounded export, when requested, is canonical, local-only, safe-content, redacted and hash verified.

Every rejection is fail-closed. Rejected results do not return router or handler execution details and do not echo raw unsafe payload.

## Explicit Non-Capabilities

- No destructive user-facing action.
- No unbounded physical export/write.
- No external/cloud export.
- No provider/cloud/network.
- No DB/migration.
- No KMS/WORM/external trust.
- No Browser/CDP/WCU/OCR/Recipes live execution.
- No endpoint/controller/route mapping.
- No productive DI/service registration.
- No external telemetry/sync.
- No billing/licensing cloud.
- No release/commercial readiness.

## Readiness

- Product Ledger public local-only actions: 72%.
- Product command handler local-only/non-destructive mediation: 72%.
- Bounded local export: 100%.
- Public UI safety: 78%.
- External/cloud readiness: 0%.
- DB/migration: 0%.
- KMS/WORM/external trust: 0%.
- Live automation: 0%.
- Release/commercial: 0%.

## Decision

`GO_WITH_FINDINGS_PRODUCT_LEDGER_PUBLIC_UI_ACTIONS_COMMAND_HANDLER_LOCAL_ONLY_NON_DESTRUCTIVE_READY`.
