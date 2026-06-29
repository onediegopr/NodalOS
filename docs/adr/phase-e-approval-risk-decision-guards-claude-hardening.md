# ADR: Phase E Approval Risk Decision Guards With Claude Hardening

Decision target: `GO_PHASE_E_APPROVAL_RISK_DECISION_GUARDS_WITH_CLAUDE_HARDENING_READY`

## Status

Accepted as read-only guard hardening.

## Context

Phase E opened with a deterministic Approval/Human Review read-only foundation. An external Claude audit on HEAD `cb18bf0539df9a4143e662828e344b61523886cf` returned GO with no P0/P1 and four P2 plus four P3 findings. This milestone resolves or documents those findings before adding risk/decision guard coverage.

## Claude P2/P3 Disposition

- `P2-001 ApprovalArtifactWriter live IO reachable`: `ApprovalArtifactWriter` remains preexisting and is not modified. Phase E read-only files must not reference `ApprovalArtifactWriter`, `ApprovalPolicy`, `ApprovalBindingValidator`, `Pilot`, `AgentOperations`, writer methods or policy execution paths. Safety tests scan the read-only Phase E source files for those references.
- `P2-002 NoSideEffectProof declarative`: no-side-effect proof remains declarative by design. Source scans remain required, and a test snapshots the non-product `artifacts/approvals` directory before/after calling the read-only presenter and risk/decision guard to prove these paths do not create approval artifacts.
- `P2-003 Schema Rejected dead branch`: schema compatibility now collapses unsafe issues to `Blocked`; the unreachable `Rejected` enum member was removed. This keeps fail-closed behavior.
- `P2-004 ExpectedDecision fixture assertions`: Phase D guard safety tests now compare `fixture.ExpectedDecision` with actual result decisions and distinguish `Blocked` from `Excluded`.
- `P3-001 Exact expected issues`: authority/freshness safety coverage now verifies expected issue values per fixture.
- `P3-002 Cloak/CDP command aliases/docs`: validation reports must list actual dotnet filter equivalents rather than invented gate names.
- `P3-003 Large context file`: no refactor is performed in this hito; keep as P3 debt.
- `P3-004 AssertBlocked naming`: helper names now use `BlockedOrExcluded` where both decisions are valid.

## Decision

Add `ApprovalRiskDecisionReadOnlyGuard` as a deterministic in-memory guard.

It models:

- risk level;
- decision option kind;
- evidence/context state;
- contradiction state;
- human-review state;
- candidate action implication;
- product action count;
- state mutation count;
- preview-only decision semantics;
- blockers and warnings;
- no-side-effect proof.

## Guard Rules

- Critical risk blocks approve.
- Missing evidence blocks approve, except a request-more-evidence preview label.
- Missing/stale/excluded context blocks approve, except a request-context-refresh preview label for missing/stale context.
- Unresolved contradiction blocks approve.
- Product action count greater than zero blocks.
- State mutation count greater than zero blocks.
- Filesystem write, runtime/live, provider/cloud, semantic/vector and LLM-live implications block.
- Raw/sensitive payload is excluded.
- Fixture-only packets are warnings and are not production trusted.
- Reject, request more evidence, request context refresh and defer are labels only; they do not execute and do not mutate state.

## No-Side-Effect Proof Limit

The proof flags are declarative contract fields, not runtime instrumentation. They are supported by:

- source scans for forbidden APIs and execution paths;
- deterministic fixture-only constructors;
- a test snapshot against non-product approval artifact paths;
- explicit zero product-action and zero state-mutation assertions.

## No Goals

- No approval execution.
- No approval state mutation.
- No product action command.
- No runtime/live.
- No filesystem product read/write.
- No DB or dependency.
- No provider/cloud/network.
- No semantic/vector backend.
- No LLM live.
- No durable memory.
- No migration runner.
- No physical export, clipboard or download.

## Future Unlock Requirements

Any future approval execution must require a separate protected design/audit block, explicit user approval model, durable audit trail design, non-fixture evidence/context compatibility, runtime gate review, and a full re-audit of writer/policy/service registration paths.
