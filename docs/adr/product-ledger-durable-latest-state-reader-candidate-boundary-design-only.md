# Product Ledger Durable Latest State Reader Candidate Boundary Design-Only

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_DURABLE_LATEST_STATE_READER_CANDIDATE_BOUNDARY_DESIGN_ONLY_READY`

Baseline HEAD: `2caa0aaf641b4626c93f54663178664458b837cc`

## Scope

This block designs a future local-only/internal-only/Development-only durable latest-state reader candidate.

It does not implement:

- active durable reader code;
- read precedence;
- product read-model authority;
- public/product route;
- Production route;
- runtime enablement;
- service registration;
- command handler;
- UI product action;
- DB/provider/cloud/network;
- KMS/WORM/external trust;
- release/commercial readiness.

## Current Inputs

Existing evidence chain:

- Latest-state snapshots: `docs/test-output/product-ledger/operator-surface-latest-state-snapshots/`.
- Latest-state manifests: `docs/test-output/product-ledger/operator-surface-latest-state-manifests/`.
- Manifest classification: `LOCAL_INTERNAL_DEV_ONLY_VERSIONED_MANIFEST_NOT_AUTHORITY`.
- Manifest stale policy: `MANIFESTS_ARE_HISTORICAL_INDEX_EVIDENCE_ONLY_NOT_LIVE_PRODUCT_AUTHORITY`.

## Future Candidate Contract

Future candidate name:

`LocalDurableLatestStateReaderCandidateNotAuthority`

The candidate may be considered only if it remains:

- local-only;
- internal-only;
- Development-only;
- diagnostic/readiness-only;
- default-off;
- fail-closed;
- no read precedence;
- no product authority;
- no public/product path;
- no Production route.

The candidate must read only explicitly supplied, already validated manifest/snapshot evidence. It must not scan directories, infer latest files, choose `latest.json`, accept user-selected paths, follow payload-controlled roots or create mutable pointers.

## Required Validation Rules

Future implementation must fail closed when:

- manifest is missing;
- manifest payload is corrupt or incomplete;
- manifest claims authority/product/compliance/cloud-backed durability;
- manifest requests latest pointer/read precedence;
- manifest safe relative path escapes the allowed boundary;
- manifest points to a missing snapshot;
- source snapshot hash or checkpoint does not match the manifest entry;
- source snapshot is not historical evidence only;
- source snapshot claims live/product authority;
- reparse/symlink/junction validation is unresolved;
- redaction-before-persistence evidence is absent;
- any input contains raw secrets, tokens, provider URLs, DB migration strings, shell/subprocess strings or absolute sensitive paths.

## Test Plan

Future tests must cover:

- happy-path diagnostic candidate from a valid manifest and valid source snapshot;
- corrupt manifest JSON;
- incomplete manifest collections;
- manifest authority/product/read-precedence/latest-pointer claims;
- missing snapshot file;
- snapshot hash mismatch;
- snapshot checkpoint mismatch;
- unsafe relative path/traversal/Windows-drive/UNC variants;
- reparse point rejection;
- stale manifest visibility without authority;
- no directory scan;
- no mutable latest pointer;
- no public/product/Production route;
- no DI product registration;
- no command handler;
- no UI product action;
- no provider/cloud/network/DB/KMS/WORM/live automation/release/commercial.

## Decision

The next implementation, if authorized, should produce a diagnostic candidate result only. It must not make the durable candidate the operator surface read source and must not alter read precedence.

## Findings

- P0: 0.
- P1: 0.
- P2: 0.
- P3: future reader candidate would introduce durable local reads from test-output evidence and therefore needs strict no-authority/no-precedence guards.
- P4: candidate state can be stale and must be displayed as evidence only.

## Next Frontier

The next step is implementation of a local/internal/Development-only reader candidate. That is no longer just docs/design; it must receive explicit GO if it will add reader code or route behavior.
