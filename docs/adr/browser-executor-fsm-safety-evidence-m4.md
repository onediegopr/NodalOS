# ADR: Browser Executor FSM / Safety / Evidence integration M4

## Status

Accepted for M4. This ADR documents the internal integration between the CDP-first Browser Executor and ONE BRAIN execution governance.

## Context

M1 defined Browser Executor contracts. M2 implemented a fixture-first CDP executor. M3 added smoke gates and diagnostics. M4 connects that executor to an internal execution adapter that applies policy, approval, verification, and evidence before a browser step can succeed.

The existing `SafeExecutionFsm` remains focused on the current UIA execution path. M4 does not replace it. Instead, it introduces a browser-specific adapter that mirrors the same fail-closed principles and writes to the existing `EvidenceLedger` model.

## What Was Integrated

- `BrowserExecutorStepRunner` orchestrates browser step execution.
- `BrowserExecutorPolicyGate` validates actions before execution.
- `BrowserHumanHandoffRequest` represents approval/human-in-the-loop requirement.
- `BrowserExecutorEvidenceAudit` combines browser evidence with Core `EvidenceLedger`.
- Browser runtime errors map to `FailureKind` and browser step states.
- Verification is mandatory before success.

## FSM Mapping

Browser states:

- `Planned`
- `PolicyChecking`
- `ApprovalRequired`
- `ReadyToExecute`
- `Executing`
- `Executed`
- `Verifying`
- `Verified`
- `Failed`
- `Uncertain`
- `Cancelled`
- `TimedOut`
- `Blocked`
- `RequiresHuman`

These map to Core `StepState` values for evidence/audit. `Executed` maps to a non-terminal verification state. Only `Verified` maps to success.

## Safety / Policy

The policy gate enforces:

- action contract validation;
- capability/risk checks;
- stale target rejection;
- idempotency requirement for modifying actions;
- approval requirement for critical or explicitly approval-bound actions.

Policy failure blocks before dispatch. Browser executor code is not called when policy denies execution.

## Approval / HITL

M4 introduces a minimal non-UI approval model:

- critical/sensitive actions without approval return `ApprovalRequired`;
- a `BrowserHumanHandoffRequest` preserves correlation id, action id, risk, reason, and target context;
- no browser action is executed before approval.

This is not a full workflow or side panel UI.

## Evidence / Audit

M4 records:

- action evidence from the Browser Executor;
- verification evidence;
- transition evidence in Core `EvidenceLedger`;
- policy/verification reasons;
- target context hash as observed identity.

If the Core evidence model evolves, this adapter should be wired into the canonical ledger rather than replaced by a parallel log.

## Verification Gate

Rules enforced:

```text
Executed != Verified
Uncertain != Done
Failed verification != Success
```

The runner marks success only when:

- action executed;
- verification status is `Verified`;
- verification has evidence refs;
- browser evidence exists.

## Out of Scope

M4 does not:

- touch MercadoLibre or external sites;
- automate login, CAPTCHA, 2FA, banks, AFIP, or real portals;
- migrate the service worker;
- change UX or side panel;
- add product features;
- implement WebView2 or CEF;
- use a real user profile;
- implement full session/profile manager;
- implement recorder/download/upload/network capture.

## What This Prepares

M4 prepares M5 by proving that browser execution can be governed by Core-style policy, approval, verification, and evidence. The next step is removing execution ownership from the MV3 service worker without losing these guarantees.

## Progress

Browser Executor / Browser Runtime Layer after M4: approximately 71%.

ONE BRAIN global after M4: approximately 70%.
