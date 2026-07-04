# Product Ledger Local Dev Visual QA Screenshot Evidence

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_LOCAL_DEV_VISUAL_QA_SCREENSHOT_EVIDENCE_READY`

## Context

The Product Ledger operator surface has a Development-only local route and a deterministic HTML snapshot. This block adds local-only visual QA evidence without public deploy, external network, telemetry, provider/cloud, DB/migration, KMS/WORM/external trust, productive Browser/CDP, WCU/OCR/Recipes live execution, destructive action, unbounded export/write or release/commercial readiness.

## Decision

Use fixture-only static HTML evidence instead of a real browser screenshot. The environment can validate the rendered surface through deterministic HTML and DOM assertions without starting a browser/CDP session or creating external traffic.

Add `ProductLedgerLocalDevVisualQaEvidence` to evaluate:

- local-only and Development-only fixture scope;
- renderable local-dev route output;
- visible sections;
- disabled dangerous action affordances;
- required local-only/internal/dev notices;
- absence of active release/commercial, WORM/KMS/external trust, provider/cloud/network, DB, telemetry, live automation, destructive action and unbounded export/write claims.

## Artifact Policy

The committed visual artifact is `docs/qa/nodal-os-product-ledger-local-dev-visual-qa-screenshot-evidence/visual-snapshot.html`.

It is:

- local-only;
- fixture-only;
- static HTML;
- not deployed;
- no telemetry;
- no external network;
- no external script;
- no release/commercial;
- no external trust;
- no WORM/KMS/cloud;
- not compliance-grade custody.

## Readiness

- Local dev route: 100%.
- Visual QA evidence: 100%.
- Renderable snapshot: 100%.
- DOM contract: 100%.
- Product Ledger public local-only actions: 76%.
- Operator acceptance: 82%.
- External/cloud readiness: 0%.
- Release/commercial: 0%.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Real screenshot evidence can be added later only with a local-only non-productive browser path and a separate safety review.
- Visual diff can be expanded with a fixture-only renderer if needed.

P4:

- Evidence is static HTML fixture evidence, not a pixel screenshot.
- No live browser/CDP was used.
- Local fixture evidence is not WORM/compliance-grade custody.
