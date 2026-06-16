# M115-M117 - Local Private Preview Release Gate

## Status

Ready with restrictions for controlled internal local private preview.

## Scope

Allowed:

- local Product/Admin preview;
- private local API in-process;
- readiness dashboard;
- diagnostics/evidence review;
- local issue triage;
- operator blocker explanations.

Blocked:

- public SaaS;
- public API;
- real billing;
- real email;
- real credentials;
- sensitive sites;
- submit/pay/sign/delete;
- productive recorder/replay;
- external CDP general-ready claims.

## Evidence

- M51 closed with HTTP read-only target-owned scope.
- M65 closed with target-owned Chrome/CDP/DOM read-only scope.
- M65 ledger ref: `audit-ledger-edb3e2fbb0a0446788dae17a269c0058`.
- M65 ledger hash: `61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e`.

## Authority Boundary

Core decides. Browser Runtime executes. UI/Admin/Companion observe and transport but do not authorize or bypass gates.

## Release Gate Decision

The local release gate may emit `ReadyWithRestrictions` only when:

- build is OK;
- tests are OK;
- canonical worktree is OK;
- M51 and M65 evidence are present with their limited scopes;
- Product/Admin local readiness is present;
- operator runbook exists;
- blocker explanations are ready;
- evidence/log summary is available;
- dangerous surfaces remain blocked.

`ReadyWithRestrictions` is not production readiness and is not external general readiness.
