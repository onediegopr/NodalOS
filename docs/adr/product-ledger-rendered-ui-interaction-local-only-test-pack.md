# Product Ledger Rendered UI Interaction Local-Only Test Pack

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_RENDERED_UI_INTERACTION_LOCAL_ONLY_TEST_PACK_READY`

## Context

The Product Ledger operator surface has a canonical local/dev/read-only route model and a Development-only Pilot mapping for `/internal/product-ledger/operator-surface`. The next safe step is rendered UI/DOM interaction evidence without opening public UI, live browser automation, product command execution, writes, exports or release claims.

The repo does not currently carry `WebApplicationFactory`, ASP.NET `TestServer` or an equivalent in-process HTTP testing package in the Safety/Recipes test projects. This block therefore uses the allowed fallback: direct route render invocation through `ProductLedgerLocalDevRoutePreview.CreateDefaultLocalDevRequest()` and DOM/interaction contract assertions over the rendered HTML string.

## Decision

Add rendered route interaction coverage that verifies:

- canonical Product Ledger DOM anchors are present and non-empty;
- local-only, development-only and read-only markers are visible;
- blocked frontiers and safe next steps render;
- canonical action previews render as disabled/read-only/no-op;
- rendered HTML emits no form posts, scripts, external links, callbacks or executable controls;
- changed Core route/model source does not invoke command execution, append/write/export, Pilot `/run`, network, DB/migration or external automation APIs.

## Scope

Allowed and implemented:

- render-function DOM contract tests;
- rendered interaction/non-execution tests;
- route source safety scan tests;
- Recipes smoke coverage for canonical rendered DOM;
- QA/ADR/handoff/decision-log evidence packet.

Not implemented:

- real in-process HTTP response test;
- browser screenshot update;
- live Browser/CDP/WCU/OCR/Recipes execution;
- public route/UI/product action;
- write/export/append interaction.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- HTTP in-process route response testing remains future unless a local test host package is intentionally added.
- Current evidence is rendered-function DOM evidence, not browser pixel evidence.
- Canonical route still uses fixture-safe read-model status, not a live local ledger path read.

P4:

- Broad scans contain forbidden terms only as negative assertions, blocked-frontier labels and documentation limitations.
- Core build initially showed a transient parallel-build file lock; rerun after build-server shutdown passed cleanly.

## Readiness

- Product Ledger local-only core: 94-96%.
- Approval/Human Review: 87-92%.
- Evidence/Timeline/Audit Trail: 79-85%.
- Runtime/Command/Execution: 42-50%.
- UI/Operator Surface: 42-52%.
- Local-only internal product: 58-66%.
- Usable end-to-end local product: 30-40%.
- External/cloud: 0%.
- Release/commercial: 0%.

## Next Safe Block

Recommended next macro-block: `NODAL_OS_LOCAL_APPROVAL_TO_ACTION_READ_ONLY_PREVIEW_LOOP`.

It must remain local-only/read-only/preview-only unless a separate GO opens a larger frontier.
