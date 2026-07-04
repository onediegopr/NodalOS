# Product Ledger Public Local-Only Manual QA Operator Acceptance

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PUBLIC_LOCAL_ONLY_MANUAL_QA_OPERATOR_ACCEPTANCE_READY`

## Context

The Product Ledger public local-only/non-destructive action surface exists as a Core-only surface mediated by `ProductLedgerInternalCommandPreviewRouter` and `ProductLedgerInternalCommandHandler`. This block validates that surface from Manual QA, operator acceptance and UX safety perspectives without adding destructive action, unbounded export/write, external/cloud export, provider/cloud/network, DB/migration, KMS/WORM/external trust, live automation or release/commercial readiness.

## Manual QA Acceptance Matrix

| Case | Expected result | Required evidence | Risk | Pass/fail criteria | Automated reference | Manual verification |
| --- | --- | --- | --- | --- | --- | --- |
| `ViewDiagnostics` | Completes local-only/non-destructive in memory. | public disabled preview, router preview, handler result. | Low. | PASS if command result is read-only/in-memory and no external flags. | `ProductLedgerPublicLocalOnlyOperatorAcceptanceTests.OperatorAcceptance_FixtureOnlyWalkthrough...` | Confirm label says diagnostics and risk is local-only/non-destructive. |
| `ViewLedgerReadiness` | Completes local-only/non-destructive in memory. | readiness evidence, router preview, handler result. | Low. | PASS if no writer execution or release claim. | Same fixture walkthrough. | Confirm readiness is not represented as release-ready. |
| `ViewRuntimeGateStatus` | Completes local-only/non-destructive in memory. | runtime local-only gate evidence. | Low. | PASS if no runtime external enablement is implied. | Same fixture walkthrough. | Confirm default-off/local-only wording. |
| `ViewCheckpointHeadStatus` | Completes local-only/non-destructive in memory. | checkpoint/head evidence and same-boundary limitation. | Medium. | PASS if local trust limitation remains visible. | Same fixture walkthrough. | Confirm no WORM/KMS/external trust claim. |
| `ViewEvidenceGates` | Completes local-only/non-destructive in memory. | redaction, retention, authority, replay/rollback evidence. | Medium. | PASS if evidence is required and no raw payload shown. | Same fixture walkthrough. | Confirm raw/secret display is absent. |
| `StaticScanPreview` | Completes in memory, no scanner process launched. | static scan packet. | Low. | PASS if no external process/network. | Same fixture walkthrough. | Confirm it is a preview only. |
| `RequestExternalAuditPreview` | Completes in memory, no external model/service call. | read-only audit packet. | Low. | PASS if no external call/credential. | Same fixture walkthrough. | Confirm request is preview-only. |
| `LocalReportPhysicalExportBoundedInternal` | Writes only bounded local report and verifies post-write hash. | canonical boundary, safe content, redaction evidence, hash. | Medium. | PASS if file path stays under allowed root and hash matches content. | Same fixture walkthrough. | Confirm bounded local warning is present. |
| dangerous actions disabled | Dangerous buttons are disabled/blocked. | disabled button state, risk label, required evidence. | High if enabled. | PASS if destructive/external/release buttons are disabled. | `OperatorAcceptance_UxSafetyReview...` | Confirm disabled reason is visible. |
| malformed command | Fails closed. | blocker evidence. | Medium. | PASS if no handler invocation. | Public action surface Safety tests. | Confirm operator sees blocked/fail-closed state. |
| unknown command | Fails closed. | blocker evidence. | Medium. | PASS if no handler invocation. | Public action surface Safety tests. | Confirm no fallback execution. |
| stale evidence | Fails closed through unsafe preview/readiness packet. | stale/missing readiness blocker. | Medium. | PASS if stale packet rejects. | Public disabled preview Safety tests. | Confirm stale UI cannot execute. |
| unsafe export content | Fails closed through bounded export service. | unsafe content blocker. | High. | PASS if no file write outside allowed safe path. | Public action corpus hardening test. | Confirm raw/secret content is blocked. |
| external/cloud attempt | Fails closed. | external/cloud blocker. | High. | PASS if no provider/network/export. | Operator acceptance UX test. | Confirm external/cloud wording is blocked. |
| release/commercial attempt | Fails closed. | release/commercial blocker. | High. | PASS if no release/commercial readiness. | Operator acceptance UX test. | Confirm not release-ready and not commercial-ready. |

## Fixture-Only QA Execution

The fixture-only execution is implemented in `ProductLedgerPublicLocalOnlyOperatorAcceptanceTests`:

- allowed actions complete only local-only/non-destructive;
- router and handler mediation are asserted;
- bounded local export writes only under a temporary local test root;
- post-write hash is verified;
- dangerous buttons are disabled;
- destructive, unbounded export, external/cloud, provider/cloud/network, DB/migration, KMS/WORM/external trust, live automation, release/commercial, telemetry/sync and billing/licensing cloud attempts reject.

The fixture does not contact network, DB, provider, browser/CDP, WCU/OCR, Recipes live, credentials or external audit services.

## UX Safety Review

Required UX states:

- labels use action-specific names;
- safe actions show `local-only non-destructive`;
- dangerous actions show `blocked dangerous action`;
- disabled states are visible for destructive, unbounded export, external/cloud, DB/migration, KMS/WORM/external trust, live automation and release/commercial actions;
- required evidence is listed for every action;
- bounded export warning remains explicit;
- checkpoint/head status keeps same-boundary local trust limitation;
- copy states not release-ready, not commercial-ready, not WORM/KMS/cloud and not external trust;
- bounded export evidence is not compliance-grade custody.

## Operator Acceptance Packet

Safe to use now:

- local-only/non-destructive read actions;
- bounded local diagnostic report export with safe content and hash verification.

Still blocked:

- destructive user-facing action;
- unbounded physical export/write;
- external/cloud export;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- live Browser/CDP/WCU/OCR/Recipes;
- external telemetry/sync;
- billing/licensing cloud;
- release/commercial.

Operator must check:

- local-only status;
- non-destructive status;
- router/handler mediation;
- bounded export path;
- post-write hash;
- redaction-before-export evidence;
- no raw payload/secret display;
- no release/commercial or WORM/KMS/cloud/external trust wording.

## Decision

`GO_WITH_FINDINGS_PRODUCT_LEDGER_PUBLIC_LOCAL_ONLY_MANUAL_QA_OPERATOR_ACCEPTANCE_READY`.
