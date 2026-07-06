# Nodal OS Durable Latest State Reader Candidate Boundary Design-Only Handoff

Date: 2026-07-06

Decision: `GO_WITH_FINDINGS_DURABLE_LATEST_STATE_READER_CANDIDATE_BOUNDARY_DESIGN_ONLY_READY`

Baseline HEAD: `2caa0aaf641b4626c93f54663178664458b837cc`

## Outcome

Defined a future `LocalDurableLatestStateReaderCandidateNotAuthority` contract and test plan.

No implementation was added.

## Still Not Enabled

- Active durable reader.
- Read precedence.
- Runtime/product enablement.
- Public/product path.
- Production route.
- Product DI/service registration.
- Command handler.
- UI product action.
- Provider/cloud/network.
- DB/migration.
- KMS/WORM/external trust.
- Browser/CDP/WCU/OCR/Recipes live authority.
- Release/commercial readiness.

## Stop Frontier

The next meaningful step is reader candidate implementation. That adds reader code or route behavior and requires explicit GO.
