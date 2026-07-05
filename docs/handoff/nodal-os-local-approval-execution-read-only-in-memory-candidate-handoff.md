# NODAL OS Local Approval Execution Read-Only In-Memory Candidate Handoff

Date: 2026-07-05

Decision: `GO_WITH_FINDINGS_LOCAL_APPROVAL_EXECUTION_READ_ONLY_IN_MEMORY_CANDIDATE_READY`

## Completed

- Added Core-only approval execution candidate.
- Required fresh approval, action/evidence binding, policy recheck and verified read model.
- Delegated only to read-only/in-memory internal commands.
- Blocked bounded export, append/write/export, public UI, productive command handler, productive DI, path input, raw payload and external/release claims.
- Added Safety and Recipes tests.

## Not Enabled

- route wiring;
- public UI action;
- productive command handler;
- productive DI/service registration;
- approval state persistence;
- append/write/export;
- bounded export;
- arbitrary path input;
- provider/cloud/network;
- DB/migration;
- KMS/WORM/external trust;
- Browser/CDP/WCU/OCR/Recipes live;
- release/commercial readiness.

## Next Recommended Macro-block

`NODAL_OS_LOCAL_APPROVAL_EXECUTION_ROUTE_PREVIEW_EVIDENCE_TEST_ONLY`

Keep it local-only/internal-only/test-only. It may render/read the candidate result as route preview evidence, but must not add executable public UI controls, POST handlers, write/export, default-on runtime or external dependencies.

