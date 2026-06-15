# ADR: Browser Sensitive Read-Only Simulation M33A

## Status

Accepted.

## Context

M32 defined the compliance policy for sensitive sites. M33A proves that policy in a runtime-facing simulation without touching AFIP, banks, ERP systems, fiscal/government portals, customer accounts, or real credentials.

M25B remains blocked because no safe test-owned external target exists. Therefore M33A is explicitly local/sandbox and does not enable a real sensitive pilot.

## Decision

Add a sensitive read-only simulation layer with:

- local synthetic fixture metadata for Fiscal, Banking, and ERP surfaces;
- `BrowserSensitiveReadOnlySimulationRequest`, policy, result, step, and evidence contracts;
- `BrowserSensitiveReadOnlySimulationRunner`;
- M32 `SensitiveSitePolicyEvaluator` enforcement before any success result;
- single approval requirement for critical/high read-only simulation;
- read-only guard proof;
- semantic verification proof before Done;
- audit/evidence with secrets, cookies, session material, bodies, and sensitive header values excluded.

## What Is Simulated

- Sensitive category classification.
- Read-only dashboard/status observation.
- Human approval reference.
- Gate enforcement.
- Read-only guard.
- Blocking of submit, pay, sign, delete, publish, approve, profile changes, and credential changes.

## What Is Blocked

- AFIP, banks, ERP, fiscal, financial, or government real sites.
- Personal, commercial, or customer accounts.
- Real credentials.
- Raw Chrome profiles.
- Productive recorder/replay.
- Request/response bodies.
- Sensitive header values.
- Cookies/session values in UI, protocol, audit, evidence, or export.
- Submit/pay/sign/delete.

## Done Criteria

The simulation can close as Done only when:

- policy decision allows read-only with approval;
- gate passed;
- approval ref exists;
- dashboard/status proof exists;
- read-only guard is active;
- irreversible actions are blocked;
- semantic proof refs are present;
- audit/evidence are redacted.

HTTP 200, cookie presence, or `UserCompleted` are not sufficient.

## Future Work

Before M33B or any real sensitive pilot:

- external target ownership and allowlist must be established;
- compliance owner must approve the target;
- real pilot gate must be separate from simulation gate;
- legal/security review must approve data handling;
- no submit/pay/sign/delete until a dedicated irreversible-action policy milestone.
