# Product Ledger Browser Local-Only Screenshot Evidence Test-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_BROWSER_LOCAL_ONLY_SCREENSHOT_EVIDENCE_TEST_ONLY_READY`

## Context

The Product Ledger local-dev visual QA fixture exists as deterministic HTML. This block adds real local browser screenshot evidence in test-only mode without public deploy, public internet exposure, external network/provider/cloud, telemetry/sync, DB/migration, KMS/WORM/external trust, productive Browser/CDP, WCU/OCR/Recipes live, destructive action, unbounded export/write or release/commercial readiness.

## Decision

Generate a screenshot from the committed local HTML fixture using an installed local browser in headless mode with a temporary profile and network/telemetry reduction flags. The input URL was a local `file://` URL under the repo. No host was deployed, no external URL was opened, no credentials were used and the temporary browser profile was removed after capture.

## Evidence

- Screenshot: `docs/qa/nodal-os-product-ledger-browser-local-only-screenshot-evidence-test-only/product-ledger-local-dev-visual-qa.png`.
- SHA-256: `dfa67a2d279e878704db4a5916708dc195ce2b59e21a7893b4149d481a56d80e`.
- Size: 60109 bytes.
- Source fixture: `docs/qa/nodal-os-product-ledger-local-dev-visual-qa-screenshot-evidence/visual-snapshot.html`.
- Mode: browser local-only, test-only, not product Browser/CDP, not live automation.

## Findings

P0: 0

P1: 0

P2: 0

P3:

- Future pixel diff can compare this screenshot against a deterministic baseline if needed.

P4:

- Screenshot was generated from the static HTML fixture, not from a running product host.
- Local browser capture is test evidence, not product Browser/CDP or live automation.
