# Product Ledger Local Approval-To-Action Read-Only Preview Loop

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_APPROVAL_TO_ACTION_READ_ONLY_PREVIEW_LOOP_READY`

## Context

The Product Ledger local-only line already has a canonical operator surface, local/dev route preview, rendered DOM interaction coverage, disabled/read-only/no-op action preview controls, Product Ledger active writer hardening, redaction/retention gates, concurrency evidence, command/router preview-only behavior and Pilot `/run` default-blocked guards.

The next safe step is a coherent approval-to-action preview loop that lets the operator surface show what would need approval, what candidate action is under consideration, what policy gate blocks it, what no-op result is produced and which evidence/readiness references support the next step.

## Decision

Add a canonical `ProductLedgerLocalApprovalPreviewLoop` model and render it on the Product Ledger operator surface route.

The loop is local-only, read-only and preview-only:

- `AllowsExecution = false`
- `AllowsWrite = false`
- `AllowsExport = false`
- `AllowsNetwork = false`
- `AllowsDb = false`
- `AllowsReleaseCommercial = false`

The preview loop uses `ProductLedgerInternalCommandPreviewRouter` for a `ViewLedgerReadiness` candidate action preview. It does not call the productive command handler, writer, export service, Pilot execution gate, Browser/CDP/WCU/OCR/Recipes live surfaces, network/cloud, DB/migration or external trust systems.

## Scope

Implemented:

- approval preview DTO;
- candidate action preview DTO;
- policy/gate preview DTO;
- no-op execution preview DTO;
- evidence-link DTOs;
- operator-surface route section with stable DOM anchors;
- disabled/read-only/no-op preview control;
- Safety and Recipes tests for non-execution guarantees;
- QA report, handoff, roadmap/readiness note and decision-log evidence.

Not implemented:

- real approval execution;
- real product command execution;
- append/write/export from the route or approval loop;
- public UI action;
- public deploy;
- HTTP in-process route response test infrastructure;
- live local ledger path read-model testing;
- Browser/CDP/WCU/OCR/Recipes live execution;
- release or commercial readiness.

## Boundary Confirmation

This block does not approve real execution. It does not execute product commands, append to the Product Ledger, write files, export reports, enable Pilot `/run`, open a browser, call external providers, create DB migrations, use KMS/WORM/external trust, expose public UI actions or claim release/commercial readiness.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- HTTP in-process route response testing remains future because current tests still use render-function evidence.
- The route still uses fixture-safe canonical read model state, not arbitrary live local ledger path scanning.
- A future block can connect local route response testing to the same preview loop without enabling execution.

P4:

- The loop is intentionally conservative: policy decision remains `NeedsHumanReviewPreview` and the rendered control is disabled.
- Evidence links are readiness references, not compliance custody evidence.

TRUE_RISK: 0

## Readiness

- Product Ledger local-only core: 94-96%.
- Approval/Human Review: 90-94%.
- Evidence/Timeline/Audit Trail: 80-86%.
- Runtime/Command/Execution: 45-53%.
- UI/Operator Surface: 48-58%.
- Local-only internal product: 61-69%.
- Usable end-to-end local product: 34-44%.
- External/cloud: 0%.
- Release/commercial: 0%.

## Next Safe Block

Recommended next macro-block: `NODAL_OS_HTTP_IN_PROCESS_ROUTE_RESPONSE_TEST_INFRASTRUCTURE_LOCAL_ONLY`.

It must remain local-only/test-only/no-runtime-enable/no-product-enable and must not introduce public deployment, external network/provider/cloud, DB/migration, KMS/WORM/external trust, Browser/CDP/WCU/OCR/Recipes live execution or release/commercial readiness.
