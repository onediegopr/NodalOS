# Product Ledger Path Threat Model Design-Only

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_THREAT_MODEL_DESIGN_ONLY_READY`

## Scope

Docs-only threat model for a future Durable Audit Trail product ledger path.

This ADR does not implement a product ledger path, change `DurableAuditTrailAppendOnlyMinimal`, register services, add command handlers, add UI product actions, create DB/migrations, call provider/cloud/network, enable Browser/CDP/WCU/OCR/Recipes live execution, add KMS/WORM/cloud/external trust provider, or claim release/commercial readiness.

## Current State

Current implementation is safe only because Stage 2 remains test-only/local-temp:

- `DurableAuditTrailAppendOnlyMinimalPolicy.StorageRoot` is local-test scoped by default.
- `AppendStage2TestOnly` rejects product-looking storage roots via `IsProductLedgerPath`.
- The product ledger guard is a fragment heuristic, not a product storage design.
- Redaction-before-persistence is isolated Core/test-only.
- Runtime feature flag accepts only exact `enabled:test-only`.
- Checkpoint evidence is local-temp/caller-held only.

Therefore a product ledger path remains `0% / NO-GO`.

## Threat Model

| Threat | Risk | Required design control |
| --- | --- | --- |
| Product path accidentally points to temp/test fixture | Audit data lost or misleading. | Explicit environment-scoped product root policy; no temp fallback. |
| Product path points to repo/workspace/source tree | Source pollution, accidental commit, data leakage. | Repo/workspace exclusion and canonical path containment. |
| Product path points to user documents/downloads/desktop | Sensitive data leakage and user confusion. | Dedicated app data root, operator-visible path policy. |
| Product path points to network share/cloud sync folder | Custody ambiguity and sync race. | Network/cloud-sync disallow by default; future explicit policy only. |
| Symlink/junction/reparse escape | Writes outside approved boundary. | Canonicalization plus reparse-point and ancestry checks. |
| Case/Unicode/path separator bypass | Guard bypass on Windows paths. | Normalized canonical comparisons and path segment token checks. |
| Product path fragment heuristic false positive/negative | Wrong rejection or false acceptance. | Replace fragments with structured ledger root policy in future implementation. |
| Raw secret/PII path in evidence | Sensitive filesystem disclosure. | Redaction-before-persistence before path evidence and evidence-safe path labels. |
| Concurrent writers | Corruption or interleaving. | Single-writer policy, lock strategy and cross-process story before enablement. |
| Crash during append | Partial event or truncated ledger. | Atomic append/write-ahead or recovery design before enablement. |
| Tail deletion/replacement | Valid older ledger can hide newer events. | Checkpoint/read-model plus external trust decision before stronger claims. |
| Unauthorized runtime/handler write | Product action without authority. | Product service/command/UI authority gates before any writer is reachable. |

## Required Future Product Ledger Policy

A future product ledger policy must define:

- approved root category;
- canonical full path;
- path ownership and operator visibility;
- environment label;
- redaction-before-path-persistence requirement;
- workspace/repo exclusion;
- temp/downloads/documents/desktop exclusion unless explicitly audited;
- network/share/cloud-sync exclusion by default;
- symlink/junction/reparse handling;
- single-writer and cross-process policy;
- crash/recovery policy;
- retention/deletion/export policy alignment;
- checkpoint/trust boundary relationship;
- release/commercial prohibition until full audit.

## Required Negative Tests Before Any Implementation

Future tests must prove rejection of:

- empty path and whitespace path;
- temp root when product mode is requested;
- repo/workspace paths;
- user profile documents/downloads/desktop paths;
- path fragments that only look productive but are not policy-approved;
- symlink/junction/reparse escape;
- network share and UNC path;
- cloud-sync-looking roots;
- relative path traversal;
- mixed casing and separator variants;
- product path with missing redaction proof;
- product path with live Browser/CDP/WCU/OCR/Recipes metadata;
- product path with release/commercial flag;
- product path without explicit product runtime manual GO.

## Readiness Gates

| Gate | Status now | Required before product implementation |
| --- | --- | --- |
| Product ledger root policy | MISSING | Design + external audit + manual GO. |
| Product path canonicalization policy | MISSING | Design + hostile path tests. |
| Redaction-before-write product wiring | MISSING | Design + implementation + external audit. |
| Runtime feature flag product policy | MISSING | Design + implementation + manual GO. |
| Product service registration authority | MISSING | Design + manual GO. |
| Command/UI authority | MISSING | Design + manual GO. |
| Checkpoint/trust boundary | LOCAL_ONLY_TEST_ONLY | Product decision required before stronger claims. |
| DB/provider/cloud/network | PROHIBITED | Separate product/security decision required. |
| Release/commercial | NO-GO | Full release audit required. |

## Findings

| Severity | Count | Details |
| --- | ---: | --- |
| P0 | 0 | No product ledger implementation or runtime/product authority added. |
| P1 | 0 | No service registration, handler, UI action, DB/provider/cloud/network or KMS/WORM path added. |
| P2 | 0 | No immediate blocker in this design-only block. |
| P3 | 4 | Product ledger root policy, canonicalization/containment, crash/concurrency behavior and product redaction wiring remain blockers. |
| P4 | 1 | Current `IsProductLedgerPath` remains a useful test-only guard but not a product policy. |

## Percentages

| Track | Conservative status |
| --- | --- |
| Product ledger path threat model | 65-75% |
| Product ledger implementation | 0% / NO-GO |
| Product path canonicalization/containment | 0% / NO-GO |
| Product redaction wiring | 0% / NO-GO |
| Product runtime enablement | 0% / NO-GO |
| Release/commercial readiness | 0% / NO-GO |

## Next Safe Option

`NODAL_OS_RUNTIME_FEATURE_FLAG_PRODUCT_READINESS_DESIGN_ONLY`

This next option is safe only as design-only/no-code and must not implement product runtime flag behavior.
