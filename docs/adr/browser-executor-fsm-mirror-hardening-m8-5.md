# ADR: Browser Executor FSM Mirror Hardening (M8.5)

## Context

The Browser Executor currently uses `BrowserExecutorStepRunner` as a browser-specific state runner. It maps its state transitions into the core `EvidenceLedger` and `StepState`, but it does not directly reuse `SafeExecutionFsm`.

The post-M8 audit found no critical or high issues. The remaining medium risk is future drift between this browser FSM mirror and the canonical execution FSM.

## Decision

Keep `BrowserExecutorStepRunner` for now because it is already integrated with browser-specific policy, target context, verification and CDP action dispatch. Do not refactor it into `SafeExecutionFsm` during M8.5.

Mitigate drift with shared invariant tests:

- `Uncertain` cannot be success;
- `Executed` cannot be success without `Verified`;
- `Failed` cannot be success;
- `Blocked` does not execute;
- `ApprovalRequired` / human-required does not execute;
- critical action without approval does not execute;
- modifying action without idempotency does not execute;
- stale target blocks modifying action;
- success requires `Verified`;
- success requires semantic proof and evidence audit.

## Evidence/Proof Hardening

`EvidenceRefs` alone are not treated as semantic proof. M8.5 adds explicit proof semantics through `ProofRefs` / `HasSemanticProof`.

For browser step success, the runner now requires:

- verification status `Verified`;
- `AllowsStepDone()`;
- semantic proof refs;
- browser evidence/audit entries.

Decorative GUID refs are not sufficient to mark a step done.

## Prohibited Outcomes

The browser runtime must not:

- mark success without `Verified`;
- mark success without proof/evidence;
- treat `Uncertain` as done;
- treat action execution as verification;
- execute sensitive or critical actions without policy/approval;
- execute modifying actions without idempotency;
- execute against stale target/frame context.

## Future Convergence Criteria

Converge `BrowserExecutorStepRunner` into the canonical FSM only after:

- target/profile/session managers are stable;
- credential boundary and human handoff are formalized;
- Browser Executor runtime has enough production smoke coverage;
- the migration can be done test-first without weakening verification/evidence invariants.
