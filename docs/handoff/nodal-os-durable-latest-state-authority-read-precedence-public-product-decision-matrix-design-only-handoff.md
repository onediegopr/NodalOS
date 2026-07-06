# Nodal OS Durable Latest State Authority / Read Precedence / Public Product Decision Matrix Design-Only Handoff

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_DURABLE_LATEST_STATE_AUTHORITY_READ_PRECEDENCE_PUBLIC_PRODUCT_DECISION_MATRIX_DESIGN_ONLY_READY`

Baseline HEAD: `3923a87dedd64426d5511eca5953755d858eea15`

## Completed

- Reconciled the current chain from snapshot to manifest to reader candidate.
- Confirmed no authority, precedence, pointer, public/product or Production route exists.
- Compared options A-F.
- Recommended option A: auxiliary evidence integration without precedence or authority.
- Wrote future boundary spec and future test plan.
- Added a guard test to keep active boundaries blocked.
- Updated QA/JSON, roadmap and decision-log.

## Recommendation

Next implementation, if authorized:

`LocalDurableLatestStateAuxiliaryEvidenceNotPrecedenceNotAuthority`

Exact GO:

`AUTHORIZE_NODAL_OS_DURABLE_LATEST_STATE_AUXILIARY_EVIDENCE_NOT_PRECEDENCE_NOT_AUTHORITY_IMPLEMENTATION_WINDOW`

## Still Blocked

- Durable authority.
- Live/product authority.
- Active read precedence.
- Latest pointer.
- Latest pointer overwrite.
- Public/product exposure.
- Production route.
- Broader workspace action.
- Edit/update/delete.
- User-selected path.
- Shell/subprocess.
- Command execution.
- Browser/CDP/WCU/OCR/Recipes live.
- Pilot `/run`.
- Provider/cloud/network.
- DB/migration.
- KMS/WORM/external trust.
- Compliance custody.
- Release/commercial.

## Findings

- P0: 0.
- P1: 0.
- P2: 0.
- P3: option A is the lowest-risk next value frontier; B/C/D/E remain blocked by trust semantics and public/product risk.
- P4: stale evidence remains expected and must be labeled as non-authoritative.
