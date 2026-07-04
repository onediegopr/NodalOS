# Product Ledger Internal Operator UI Read-Only Preview

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_INTERNAL_OPERATOR_UI_READ_ONLY_PREVIEW_READY`

## Scope

This block adds an internal-only, local-only and read-only operator UI preview view-model for Product Ledger local-only state. The preview consumes `ProductLedgerLocalOnlyOperatorDiagnosticsResult` and projects it as cockpit-style header, sections, notices and disabled action previews.

## Implemented

- `ProductLedgerInternalOperatorUiPresenter`.
- `ProductLedgerInternalOperatorUiPreviewRequest`.
- `ProductLedgerInternalOperatorUiPreviewResult`.
- `ProductLedgerInternalOperatorUiPreviewViewModel`.
- Header with Product Ledger Local-Only status, readiness percentage, local-only/internal-only/read-only notice and no release/commercial notice.
- Sections for runtime gate, Product Ledger path policy, bounded writer, checkpoint/head, evidence gates, disabled actions and safe next step.
- Disabled action previews for public UI, destructive action, command handler, provider/cloud, DB migration, KMS/WORM, Browser/CDP/WCU/OCR/Recipes live and release/commercial.
- Fail-closed behavior for missing diagnostics, unsafe diagnostics, missing required sections and public/product/external/release/telemetry/billing claims.

## Boundary

Allowed:

- internal-only UI preview model;
- local-only read-only cockpit DTO;
- diagnostics/readiness binding;
- disabled buttons/action previews without handler, command id or callback;
- safety/recipe tests, static scan hardening and docs.

Not allowed and not implemented:

- public UI action;
- destructive user-facing action;
- command handler productivo;
- productive DI/service registration;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- Browser/CDP/WCU/OCR/Recipes live;
- release/commercial;
- external telemetry/sync;
- billing/licensing cloud.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Mounting into a public UI remains future-gated.
- Productive command handlers and productive service registration remain absent.
- External provider/cloud/network, DB/migration and KMS/WORM/external trust remain future-gated.

P4:

- Readiness percentage is preview/operator status only, not release readiness.
- Checkpoint/head notice remains same-boundary local trust only.

## Next Frontier

`PUBLIC_UI_OR_EXTERNAL_PROVIDER_DB_KMS_LIVE_AUTOMATION_RELEASE_OR_DESTRUCTIVE_USER_FACING_ACTION_REQUIRES_NEW_EXPLICIT_GO`
