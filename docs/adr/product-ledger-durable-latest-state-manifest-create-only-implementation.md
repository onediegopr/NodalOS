# Product Ledger Durable Latest State Manifest Create-Only Implementation

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_DURABLE_LATEST_STATE_MANIFEST_CREATE_ONLY_IMPLEMENTATION_READY`

Baseline HEAD: `931f40fbc283958733afb0c163716b9456fd6008`

## Scope

This window implements `LocalOperatorSurfaceLatestStateManifestCreateOnly` as a local-only, internal-only, Development-only versioned manifest/index over the latest-state snapshot evidence.

Implemented boundary:

`docs/test-output/product-ledger/operator-surface-latest-state-manifests/`

Implemented route:

`POST /internal/product-ledger/operator-surface/create-latest-state-manifest`

Implemented state route:

`GET /internal/product-ledger/operator-surface/latest-state-manifest-state`

Production remains 404 because the Product Ledger local dev route mapper returns without mapping these endpoints outside `environment.IsDevelopment()`.

## Implemented Behavior

- Manifest files are `.json` only.
- Filenames are immutable and versioned from manifest id plus source snapshot content hash prefix.
- Writes use create-only semantics with `FileMode.CreateNew`.
- No overwrite, mutable `latest.json`, latest pointer, latest pointer overwrite or read precedence is implemented.
- The source latest-state snapshot must already be created local-only and historical-evidence-only.
- Source snapshot content hash and checkpoint hash must match exactly before manifest creation.
- Manifest payloads contain safe metadata only, including source snapshot id/path/hash/checkpoint, evidence refs, negative flags, manifest content hash and checkpoint hash.
- Redaction-before-persistence is required before a manifest can be created.
- Existing identical safe manifest payloads replay idempotently.
- Existing corrupt, incomplete, authority-claiming or conflicting manifest files are rejected fail-closed.
- Payload path/root/filename/command/url/provider/DB migration fields and user-selected path claims are rejected.
- Public/product, Production, shell/subprocess, command execution, provider/cloud/network, DB/migration, KMS/WORM/external trust, live Browser/CDP/WCU/OCR/Recipes, Pilot `/run`, compliance custody, cloud-backed durability and release/commercial claims are rejected.
- The operator surface shows manifest state, safe relative path, source snapshot hash/checkpoint, manifest hash/checkpoint, stale policy, evidence refs, blockers and negative flags.

## Manifest Classification

The manifest is classified as:

`LOCAL_INTERNAL_DEV_ONLY_VERSIONED_MANIFEST_NOT_AUTHORITY`

Stale policy:

`MANIFESTS_ARE_HISTORICAL_INDEX_EVIDENCE_ONLY_NOT_LIVE_PRODUCT_AUTHORITY`

This implementation is an index/evidence artifact only. It is not an active durable reader, read-model authority, product ledger path, public product route, Production route, compliance custody claim or cloud-backed durability claim.

## Corrections During Implementation

- P2 fixed before commit: existing JSON that deserializes into an incomplete manifest payload now returns `ExistingManifestCorrupt` instead of falling through to a generic path-canonicalization rejection.
- Static guards were updated from seven to eight allowed local Development POST routes, adding only the manifest create-only route.
- Production-route Recipes coverage now asserts both manifest POST and manifest state GET remain 404.

## Explicit Non-Goals

This window does not implement or enable:

- active durable/latest-state reader;
- read precedence over snapshots or the in-process surface;
- mutable latest pointer;
- public/product path;
- Production route;
- broader workspace action;
- edit/update/delete;
- user-selected path;
- shell/subprocess;
- command execution;
- Pilot `/run`;
- Browser/CDP/WCU/OCR/Recipes live authority;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- release/commercial readiness;
- compliance custody;
- cloud-backed durability.

## Findings

- P0: 0.
- P1: 0.
- P2: 0 after the corrupt existing payload classification fix.
- P3: a new bounded local file write exists only under `docs/test-output/product-ledger/operator-surface-latest-state-manifests/`, guarded by create-only/no-overwrite, redaction, hash/checkpoint and reparse fail-closed checks.
- P4: manifests are historical index/evidence only and can become stale by design.

## Next Frontier

The next safe macro-block is `NODAL_OS_DURABLE_LATEST_STATE_MANIFEST_CREATE_ONLY_EXTERNAL_AUDIT_READ_ONLY`.

Separate authorization remains required before active durable latest-state reader behavior, read precedence, public/product exposure, Production route, provider/cloud/network, DB/migration, KMS/WORM/external trust, release/commercial readiness or compliance custody.
