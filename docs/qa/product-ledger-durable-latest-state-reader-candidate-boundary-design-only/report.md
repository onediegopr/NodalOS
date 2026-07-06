# Product Ledger Durable Latest State Reader Candidate Boundary Design-Only QA

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_DURABLE_LATEST_STATE_READER_CANDIDATE_BOUNDARY_DESIGN_ONLY_READY`

Baseline HEAD: `2caa0aaf641b4626c93f54663178664458b837cc`

## Scope

Design-only/readiness-only/test-plan-only boundary for a future `LocalDurableLatestStateReaderCandidateNotAuthority`.

## QA Result

- Source/runtime behavior changed: no.
- Active reader implemented: no.
- Read precedence changed: no.
- Runtime/product enabled: no.
- Public/product route: no.
- Production route: no.
- Provider/cloud/network/DB/KMS/WORM/release/commercial: no.

## Findings

- P0: 0.
- P1: 0.
- P2: 0.
- P3: future candidate reads must remain diagnostic-only and fail closed on stale/corrupt/mismatched evidence.
- P4: reader candidate evidence can become stale and must not be authority.

## Test Plan Coverage Required Later

- Valid manifest plus matching snapshot creates a diagnostic candidate only.
- Corrupt/incomplete manifest blocks.
- Manifest authority/product/latest-pointer/read-precedence claims block.
- Missing snapshot blocks.
- Hash/checkpoint mismatch blocks.
- Path traversal, Windows-drive, UNC and reparse variants block.
- Directory scan and mutable `latest.json` are absent.
- Production route remains absent.
- Product DI, command handler, UI action, DB/provider/cloud/network/KMS/WORM/live automation and release/commercial remain absent.

## Next Step

Implementation of the reader candidate requires explicit GO if it adds code or route behavior.
