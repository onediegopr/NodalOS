# Product Ledger Browser Local-Only DOM/Snapshot Visual Diff Test-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_BROWSER_LOCAL_ONLY_DOM_SNAPSHOT_VISUAL_DIFF_TEST_ONLY_READY`

## Context

The previous local-only screenshot packet produced a real PNG from the committed static fixture. This follow-up adds a safe test-only visual contract without enabling product Browser/CDP, a public route, telemetry, network, DB, KMS/WORM or release/commercial readiness.

## Scope

- Verify the screenshot artifact is a PNG with expected local capture viewport dimensions.
- Verify required PNG chunks are present.
- Verify the source DOM fixture keeps required visible sections for header, runtime gate, writer, bounded export, evidence gates, disabled dangerous actions and safe next step.
- Verify dangerous action affordances remain non-executable and disabled.
- Verify the fixture has no scripts, external source attributes, links, click handlers or form actions.

## Non-Goals

- No pixel/OCR comparison.
- No public deploy.
- No external network/provider/cloud.
- No telemetry/sync.
- No DB/migration.
- No KMS/WORM/external trust.
- No product Browser/CDP or live automation.
- No WCU/OCR/Recipes live execution.
- No destructive user-facing action.
- No unbounded export/write.
- No release/commercial readiness.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Future pixel-level diff can remain safe if it runs only against local fixture artifacts and preserves the no-external/no-product-browser boundary.

P4:

- DOM/snapshot checks are deterministic contract evidence, not human visual approval or compliance-grade custody.
