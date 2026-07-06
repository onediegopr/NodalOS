# Product Ledger Local Operator Surface Latest State Snapshot Implementation

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_LOCAL_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_IMPLEMENTATION_READY`

Baseline HEAD: `8ca6cdbc72a8f0170d336c4b263981e87d8cf9b1`

## Scope

This window implements `LocalOperatorSurfaceLatestStateSnapshotCreateOnly` as a local-only, internal-only, Development-only historical evidence snapshot path.

Implemented boundary:

`docs/test-output/product-ledger/operator-surface-latest-state-snapshots/`

Implemented route:

`POST /internal/product-ledger/operator-surface/create-latest-state-snapshot`

Implemented state route:

`GET /internal/product-ledger/operator-surface/latest-state-snapshot-state`

Production remains 404 because the Product Ledger local dev route mapper returns without mapping these endpoints outside `environment.IsDevelopment()`.

## Implemented Behavior

- Snapshot files are `.json` only.
- Filenames are immutable and versioned from snapshot id plus operator surface hash prefix.
- Writes use create-only semantics and do not overwrite existing files.
- No latest pointer file is written or overwritten.
- Payload path, root, filename, command, url, provider and DB migration fields are rejected fail-closed.
- User-selected paths, payload-controlled roots, filesystem scan claims, overwrite claims, latest pointer overwrite claims and public/product claims are rejected fail-closed.
- Reparse/symlink/junction escape checks are fail-closed for the workspace root, `docs`, `docs/test-output`, `docs/test-output/product-ledger` and final output directory.
- Redaction-before-persistence is required before a snapshot can be created.
- The source approval/no-op/bounded/local handoff/workspace test-jail/user workspace allowlisted chain must be completed.
- The operator surface model hash must match exactly before append.
- Existing identical safe snapshot payloads replay idempotently; corrupt or conflicting existing files are rejected.
- The operator surface shows latest snapshot state, safe relative path, hashes, stale-state classification, blockers, evidence refs and negative flags.

## Snapshot Content

The JSON payload contains safe metadata only:

- snapshot id and action id;
- created-at timestamp;
- `LOCAL_INTERNAL_DEV_ONLY_HISTORICAL_SNAPSHOT`;
- stale-state classification: `STALE_SNAPSHOTS_ARE_HISTORICAL_EVIDENCE_ONLY_NOT_LIVE_PRODUCT_AUTHORITY`;
- source chain ids and content hash prefixes;
- safe relative paths only;
- evidence refs;
- redacted chain summaries;
- negative flags;
- operator surface model hash;
- snapshot content hash;
- checkpoint hash;
- safe relative snapshot path.

The snapshot must not contain secrets, API keys, tokens, env values, provider credentials, raw unredacted payloads, raw absolute sensitive paths, business signoff claims, release/commercial claims or compliance custody claims.

## Explicit Non-Goals

This window does not implement or enable:

- public/product path;
- Production route;
- broader workspace action;
- edit/update/delete;
- user-selected path;
- latest pointer overwrite;
- shell/subprocess;
- command execution;
- Pilot `/run`;
- Browser/CDP/WCU/OCR/Recipes live authority;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- release/commercial readiness;
- compliance custody.

## Findings

- P0: 0.
- P1: 0.
- P2: 0.
- P3: a new local file write exists, but only under the fixed `docs/test-output/product-ledger/operator-surface-latest-state-snapshots/` boundary, with create-only/no-overwrite, redaction, hash and reparse fail-closed checks.
- P4: stale snapshots are durable local historical evidence, not a live/product authority source.

## Next Frontier

The next safe macro-block is `NODAL_OS_LOCAL_OPERATOR_SURFACE_LATEST_STATE_SNAPSHOT_EXTERNAL_AUDIT_READ_ONLY`.

Separate authorization is still required before any public/product exposure, Production route, broader workspace action, durable snapshot/state promotion, edit/update/delete, provider/cloud/network, DB/migration, KMS/WORM/external trust or release/commercial claim.
