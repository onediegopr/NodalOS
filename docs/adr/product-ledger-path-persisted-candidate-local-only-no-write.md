# Product Ledger Path Persisted Candidate Local-Only No-Write

Date: 2026-07-04

Decision: `GO_WITH_FINDINGS_PRODUCT_LEDGER_PATH_PERSISTED_CANDIDATE_LOCAL_ONLY_NO_WRITE_READY`

## Scope

This block implements a local-only persisted active path candidate registry for product ledger path readiness.

Persistence is in-memory inside the Core registry instance. It is not a product ledger path, not a filesystem ledger, not a writer and not runtime/product enablement.

## Implemented

- `ProductLedgerPathPersistedCandidateRegistry`.
- `ProductLedgerPathPersistedCandidateRequest`.
- `ProductLedgerPathPersistedCandidateResult`.
- `ProductLedgerPathPersistedCandidateRecord`.
- `ProductLedgerPathPersistedCandidateDecision`.
- `ProductLedgerPathPersistedCandidateBlocker`.
- Safety tests for fail-closed defaults, missing/failed active policy, missing/failed canonicalization, candidate id blockers, evidence blockers, product enablement blockers, in-memory persistence only and static no-writer/no-registration guards.
- Recipes tests for accepted local-memory candidate and unsafe corpus rejection.

## Boundary

Positive status is `PERSISTED_ACTIVE_PATH_CANDIDATE_LOCAL_ONLY_NO_WRITE`.

Explicit non-capabilities:

- no active product ledger path;
- no writer;
- no append-only ledger;
- no filesystem ledger persistence;
- no product DI/service registration;
- no command handlers;
- no UI product actions;
- no DB/migration/provider/cloud/network;
- no KMS/WORM/external trust;
- no Browser/CDP/WCU/OCR/Recipes live execution;
- no runtime/product enablement;
- no release/commercial readiness.

## Remaining Product Frontier

The registry stores a candidate record in local process memory only. The next step can still be a safe writer scaffold disabled/test-only block, but any product writer, persisted active path connected to runtime, productive registration, DB/provider/cloud/network, KMS/WORM/external trust or release/commercial readiness remains outside this boundary.

## Next Safe Block

`NODAL_OS_PRODUCT_LEDGER_PATH_PERSISTED_CANDIDATE_EXTERNAL_AUDIT_READ_ONLY`
